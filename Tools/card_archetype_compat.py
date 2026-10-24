"""Keep Babel's card updates but use the archetype IDs required by the native scripts."""
import hashlib
import json
import re
from pathlib import Path
import sqlite3
import urllib.request

COMMIT='217ae165c1886af2fc36b823fc08f9fc3ec13185'
SHA256='ff52f585de24a762a4e613ac37654b1a7fb15a58990c14f5eada85f28c3be152'
URL=f'https://raw.githubusercontent.com/mycard/ygopro-database/{COMMIT}/locales/zh-CN/cards.cdb'
NAMES_URL=f'https://raw.githubusercontent.com/mycard/ygopro-database/{COMMIT}/locales/en-US/strings.conf'
NAMES_SHA256='f1282302f54926094044cfb2e2f9a66a74b7fd642b891d8be7f4e79d8cbdc2e5'

def align_names(root, cache):
    names_cache=cache.parent/'native-compatible-en-strings.conf'
    if not names_cache.exists():names_cache.write_bytes(urllib.request.urlopen(NAMES_URL,timeout=60).read())
    assert hashlib.sha256(names_cache.read_bytes()).hexdigest()==NAMES_SHA256,'Archetype names checksum mismatch.'
    def parse(text):
        return {int(k,16):v.strip() for k,v in re.findall(r'^!setname\s+(0x[0-9a-fA-F]+)\s+(.+)$',text,re.M)}
    native=parse(names_cache.read_text(encoding='utf-8-sig'))
    path=root/'Data/locales/en-US/strings.conf'
    text=path.read_text(encoding='utf-8-sig')
    local=parse(text)
    for code,name in native.items():
        if code not in local:text+=f'\n!setname {hex(code)} {name}'
        elif local[code]!=name:
            text=re.sub(rf'^!setname\s+0x{code:x}\s+.+$',lambda _:f'!setname {hex(code)} {name}',text,flags=re.M|re.I)
    text=re.sub(r'^!system 1329 .+$','!system 1329 Archetype:',text,flags=re.M)
    path.write_text(text.rstrip()+'\n',encoding='utf-8')
    assert all(parse(text).get(k)==v for k,v in native.items())
    return dict(source=NAMES_URL,sha256=NAMES_SHA256,verifiedLabels=len(native))

def apply(root):
    root=Path(root)
    cache=root.parent/'checks/current-card-data/native-compatible-cards.cdb'
    cache.parent.mkdir(parents=True,exist_ok=True)
    if not cache.exists():cache.write_bytes(urllib.request.urlopen(URL,timeout=120).read())
    assert hashlib.sha256(cache.read_bytes()).hexdigest()==SHA256,'Archetype source checksum mismatch.'
    with sqlite3.connect(cache) as source:tags=dict(source.execute('select id,setcode from datas'))
    report_path=root/'Data/card-archetype-audit.json'
    prior=json.loads(report_path.read_text()) if report_path.exists() else {}
    corrections={r['id']:r for r in prior.get('cards',[])}
    changed={}
    verified={}
    for path in sorted((root/'Data/locales').glob('*/cards.cdb')):
        with sqlite3.connect(path) as db:
            edits=[]
            for code,alias,value in db.execute('select id,alias,setcode from datas'):
                native=tags.get(code,tags.get(alias))
                if native is not None and native!=value:
                    edits.append((native,code))
                    corrections.setdefault(code,dict(id=code,babelSetcode=value,nativeSetcode=native))
            db.executemany('update datas set setcode=? where id=?',edits)
            assert db.execute('pragma integrity_check').fetchone()[0]=='ok'
            changed[path.parent.name]=len(edits)
            checked=0
            for code,alias,value in db.execute('select id,alias,setcode from datas'):
                native=tags.get(code,tags.get(alias))
                if native is not None:
                    assert value==native,(path,code,value,native)
                    checked+=1
            verified[path.parent.name]=checked
    labels=align_names(root,cache)
    report=dict(source=URL,sourceCommit=COMMIT,sourceSha256=SHA256,field='setcode',labels=labels,verifiedCardsByLocale=verified,
        reason='Babel and Fluorohydride assign different archetype IDs; native card scripts require the latter.',
        cards=sorted(corrections.values(),key=lambda r:r['id']))
    report_path.write_text(json.dumps(report,indent=2)+'\n')
    print('ARCHETYPE_COMPAT',len(corrections),'card tags normalized;',changed,flush=True)
    return report

if __name__=='__main__':apply(Path(__file__).resolve().parents[1])
