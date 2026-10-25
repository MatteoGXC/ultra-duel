"""Refresh maintained card data without losing translated text or alternate artwork IDs."""
import argparse
import concurrent.futures
import datetime
import hashlib
import json
from pathlib import Path
import re
import sqlite3
import time
import urllib.error
import urllib.request
import zipfile
from card_script_compat import apply_description_fixes

ROOT = Path(__file__).resolve().parents[1]
CACHE = ROOT.parent/'checks/current-card-data'
SCRIPT_COMMIT = '0be9a8ac948f6ac38f4ed3ba45d439290d04415a'
DATABASE_COMMIT = 'cf9a274244f2e61dfad12a68ed4517613ac65ba5'

def write_json(path, data):
    path.write_bytes((json.dumps(data, ensure_ascii=False, indent=2)+'\n').encode('utf-8'))

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--art', action='store_true')
    args = parser.parse_args()
    CACHE.mkdir(parents=True, exist_ok=True)
    for name,url in {
        'ignis-cards.cdb':'https://raw.githubusercontent.com/ProjectIgnis/BabelCDB/'+DATABASE_COMMIT+'/cards.cdb',
        'scripts.zip':'https://codeload.github.com/Fluorohydride/ygopro-scripts/zip/'+SCRIPT_COMMIT,
    }.items():
        if not (CACHE/name).exists():
            with urllib.request.urlopen(url, timeout=120) as response: (CACHE/name).write_bytes(response.read())
    previous = {}
    if (ROOT/'Data/card-update-audit.json').exists():
        previous = json.loads((ROOT/'Data/card-update-audit.json').read_text(encoding='utf-8'))
    database_sha = hashlib.sha256((CACHE/'ignis-cards.cdb').read_bytes()).hexdigest()
    if previous.get('databaseCommit') == DATABASE_COMMIT and previous['databaseSha256'] != database_sha:
        raise ValueError('Cached database does not match the pinned audited snapshot.')
    if previous.get('databaseSha256') != database_sha: previous = {}
    incoming = sqlite3.connect(CACHE/'ignis-cards.cdb')
    data = {r[0]:r for r in incoming.execute('select * from datas')}
    text = {r[0]:r for r in incoming.execute('select * from texts')}
    old = sqlite3.connect(ROOT/'Data/locales/en-US/cards.cdb')
    before = {r[0]:r for r in old.execute('select * from datas')}
    old_text = {r[0]:r for r in old.execute('select * from texts')}
    old.close()
    missing = sorted(set(data)-set(before))
    flags = [i for i in data.keys() & before.keys() if data[i][1] != before[i][1]]
    if not missing: missing = previous.get('addedIds', [])
    if not flags: flags = previous.get('availabilityCorrections', [])
    retained = []
    for code in before.keys()-data.keys():
        original = before[code]
        alias = original[2]
        if alias in data:
            row = list(original); row[1] = data[alias][1]
            data[code] = tuple(row)
            text[code] = old_text[code]
        else:
            data[code] = original; text[code] = old_text[code]
        retained.append(code)
    for path in sorted((ROOT/'Data/locales').glob('*/cards.cdb')):
        db = sqlite3.connect(path)
        local = {r[0]:r for r in db.execute('select * from texts')}
        dc = len(next(iter(data.values()))); tc = len(next(iter(text.values())))
        with db:
            db.executemany('insert or replace into datas values ('+','.join('?'*dc)+')', data.values())
            rows = [text[i] if path.parent.name=='en-US' or i not in local else local[i] for i in data]
            db.executemany('insert or replace into texts values ('+','.join('?'*tc)+')', rows)
        assert db.execute('pragma integrity_check').fetchone()[0]=='ok'
        db.close()
    with zipfile.ZipFile(ROOT/'Data/script.zip') as archive:
        scripts = {Path(n).name:archive.read(n) for n in archive.namelist() if n.endswith('.lua')}
    with zipfile.ZipFile(CACHE/'scripts.zip') as archive:
        scripts.update({Path(n).name:archive.read(n) for n in archive.namelist() if n.endswith('.lua')})
    aliases = []
    for code,row in data.items():
        name = f'c{code}.lua'; canonical = f'c{row[2]}.lua'
        # This core only automatically redirects nearby alternate-art IDs. Keep
        # the maintained canonical effects for new aliases outside that range.
        if name not in scripts and row[2] and abs(code-row[2])>10 and canonical in scripts:
            scripts[name] = scripts[canonical].replace(f'c{row[2]}'.encode(), f'c{code}'.encode())
            aliases.append(dict(id=code, alias=row[2]))
    apply_description_fixes(scripts)
    with zipfile.ZipFile(ROOT/'Data/script.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
        for name,body in sorted(scripts.items()):
            archive.writestr('script/'+name, body)
    # Official English Burst Protocol product index; the previous pack database
    # only indexed 16 cards in English despite the cards already being present.
    burst = json.loads((ROOT/'Data/burst-protocol-index.json').read_text(encoding='utf-8'))
    db = sqlite3.connect(ROOT/'Data/pack/zz-ultra-tcg.db')
    with db:
        db.execute('create table if not exists pack (id integer primary key, pack_id text, pack text, rarity text, date text)')
        db.execute('delete from pack')
        db.executemany('insert or replace into pack values (?,?,?,?,?)',
            [(r['id'], 'BPRO-EN', 'BURST PROTOCOL', '', '2026-02-06') for r in burst])
    db.close()
    image_results = []
    if args.art:
        arts = ROOT/'Picture/Art'; arts.mkdir(parents=True, exist_ok=True)
        existing = {int(p.stem) for p in arts.iterdir() if p.stem.isdigit()}
        zipped = set()
        for p in (ROOT/'Expansions').glob('*.zip'):
            with zipfile.ZipFile(p) as z:
                zipped.update(int(Path(n).stem) for n in z.namelist()
                    if Path(n).stem.isdigit() and n.lower().endswith(('.jpg','.png','.jpeg')))
        targets = [i for i in data if i not in existing|zipped and not data[i][4]&0x4000]
        def download(code):
            dest = arts/f'{code}.jpg'
            for attempt in range(2):
                try:
                    url = f'https://images.ygoprodeck.com/images/cards_cropped/{code}.jpg'
                    with urllib.request.urlopen(url, timeout=20) as response: body=response.read()
                    if not body.startswith(b'\xff\xd8') or len(body)<1000: raise ValueError('Not JPEG')
                    dest.write_bytes(body)
                    return dict(id=code, source=url, kind='art')
                except (OSError, ValueError): time.sleep(.3)
            alias=data[code][2]
            if alias and (arts/f'{alias}.jpg').is_file():
                dest.write_bytes((arts/f'{alias}.jpg').read_bytes())
                return dict(id=code, alias=alias, kind='canonical-art')
            try:
                url=f'https://cdn.233.momobako.com/ygopro/pics/{code}.jpg'
                with urllib.request.urlopen(url, timeout=20) as response: body=response.read()
                if not body.startswith(b'\xff\xd8'): raise ValueError('Not JPEG')
                return dict(id=code, source=url, kind='pics', body=body)
            except (OSError, ValueError): return dict(id=code, missing=True, tcg=bool(data[code][1]&2))
        with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
            for index,result in enumerate(pool.map(download, targets)):
                image_results.append(result)
                if (index+1)%25==0: print('ART',index+1,'/',len(targets),flush=True)
        fallbacks=[r for r in image_results if r.get('kind')=='pics']
        if fallbacks:
            with zipfile.ZipFile(ROOT/'Expansions/ultra-card-art.zip','w',zipfile.ZIP_DEFLATED) as z:
                for row in fallbacks: z.writestr(f"pics/{row['id']}.jpg",row.pop('body'))
    if not image_results: image_results = previous.get('art', [])
    report = dict(updatedAt=datetime.datetime.now(datetime.timezone.utc).isoformat(),
        databaseSource='https://github.com/ProjectIgnis/BabelCDB',
        databaseCommit=DATABASE_COMMIT, databaseSha256=database_sha,
        scriptSource='https://github.com/Fluorohydride/ygopro-scripts', scriptCommit=SCRIPT_COMMIT,
        addedIds=missing, addedCount=len(missing), availabilityCorrections=flags,
        retainedLegacyIds=retained, scriptAliasCompatibility=aliases,
        burstProtocolSource='https://www.db.yugioh-card.com/yugiohdb/card_search.action?ope=1&pid=2000001574000&rp=99999&request_locale=en',
        burstProtocolIndexed=len({r['id'] for r in burst}), art=image_results)
    write_json(ROOT/'Data/card-update-audit.json',report)
    from card_archetype_compat import apply as align_archetypes
    align_archetypes(ROOT)
    print('CARD_REFRESH',len(missing),'added;',len(flags),'availability corrections;',len(scripts),'scripts;',len(retained),'legacy IDs preserved',flush=True)
    missing_tcg=[r['id'] for r in image_results if r.get('missing') and r.get('tcg')]
    print('TCG_ART_STILL_MISSING',missing_tcg,flush=True)

if __name__=='__main__': main()
