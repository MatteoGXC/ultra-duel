"""Build beginner archetype recipes and validate each supported format.

Sources are pinned numeric deck snapshots, never executable content. Adaptations
keep each recipe's engine, record every change, and omit unsupported variants.
"""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import re
import sqlite3
import unicodedata
from urllib.parse import quote
import zipfile

ROOT=Path(__file__).resolve().parents[1]
COMMIT='950dc0f2452e5e79c48398efc607e6fdd94c4215'
SOURCE=f'https://github.com/anonfansub/ygodecks/tree/{COMMIT}/Archetypes'
EXTRA=0x4802040
EXCLUDED=0x5000000

def norm(value):
    return re.sub(r'[^a-z0-9]','',unicodedata.normalize('NFKC',value).casefold())

def parse(text):
    parts={'main':[],'extra':[],'side':[]};section=None
    for line in text.lstrip('\ufeff').splitlines():
        line=line.strip()
        if line in ('#main','#extra','!side','#side'):section=line.lstrip('#!')
        elif line.isdigit() and section:parts[section].append(int(line))
    return parts

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--archive',type=Path,required=True)
    args=parser.parse_args()
    with sqlite3.connect(ROOT/'Data/locales/en-US/cards.cdb') as db:
        cards={r[0]:dict(zip(('id','ot','alias','type','setcode','level','name','text'),r)) for r in db.execute('select d.id,d.ot,d.alias,d.type,d.setcode,d.level,t.name,t.desc from datas d join texts t on d.id=t.id')}
    by_name={norm(c['name']):c['id'] for c in sorted(cards.values(),key=lambda c:(bool(c['alias']),c['id']),reverse=True)}
    points={}
    for line in (ROOT/'Data/lflist_genesys.conf').read_text(encoding='utf-8-sig').splitlines():
        p=line.split()
        if len(p)>2 and p[0].isdigit() and p[1]=='$genesys':points[int(p[0])]=int(p[2])
    limits={};started=False
    for line in (ROOT/'Data/lflist.conf').read_text(encoding='utf-8-sig').splitlines():
        if line.startswith('!'):
            if started:break
            started=True
        p=line.split()
        if started and len(p)>1 and p[0].isdigit():limits[int(p[0])]=int(p[1])
    art=json.loads((ROOT/'Data/card-image-source.json').read_text(encoding='utf-8'))
    has_art={c['id'] for c in art['cards'] if c['status']=='ok'}
    set_names={}
    for line in (ROOT/'Data/locales/en-US/strings.conf').read_text(encoding='utf-8-sig').splitlines():
        p=line.split(maxsplit=2)
        if len(p)==3 and p[0]=='!setname':
            try:set_names[int(p[1],0)]=p[2]
            except ValueError:pass
    def key(code):return cards[code]['alias'] or code
    def cost(code):return points.get(code,points.get(key(code),0))
    def allowed(code,mode,part):
        c=cards.get(code)
        return bool(c and c['ot']&2 and not c['type']&0x4000 and code in has_art
            and bool(c['type']&EXTRA)==(part=='extra') and (mode=='TCG' or not c['type']&EXCLUDED))
    def codes(code):
        value=cards[code]['setcode'] & ((1<<64)-1)
        return [(value>>shift)&0xffff for shift in range(0,64,16) if (value>>shift)&0xffff]
    def theme_ids(recipe):
        title=norm(recipe['archetype'])
        matches={value for value,name in set_names.items() if norm(name)==title or (len(norm(name))>=5 and norm(name) in title)}
        result=set()
        for code in recipe['main']+recipe['extra']:
            if code not in cards:continue
            card=cards[code]
            if (len(title)>=4 and title in norm(card['name'])) or any((value&0xfff)==(wanted&0xfff) for value in codes(code) for wanted in matches):result.add(code)
        if not result:
            frequencies=Counter(value&0xfff for code in recipe['main'] if code in cards for value in codes(code))
            if frequencies:
                major=frequencies.most_common(1)[0][0]
                result={code for code in recipe['main']+recipe['extra'] if code in cards and any(value&0xfff==major for value in codes(code))}
        return result

    snapshots=[]
    with zipfile.ZipFile(args.archive) as archive:
        for name in archive.namelist():
            if '/Archetypes/' not in name or not name.endswith('.ydk'):continue
            raw=archive.read(name)
            title=re.sub(r'^\[TCG\]\s*','',Path(name).stem)
            title=re.sub(r'\s*(?:20\d\d|[2-9])$','',title).strip()
            relative=name.split('/',1)[1]
            snapshots.append(dict(archetype=title,**parse(raw.decode('utf-8-sig')),source=f'https://github.com/anonfansub/ygodecks/blob/{COMMIT}/'+quote(relative),sourceSha256=hashlib.sha256(raw).hexdigest()))
    def named_counts(items):
        out=[]
        for name,count in items:
            if norm(name) not in by_name:raise ValueError('Missing recipe card: '+name)
            out.extend([by_name[norm(name)]]*count)
        return out
    snapshots.append(dict(archetype='Kewl Tune',formats=['TCG'],source='Ultra Duel basic Kewl Tune recipe; card effects verified in the installed database',
        main=named_counts([('Kewl Tune Reco',3),('Kewl Tune Cue',3),('Kewl Tune Clip',3),('Kewl Tune Mix',3),('JJ "Kewl Tune"',3),('Kewl Tune Synchro',3),('Kewl Tune Playlist',2),('Effect Veiler',3),('Ghost Ogre & Snow Rabbit',3),('D.D. Crow',3),('Skull Meister',3),('Book of Moon',3),('Cosmic Cyclone',3),('Compulsory Evacuation Device',2)]),
        extra=named_counts([('Kewl Tune RS',3),('Kewl Tune Loudness War',3),('Kewl Tune Track Maker',3),('Kewl Tune Remix',3),('Kewl Tune B2B',1),('Kewl Tune Crackle',1)]),side=[]))
    snapshots.append(dict(archetype='Kewl Tune',formats=['Genesys 100','Genesys 0'],
        source='Ultra Duel basic Genesys Kewl Tune recipe; low-cost Tuners support the hand Synchro mechanic',
        main=named_counts([('Kewl Tune Reco',3),('Kewl Tune Cue',3),('Kewl Tune Clip',3),('Kewl Tune Mix',3),('JJ "Kewl Tune"',3),('Kewl Tune Synchro',2),('Kewl Tune Playlist',2),('Hanewata',3),('Wattfox',3),('Wattberyx',3),('Tuning Gum',3),('Mystical Space Typhoon',3),('Lost Wind',3),('Compulsory Evacuation Device',3)]),
        extra=named_counts([('Kewl Tune RS',3),('Kewl Tune Loudness War',3),('Kewl Tune Track Maker',3),('Kewl Tune Remix',3)]),side=[]))
    for filename,title in [('AI_Ryzeal.ydk','Ryzeal'),('AI_Maliss.ydk','Maliss'),('AI_Yubel.ydk','Yubel')]:
        path=ROOT/'Data/Windbot/Decks'/filename
        if path.exists():snapshots.append(dict(archetype=title,**parse(path.read_text(encoding='utf-8-sig')),source='Bundled WindBot archetype recipe: '+filename))
    fillers=['Compulsory Evacuation Device','Mystical Space Typhoon','Forbidden Chalice','Lost Wind','Dimensional Prison','Sakuretsu Armor','Smashing Ground','Fissure','Bottomless Trap Hole','Enemy Controller']
    filler_ids=[by_name[norm(name)] for name in fillers if norm(name) in by_name]

    def adapt(recipe,mode,cap):
        theme=theme_ids(recipe)
        original=Counter(recipe['main'])
        # Named one-of monsters can be essential too (for example Exodia pieces).
        # Never remove those merely because a source recipe uses a single copy.
        core={code for code,count in original.items() if code in cards and
            ((code in theme and (count>=2 or cards[code]['type']&1)) or cards[code]['type']&0x80)}
        # Ritual and Fusion recipes must retain their named summoning spells.
        core.update(code for code in recipe['main'] if code in theme and code in cards and (cards[code]['type']&0x80 or ('ritual summon' in cards[code]['text'].lower() and cards[code]['type']&2)))
        if mode!='TCG' and any(cards[code]['type']&EXCLUDED for code in core):
            return None,'This recipe needs Pendulum or Link Monsters, which Genesys does not allow.'
        if mode!='TCG' and cap==0 and any(cost(code)>0 for code in core):
            names=', '.join(cards[code]['name'] for code in sorted(core) if cost(code)>0)
            return None,'The starter engine has pointed cards: '+names+'. No zero-point recipe is included.'
        if any(not allowed(code,mode,'main') for code in core):
            return None,'A required engine card is unavailable in this format or image catalog.'
        main=[];extra=[];used=Counter()
        for part,out in [('main',main),('extra',extra)]:
            for code in recipe[part]:
                if not allowed(code,mode,part):continue
                limit=3 if mode!='TCG' else min(3,limits.get(key(code),3),limits.get(code,3))
                if used[key(code)]<limit and len(out)<(60 if part=='main' else 15):out.append(code);used[key(code)]+=1
        if not core.issubset(main):return None,'A required engine card is forbidden by the TCG list.'
        original_bosses={code for code in recipe['extra'] if code in theme and code in cards}
        bosses=original_bosses or {code for code in recipe['extra'] if code in cards}
        if bosses and not any(code in bosses for code in extra):
            return None,'This strategy has no usable Extra Deck payoff under these rules.'
        def removable(code,part):
            if part=='main' and code in core and main.count(code)<=1:return False
            if part=='extra' and code in bosses and sum(c in bosses for c in extra)<=1:return False
            return True
        if mode!='TCG':
            while sum(cost(code) for code in main+extra)>cap:
                options=[(code,part) for part,arr in [('main',main),('extra',extra)] for code in arr if cost(code)>0 and removable(code,part)]
                if not options:return None,'The required starter engine exceeds the '+str(cap)+'-point budget.'
                code,part=max(options,key=lambda pair:(pair[1]=='extra',(pair[0] not in theme),cost(pair[0]),pair[0]))
                (main if part=='main' else extra).remove(code);used[key(code)]-=1
        if len(main)<min(28,int(len(recipe['main'])*.65)):
            return None,'Too much of the source engine would be removed to make a coherent basic deck.'
        if len(main)>40:
            # Keep the source's original legal 40–60 card plan rather than arbitrarily trimming it.
            target=len(main)
        else:target=40
        thematic=[code for code,count in original.most_common() if code in theme and count>=2]
        added=[]
        for pool,maximum in [(thematic,40),(filler_ids,12)]:
            count=0
            while len(main)<target and count<maximum:
                candidates=[]
                for code in pool:
                    if not allowed(code,mode,'main'):continue
                    limit=3 if mode!='TCG' else min(3,limits.get(key(code),3),limits.get(code,3))
                    if used[key(code)]>=limit:continue
                    if mode!='TCG' and sum(cost(c) for c in main+extra)+cost(code)>cap:continue
                    candidates.append(code)
                if not candidates:break
                code=min(candidates,key=lambda c:(main.count(c),-original[c],pool.index(c)))
                main.append(code);used[key(code)]+=1;added.append(code);count+=1
        if len(main)<40:return None,'Not enough legal engine cards remain for a full starter deck.'
        if sum(code in theme for code in main)<min(12,sum(code in theme for code in recipe['main'])):
            return None,'The archetype core would be too small after applying this format.'
        total=sum(cost(code) for code in main+extra)
        errors=[]
        if not 40<=len(main)<=60 or len(extra)>15:errors.append('size')
        if any(n>(3 if mode!='TCG' else limits.get(code,3)) for code,n in used.items()):errors.append('copies')
        if mode!='TCG' and total>cap:errors.append('points')
        if errors:raise ValueError(recipe['archetype']+': '+str(errors))
        removed=Counter(recipe['main']+recipe['extra'])-Counter(main+extra)
        return dict(main=main,extra=extra,side=[],points=total if mode!='TCG' else 0,
            preservedMain=sum((Counter(main)&original).values()),sourceMain=len(recipe['main']),
            added=dict(Counter(added)),removed=dict(removed)),''

    grouped={}
    for recipe in snapshots:grouped.setdefault(recipe['archetype'],[]).append(recipe)
    entries=[];out=ROOT/'Data/PremadeDecks';out.mkdir(parents=True,exist_ok=True)
    for archetype,recipes in sorted(grouped.items(),key=lambda p:p[0].casefold()):
        for mode,cap,label in [('TCG',100,'TCG'),('Genesys',100,'Genesys 100'),('Genesys',0,'Genesys 0')]:
            variants=[];reasons=[]
            for recipe in recipes:
                if recipe.get('formats') and label not in recipe['formats']:continue
                variant,reason=adapt(recipe,mode,cap)
                if variant:variants.append((variant,recipe))
                else:reasons.append(reason)
            slug=re.sub(r'[^a-z0-9]+','-',archetype.casefold()).strip('-')
            identifier=slug+'-'+label.lower().replace(' ','-')
            row=dict(id=identifier,archetype=archetype,format=mode,cap=cap,label=label,available=bool(variants))
            if variants:
                variant,recipe=max(variants,key=lambda p:(p[0]['preservedMain']/max(1,p[0]['sourceMain']),-len(p[0]['added']),len(p[0]['extra'])))
                filename=identifier+'.ydk';text=f'#created by Ultra Duel\n#ultra-format:{mode}:{cap}\n'
                for heading,part in [('#main','main'),('#extra','extra'),('!side','side')]:text+=heading+'\n'+''.join(str(code)+'\n' for code in variant[part])
                (out/filename).write_bytes(text.encode('utf-8'))
                themed=theme_ids(recipe)
                covers=list(dict.fromkeys([code for code in variant['extra']+variant['main'] if code in themed]))[:3]
                if not covers:covers=list(dict.fromkeys(variant['main']))[:3]
                row.update(file='PremadeDecks/'+filename,points=variant['points'],mainCount=len(variant['main']),extraCount=len(variant['extra']),covers=covers,
                    source=recipe['source'],description='A basic archetype recipe for learning its engine. Save a copy to customize it.',
                    sha256=hashlib.sha256(text.encode()).hexdigest(),adaptation={k:variant[k] for k in ('preservedMain','sourceMain','added','removed')})
            else:
                row.update(reason=reasons[0] if reasons else 'No suitable starter recipe is available.',covers=list(dict.fromkeys(c for c in recipes[0]['extra']+recipes[0]['main'] if c in has_art))[:3])
            entries.append(row)
    result=dict(version=1,rules=json.loads((ROOT/'Data/format-data-sources.json').read_text(encoding='utf-8')),
        source=SOURCE,sourceCommit=COMMIT,archetypeCount=len(grouped),entries=entries)
    (ROOT/'Data/premade-decks.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    (out/'source-recipes.json').write_text(json.dumps(snapshots,indent=2)+'\n',encoding='utf-8')
    published={Path(row['file']).name for row in entries if row['available']}
    for file in out.glob('*.ydk'):
        if file.name not in published and file.read_bytes().startswith(b'#created by Ultra Duel\n#ultra-format:'):
            file.unlink()
    print('ARCHETYPES',len(grouped),'RECIPES',len(snapshots))
    for label in ('TCG','Genesys 100','Genesys 0'):
        rows=[r for r in entries if r['label']==label]
        print(label,':',sum(r['available'] for r in rows),'available /',len(rows))
    print('KEWL TUNE',json.dumps([r for r in entries if r['archetype']=='Kewl Tune']))

if __name__=='__main__':main()
