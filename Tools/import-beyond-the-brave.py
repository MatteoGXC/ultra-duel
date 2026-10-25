"""Import 100 Beyond the Brave cards with reviewed native scripts and archetypes."""
import argparse,hashlib,json,re,shutil,sqlite3,zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Tools/BeyondTheBrave'
def main():
 p=argparse.ArgumentParser();p.add_argument('--art',type=Path,required=True);args=p.parse_args()
 cards=json.loads((SOURCE/'cards.json').read_text(encoding='utf-8'))['data']
 records=json.loads((SOURCE/'native-records.json').read_text(encoding='utf-8'));assert len(cards)==len(records)==100
 ids={c['id'] for c in cards};assert ids=={r['datas'][0] for r in records}
 for file in sorted((ROOT/'Data/locales').glob('*/cards.cdb')):
  with sqlite3.connect(file) as db:
   for row in records:
    d,t=row['datas'],row['texts'];db.execute('insert or replace into datas values ('+','.join('?'*len(d))+')',d)
    verb='insert or replace' if file.parent.name=='en-US' else 'insert or ignore'
    db.execute(verb+' into texts values ('+','.join('?'*len(t))+')',t)
   assert db.execute('pragma integrity_check').fetchone()[0]=='ok'
 for file in sorted((ROOT/'Data/locales').glob('*/strings.conf')):
  if file.parent.name=='en-US':
   text=file.read_text(encoding='utf-8-sig');text=re.sub(r'^!setname 0x1e2 .+$','!setname 0x1e2 Angelechy',text,flags=re.M);file.write_text(text,encoding='utf-8')
 with sqlite3.connect(ROOT/'Data/pack/zz-ultra-tcg.db') as db:
  for c in cards:db.execute('insert or replace into pack values (?,?,?,?,?)',(c['id'],'BETB-EN','BEYOND THE BRAVE',' / '.join(sorted({a['set_rarity'] for a in c.get('card_sets',[]) if a['set_name']=='Beyond the Brave'})),'2026-10-08'))
  assert db.execute('select count(*) from pack where pack_id="BETB-EN"').fetchone()[0]==100
 with zipfile.ZipFile(ROOT/'Data/script.zip') as z:scripts={n:z.read(n) for n in z.namelist()}
 for file in SOURCE.glob('c*.lua'):
  if re.fullmatch(r'c[0-9]+\.lua',file.name):scripts['script/'+file.name]=file.read_bytes()
 marker=b'\n-- ULTRA_BEYOND_THE_BRAVE_COMPAT_BEGIN\n'
 scripts['script/utility.lua']=scripts['script/utility.lua'].split(marker)[0]+marker+(SOURCE/'compat.lua').read_bytes()
 temp=ROOT/'Data/script-betb.tmp'
 with zipfile.ZipFile(temp,'w',zipfile.ZIP_DEFLATED) as z:
  for name,body in scripts.items():z.writestr(name,body)
 temp.replace(ROOT/'Data/script.zip')
 art=[]
 for c in cards:
  file=args.art/(str(c['id'])+'.jpg');body=file.read_bytes();assert body.startswith(b'\xff\xd8') and len(body)>1000
  target=ROOT/'Picture/Art'/file.name;shutil.copy2(file,target)
  art.append(dict(id=c['id'],bytes=len(body),sha256=hashlib.sha256(body).hexdigest()))
 index=[dict(id=c['id'],name=c['name'],pack='BEYOND THE BRAVE',packId='BETB-EN',date='2026-10-08') for c in cards]
 (ROOT/'Data/beyond-the-brave-index.json').write_text(json.dumps(index,indent=2)+'\n',encoding='utf-8')
 audit=dict(pack='BEYOND THE BRAVE',uniqueCards=100,ids=sorted(ids),portedScripts=sorted(int(p.stem[1:]) for p in SOURCE.glob('c[0-9]*.lua')),art=art,
  sources=dict(pack='https://ygoprodeck.com/pack/Beyond%20the%20Brave/',babelCommit='9b6772ad0125cce3ffc2dda6fdae58e3301ff153',scriptCommit='4105d0660807179f59e90111f3a8d804768899d6'),archetypes='Native Fluorohydride IDs; Angelechy uses 0x1e2')
 (ROOT/'Data/betb-update-audit.json').write_text(json.dumps(audit,indent=2)+'\n',encoding='utf-8')
 print('BEYOND_THE_BRAVE_IMPORTED',len(cards),'unique cards; native scripts, all locales, pack index and normal artwork')
if __name__=='__main__':main()
