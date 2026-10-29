"""Download every database card from the user's chosen full-card image source."""
import argparse
import concurrent.futures
import hashlib
import io
import json
from pathlib import Path
import sqlite3
import threading
import time
import unicodedata
import requests
from PIL import Image

TEMPLATE = 'https://static.yugiohcardlist.com/images/cards/full/{id}.jpg'
# Reviewed against the downloaded card's name, type, level, effect and ATK/DEF.
# This site stores Barrel Dragon's illustration under its alternate image number.
REVIEWED_SOURCE_IDS = {81480460: 81480461}
local = threading.local()

def resolve_aliases(root):
    report_path=root/'Data/card-image-source.json'
    report=json.loads(report_path.read_text(encoding='utf-8'))
    if not report.get('complete'):
        raise RuntimeError('Finish the full source download before resolving printed card numbers')
    with sqlite3.connect(f'{(root/"Data/locales/en-US/cards.cdb").as_uri()}?mode=ro',uri=True) as db:
        records={r[0]:r for r in db.execute('select d.id,d.alias,t.name,t.desc,d.type,d.atk,d.def,d.level,d.race,d.attribute from datas d join texts t on d.id=t.id')}
    by_id={row['id']:row for row in report['cards']}
    for row in report['cards']:
        if row['status']=='ok':continue
        if row['id'] in REVIEWED_SOURCE_IDS:
            source_id=REVIEWED_SOURCE_IDS[row['id']];url=TEMPLATE.format(id=source_id)
            response=requests.get(url,headers={'User-Agent':'Mozilla/5.0','Referer':'https://yugiohcardlist.com/'},timeout=30)
            response.raise_for_status();body=response.content
            with Image.open(io.BytesIO(body)) as image:
                image.load();size=list(image.size)
            (root/f'Picture/FullCard/{row["id"]}.jpg').write_bytes(body)
            row.update(status='ok',sourceId=source_id,url=url,size=size,bytes=len(body),sha256=hashlib.sha256(body).hexdigest(),
                resolution='Source alternate image number; name, type, level, effect and ATK/DEF visually verified')
            row.pop('error',None)
            continue
        record=records[row['id']];alias=record[1]
        candidates=[]
        for candidate in records.values():
            if unicodedata.normalize('NFKC',record[2])!=unicodedata.normalize('NFKC',candidate[2]):continue
            # Level/attribute fields have no meaning on a Spell or Trap record.
            if record[4]!=candidate[4] or (record[4]&1 and record[5:]!=candidate[5:]):continue
            same_family=(alias and candidate[0]==alias) or candidate[1]==record[0] or (alias and candidate[1]==alias)
            same_rules=record[3].replace('\r','').strip()==candidate[3].replace('\r','').strip()
            if same_family or same_rules:
                if by_id.get(candidate[0],{}).get('status')=='ok':candidates.append(candidate[0])
        if not candidates:continue
        source_id=min(candidates,key=lambda value:(value!=alias,'sourceId' in by_id[value],value))
        source=by_id[source_id]
        body=(root/f'Picture/FullCard/{source_id}.jpg').read_bytes()
        (root/f'Picture/FullCard/{row["id"]}.jpg').write_bytes(body)
        row.update(status='ok',sourceId=source.get('sourceId',source_id),url=source['url'],
            size=source['size'],bytes=len(body),sha256=source['sha256'],
            resolution='Same card name and stats, with a database alias relationship or matching rules text; source image uses the available card number')
        row.pop('error',None)
    report['downloaded']=sum(r['status']=='ok' for r in report['cards'])
    report['printedNumberMappings']=sum('sourceId' in r for r in report['cards'])
    report_path.write_text(json.dumps(report,indent=2),encoding='utf-8')
    missing=[r for r in report['cards'] if r['status']!='ok']
    (root/'Data/card-image-missing.json').write_text(json.dumps(missing,indent=2),encoding='utf-8')
    print('CARDLIST_PRINTED_NUMBERS',report['printedNumberMappings'],'available',report['downloaded'],'/',report['total'],'missing',len(missing),flush=True)

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--runtime', type=Path, required=True)
    parser.add_argument('--workers', type=int, default=8)
    parser.add_argument('--resolve-aliases-only', action='store_true')
    parser.add_argument('--retry-errors-only', action='store_true')
    args = parser.parse_args()
    root = args.runtime.resolve()
    if args.resolve_aliases_only:
        resolve_aliases(root)
        return
    destination = root/'Picture/FullCard'
    destination.mkdir(parents=True, exist_ok=True)
    with sqlite3.connect(f'{(root/"Data/locales/en-US/cards.cdb").as_uri()}?mode=ro', uri=True) as db:
        cards = [dict(id=r[0], name=r[1]) for r in db.execute('select id,name from texts order by id')]
    report_path = root/'Data/card-image-source.json'
    previous = json.loads(report_path.read_text()) if report_path.exists() else {}
    old = {r['id']:r for r in previous.get('cards',[])}
    total_cards=len(cards)
    retained=[]
    if args.retry_errors_only:
        retry_ids={r['id'] for r in old.values() if r['status']!='ok' and '404 Client Error' not in r.get('error','') and 'Not a full card image' not in r.get('error','')}
        cards=[card for card in cards if card['id'] in retry_ids]
        retained=[row for row in old.values() if row['id'] not in retry_ids]

    def decode(body):
        if not body.startswith(b'\xff\xd8'):
            raise ValueError('Source did not return a JPEG')
        with Image.open(io.BytesIO(body)) as image:
            image.load()
            if image.width < 200 or image.height < 300 or not .55 < image.width/image.height < .85:
                raise ValueError(f'Not a full card image: {image.size}')
            return list(image.size)

    def fetch(card):
        code=card['id']; path=destination/f'{code}.jpg'; url=TEMPLATE.format(id=code)
        row=dict(card,url=url)
        try:
            cached=old.get(code,{})
            if path.exists() and cached.get('status')=='ok' and cached.get('url')==url:
                body=path.read_bytes(); size=decode(body)
                if hashlib.sha256(body).hexdigest()!=cached.get('sha256'):
                    raise ValueError('Cached image hash changed')
            else:
                if not hasattr(local,'session'):
                    local.session=requests.Session()
                    local.session.headers.update({'User-Agent':'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36',
                        'Referer':'https://yugiohcardlist.com/','Accept':'image/avif,image/webp,image/apng,image/*,*/*;q=0.8'})
                for attempt in range(3):
                    response=local.session.get(url,timeout=(10,35))
                    if response.status_code in (429,500,502,503,504) and attempt<2:
                        time.sleep(min(60,float(response.headers.get('Retry-After',2**(attempt+1)))));continue
                    response.raise_for_status();break
                body=response.content;size=decode(body)
                temporary=path.with_suffix('.partial');temporary.write_bytes(body);temporary.replace(path)
            row.update(status='ok',size=size,bytes=len(body),sha256=hashlib.sha256(body).hexdigest())
        except Exception as error:
            row.update(status='unavailable',error=str(error))
        return row

    rows=list(retained)
    def save(complete=False):
        result=dict(provider='yugiohcardlist',urlTemplate=TEMPLATE,authoritative=True,
            total=total_cards,checked=len(rows),downloaded=sum(r['status']=='ok' for r in rows),
            complete=complete,cards=sorted(rows,key=lambda r:r['id']))
        temp=report_path.with_suffix('.partial');temp.write_text(json.dumps(result,indent=2),encoding='utf-8');temp.replace(report_path)
    with concurrent.futures.ThreadPoolExecutor(max_workers=args.workers) as pool:
        tasks=[pool.submit(fetch,card) for card in cards]
        for task in concurrent.futures.as_completed(tasks):
            rows.append(task.result())
            if len(rows)%200==0:
                save();print('CARDLIST',len(rows),'/',len(cards),'downloaded',sum(r['status']=='ok' for r in rows),flush=True)
    save(True)
    failures=[r for r in rows if r['status']!='ok']
    print('CARDLIST_COMPLETE',len(rows)-len(failures),'/',total_cards,'missing',len(failures),flush=True)
    (root/'Data/card-image-missing.json').write_text(json.dumps(failures,indent=2),encoding='utf-8')

if __name__=='__main__':
    main()
