"""Build curated Solo practice opponents, separately from unchanged Neuron recipes.

Existing archetype recipes retain their provenance. Branded Dracotail recipes
are local practice builds using the installed card texts and current format data.
No personal player decks are read. Re-run after changing the bundled banlist.
"""
from pathlib import Path
import collections,importlib.util,json,sqlite3,hashlib
ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('published',ROOT/'Tools/import-published-solo.py')
published=importlib.util.module_from_spec(spec);spec.loader.exec_module(published)
def parse(path):
    parts={'main':[],'extra':[],'side':[]};section=None
    for line in path.read_text(encoding='utf-8-sig').splitlines():
        if line in ('#main','#extra','!side'):section=line.lstrip('#!')
        elif line.isdigit() and section:parts[section].append(int(line))
    return parts

def main():
    db=sqlite3.connect(ROOT/'Data/locales/en-US/cards.cdb')
    cards={r[0]:r for r in db.execute('select id,ot,alias,type from datas')}
    names={r[0]:r[1] for r in db.execute('select id,name from texts')}
    ids={names[i]:i for i in cards if not cards[i][2] and cards[i][1]&2}
    limits={};started=False
    for line in (ROOT/'Data/lflist.conf').read_text(encoding='utf-8-sig').splitlines():
        if line.startswith('!'):
            if started:break
            started=True
        p=line.split()
        if started and p and p[0].isdigit():limits[int(p[0])]=int(p[1])
    points={}
    for line in (ROOT/'Data/lflist_genesys.conf').read_text(encoding='utf-8-sig').splitlines():
        p=line.split()
        if len(p)>2 and p[0].isdigit() and p[1]=='$genesys':points[int(p[0])]=int(p[2])
    rows=[]
    # These are already validated, coherent archetype builds in the shipped library.
    executors={'Albaz':'Albaz','Kashtira':'Kashtira','Maliss':'Maliss','Salamangreat':'Salamangreat',
               'Sky Striker':'SkyStriker','Tearlaments':'Tearlaments','Thunder Dragon':'ThunderDragon',
               'Horus':'Horus','Orcust':'Orcust','Altergeist':'Altergeist','Mathmech':'MathMech'}
    for entry in json.loads((ROOT/'Data/premade-decks.json').read_text(encoding='utf8'))['entries']:
        if entry['archetype'] not in executors or not entry['available']:continue
        # A Link strategy with its payoffs removed is not a useful AI opponent.
        if entry['archetype'] in ('Salamangreat','Orcust') and entry['format']!='TCG':continue
        parts=parse(ROOT/'Data'/entry['file'])
        extra=parts['extra'];banner=next((i for i in entry['covers'] if i in extra), extra[0] if extra else entry['covers'][0])
        row=dict(name='Branded Despia' if entry['archetype']=='Albaz' else entry['archetype'],executor=executors[entry['archetype']],
                 difficulty='Hard',mode=entry['format'],cap=entry['cap'],points=entry['points'],source=entry['source'] if entry['source'].startswith('https://') else '',
                 title='Curated archetype practice',file='Windbot/Decks/Practice/'+entry['id']+'.ydk',banner=banner,
                 note='Curated archetype practice list; adapted for this format. Dedicated AI with compatibility support.',**parts)
        rows.append(row)
    def expand(recipe):
        return [ids[name] for name,count in recipe for _ in range(count)]
    draco=[('Dracotail Lukias',3),('Dracotail Faimena',3),('Dracotail Mululu',2),('Dracotail Pan',1),('Dracotail Urgula',1),('Dracotail Phryxul',1),
           ('Ketu Dracotail',3),('Rahu Dracotail',3),('Dracotail Horn',2),('Dracotail Flame',1)]
    branded=[('Fallen of Albaz',2),('Aluber the Jester of Despia',3),('Guiding Quem, the Virtuous',1),('Blazing Cartesia, the Virtuous',1),
             ('Despian Tragedy',1),('Branded Fusion',1),('Branded Opening',3),('Branded Lost',1),('Branded in Red',1)]
    extra=[('Dracotail Arthalion',3),('Dracotail Gulamel',2),('Dracotail Shaulas',1),('Mirrorjade the Iceblade Dragon',2),('Lubellion the Searing Dragon',1),
           ('Albion the Branded Dragon',2),('Granguignol the Dusk Dragon',1),('Rindbrumm the Striking Dragon',1),('Guardian Chimera',1),('Titaniklad the Ash Dragon',1)]
    staples=[('Ash Blossom & Joyous Spring',3),('Infinite Impermanence',3),('Effect Veiler',3),('Droll & Lock Bird',3),('Fusion Deployment',3),('Super Polymerization',3),('Triple Tactics Talent',3),('Called by the Grave',1),("Harpie's Feather Duster",1),('Book of Moon',3),('Raigeki',3)]
    for slug,name,engine in [
        ('branded-dracotail','Branded Dracotail',draco+branded),
        ('branded-dracotail-dogmatika','Branded Dracotail Dogmatika',draco+branded+[('Dogmatika Ecclesia, the Virtuous',1),('Dogmatika Fleurdelis, the Knighted',1),('Nadir Servant',2)]),
        ('dracotail','Dracotail',draco+[('Dracotail Mululu',1),('Dracotail Pan',1),('Dracotail Phryxul',1),('Dracotail Urgula',1),('Dracotail Sting',1),('Super Polymerization',3)])]:
        counts=collections.Counter();maincards=[];changes=[]
        for i in expand(engine+staples):
            if len(maincards)>=40:break
            canonical=cards[i][2] or i
            if counts[canonical]>=limits.get(canonical,3):changes.append(names[i]);continue
            counts[canonical]+=1;maincards.append(i)
        assert len(maincards)==40,(slug,len(maincards))
        extras=[]
        for i in expand(extra):
            canonical=cards[i][2] or i
            if counts[canonical]>=limits.get(canonical,3):continue
            counts[canonical]+=1;extras.append(i)
        # Pure Dracotail uses hand/field Fusions; omit Albaz-only fusion bosses.
        if slug=='dracotail':extras=expand([('Dracotail Arthalion',2),('Dracotail Gulamel',3),('Dracotail Shaulas',2),('Guardian Chimera',2),('Mudragon of the Swamp',1),('Garura, Wings of Resonant Life',1)])
        rows.insert(0,dict(name=name,executor='Dracotail',difficulty='Hard',mode='TCG',cap=100,points=0,source='',title='Ultra Duel fusion practice',
                         file='Windbot/Decks/Practice/'+slug+'.ydk',banner=79755671 if slug=="dracotail" else 24915933 if slug.endswith("dogmatika") else 33760966,
                         note='Fusion combo practice. Search starters, turn Fusion materials into follow-up, and disrupt the opponent on their turn.',
                         main=maincards,extra=extras,side=[]))
    rows.sort(key=lambda r: {"Branded Dracotail":0,"Branded Dracotail Dogmatika":1,"Dracotail":2}.get(r["name"],3))
    audit=[]
    for row in rows:
        errors,total=published.validate(row,cards,limits,points)
        assert not errors,(row['name'],row['mode'],errors)
        row['points']=total
        file=ROOT/'Data'/row['file'];file.parent.mkdir(parents=True,exist_ok=True)
        text='# Ultra Duel curated AI practice\n'
        for head,part in [('#main','main'),('#extra','extra'),('!side','side')]:text+=head+'\n'+''.join(str(i)+'\n' for i in row[part])
        file.write_text(text,encoding='utf8')
        audit.append(dict(name=row['name'],mode=row['mode'],file=row['file'],sha256=hashlib.sha256(file.read_bytes()).hexdigest(),points=total))
    (ROOT/'Data/solo-practice-decks.json').write_text(json.dumps({'decks':rows},indent=2)+'\n',encoding='utf8')
    (ROOT/'Data/solo-practice-audit.json').write_text(json.dumps({'decks':audit},indent=2)+'\n',encoding='utf8')
    print(f'{len(rows)} legal curated opponents built; published Neuron snapshots unchanged.')
if __name__=='__main__':main()
