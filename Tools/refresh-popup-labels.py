"""Align effect-choice text with the bundled legacy scripts, preserving card data."""
from pathlib import Path
import argparse,hashlib,json,re,sqlite3,urllib.request,zipfile

ROOT=Path(__file__).resolve().parents[1]

def strings(path):
    with sqlite3.connect(path) as db:return {r[0]:r for r in db.execute('select * from texts')}

def call_body(source,start):
    depth=1;position=start;quote=None
    while position<len(source) and depth:
        char=source[position]
        if quote:
            if char=='\\':position+=2;continue
            if char==quote:quote=None
        elif char in ('"',"'"):quote=char
        elif char=='(':depth+=1
        elif char==')':depth-=1
        position+=1
    return source[start:position-1]

def references(scripts,known):
    used=set();choices=set()
    with zipfile.ZipFile(scripts) as archive:
        for name in archive.namelist():
            match=re.search(r'(?:^|/)c(\d+)\.lua$',name)
            if not match or int(match[1]) not in known:continue
            code=int(match[1]);source=archive.read(name).decode('utf-8-sig',errors='replace')
            def collect(text):
                result=set()
                for m in re.finditer(r'(?:aux|Auxiliary)\.Stringid\(\s*(id|\d+)\s*,\s*(\d+)\s*\)',text):
                    card=code if m[1]=='id' else int(m[1]);index=int(m[2])
                    if card in known and 0<=index<16:result.add((card,index))
                return result
            used.update(collect(source))
            for call in re.finditer(r'(?:Duel\.SelectOption|(?:aux|Auxiliary)\.SelectFromOptions)\(',source):
                choices.update(collect(call_body(source,call.end())))
    return used,choices

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--cache',type=Path,default=ROOT.parent/'checks/popup-label-sources');args=parser.parse_args()
    specification=json.loads((ROOT/'Data/popup-label-sources.json').read_text())
    args.cache.mkdir(parents=True,exist_ok=True);sources={}
    for locale,parts in specification['locales'].items():
        for name,part in parts.items():
            target=args.cache/(locale+('.cdb' if name.endswith('.cdb') else '.conf'))
            if not target.exists():target.write_bytes(urllib.request.urlopen(part['url'],timeout=120).read())
            assert hashlib.sha256(target.read_bytes()).hexdigest()==part['sha256'],target
        sources[locale]=strings(args.cache/(locale+'.cdb'))
    english=sources['en-US'];current=strings(ROOT/'Data/locales/en-US/cards.cdb')
    used,choices=references(ROOT/'Data/script.zip',current)
    # Every statically named multi-option choice must have a real description
    # from the source that uses the same script indexes. Never silently invent
    # wording or shift an index merely to remove a placeholder.
    for code,index in choices:
        assert english.get(code) and english[code][index+3].strip(),(code,index)
    audit=dict(sourceCommit=specification['commit'],scriptsSha256=hashlib.sha256((ROOT/'Data/script.zip').read_bytes()).hexdigest(),cards=len(current),referencedDescriptions=len(used),selectableDescriptions=len(choices),locales={})
    for path in sorted((ROOT/'Data/locales').glob('*/cards.cdb')):
        locale=path.parent.name;rows=strings(path);translated=sources.get(locale,{})
        changed=0;filled=0
        with sqlite3.connect(path) as db:
            for code,row in rows.items():
                labels=list(row[3:19]);source=translated.get(code)
                if locale=='en-US':source=english.get(code)
                for index in range(16):
                    # Heavy Borger's separate engine effects were deliberately
                    # remapped earlier; retain its tested draw/damage labels.
                    if code==92895501 and index in (2,3) and labels[index]:continue
                    label=source[index+3] if source else ''
                    if not label and not labels[index] and code in english:label=english[code][index+3]
                    if label and label!=labels[index]:
                        filled+=not bool(labels[index]);changed+=1;labels[index]=label
                if tuple(labels)!=row[3:19]:
                    db.execute('update texts set '+','.join('str'+str(n+1)+'=?' for n in range(16))+' where id=?',(*labels,code))
            assert db.execute('pragma integrity_check').fetchone()[0]=='ok'
        result=strings(path)
        missing=[(c,i) for c,i in choices if c not in result or not result[c][i+3].strip()]
        assert not missing,(locale,missing)
        audit['locales'][locale]=dict(changed=changed,filled=filled,missingSelectableDescriptions=0)
        print('POPUP_LABELS',locale,changed,'corrected;',filled,'filled',flush=True)
    audit['choices']=[dict(card=c,index=i,english=(current[c][i+3] if c==92895501 and i in (2,3) else english[c][i+3])) for c,i in sorted(choices)]
    (ROOT/'Data/popup-label-audit.json').write_text(json.dumps(audit,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print('ALL_SELECTABLE_DESCRIPTIONS_VERIFIED',len(choices),flush=True)

if __name__=='__main__':main()
