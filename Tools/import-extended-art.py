"""Import actual extended-art print images; never crop or synthesize card art."""
import concurrent.futures
import hashlib
import json
from pathlib import Path
import re
import urllib.parse
import urllib.request

ROOT=Path(__file__).resolve().parents[1]
CACHE=ROOT.parent/'checks/full-art-import'
SETS=[('Rarity Collection 5','RA05-',141,150),('Battles of Legend: Glorious Gallery','BLGG-',137,146),('Magnificent Monsters','MAMO-',1,18),('Chaos Origins','CORI-',None,None),('Beyond the Brave','BETB-',None,None)]

def fetch(url):
    return urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'Ultra Duel artwork verification'}),timeout=35).read()

def main():
    dest=ROOT/'Picture/ExtendedArt';dest.mkdir(parents=True,exist_ok=True);CACHE.mkdir(parents=True,exist_ok=True)
    candidates={}
    for name,prefix,low,high in SETS:
        path=CACHE/(name.replace(' ','-')+'-misc.json')
        if not path.exists():path.write_bytes(fetch('https://db.ygoprodeck.com/api/v7/cardinfo.php?misc=yes&cardset='+urllib.parse.quote(name)))
        selected={}
        for card in json.loads(path.read_text(encoding='utf-8'))['data']:
            prints=[s for s in card.get('card_sets',[]) if s['set_name']==name and s['set_code'].startswith(prefix)]
            for printing in prints:
                number=int(re.search(r'EN(\d+)$',printing['set_code'])[1])
                eligible=low<=number<=high if low is not None else bool({'Ultra Rare','Secret Rare'} & {s['set_rarity'] for s in prints})
                if eligible:selected[card['id']]=(card,printing['set_code'],name,low is None)
        expected=high-low+1 if low is not None else 5
        if low is not None:assert len(selected)==expected,(name,len(selected),expected)
        candidates.update(selected)
    def download(row):
        code,(card,printing,setname,probe)=row
        cid=next(m['konami_id'] for m in card['misc_info'] if m.get('konami_id'))
        page=f'https://www.db.yugioh-card.com/yugiohdb/card_search.action?ope=2&cid={cid}&request_locale=en'
        html=fetch(page).decode('utf-8');match=re.search(r"(/yugiohdb/get_image\.action\?type=2&cid="+str(cid)+r"&ciid=2&enc=[^']+)",html)
        if not match:
            if probe:return None
            raise ValueError('No verified extended-art image for '+card['name'])
        source='https://www.db.yugioh-card.com'+match[1]+'&request_locale=en'
        body=fetch(source)
        assert len(body)>5000 and (body.startswith(b'\x89PNG') or body.startswith(b'\xff\xd8')),card['name']
        assert hashlib.sha256(body).hexdigest()!=hashlib.sha256((CACHE/'dragoon-official-1.png').read_bytes()).hexdigest() if (CACHE/'dragoon-official-1.png').exists() else True
        (dest/f'{code}.png').write_bytes(body)
        print('EXTENDED_ART',printing,card['name'],flush=True)
        return dict(id=code,name=card['name'],printing=printing,set=setname,cardPage=page,image=source,sha256=hashlib.sha256(body).hexdigest())
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:rows=[r for r in pool.map(download,candidates.items()) if r is not None]
    for name,prefix,low,high in SETS:
        count=sum(r['set']==name for r in rows);assert count==(high-low+1 if low is not None else 5),(name,count)
    report=dict(kind='Official extended-art printing images, unmodified',count=len(rows),cards=sorted(rows,key=lambda r:r['id']))
    (ROOT/'Data/extended-art-audit.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print('EXTENDED_ART_IMPORTED',len(rows),flush=True)

if __name__=='__main__':main()
