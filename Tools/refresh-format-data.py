"""Generate offline format lists from Konami's current English rule pages."""
import datetime, html.parser, json, pathlib, re, sqlite3, unicodedata, urllib.request, urllib.parse

ROOT = pathlib.Path(__file__).resolve().parents[1]
DATA = ROOT / 'Data'

class Tables(html.parser.HTMLParser):
    def __init__(self):
        super().__init__(); self.rows=[]; self.row=None; self.cell=None; self.title=[]; self.in_title=False
    def handle_starttag(self, tag, attrs):
        if tag == 'tr': self.row=[]
        if tag in ('td','th') and self.row is not None: self.cell=[]
        if tag == 'title': self.in_title=True
    def handle_data(self, data):
        if self.cell is not None: self.cell.append(data)
        if self.in_title: self.title.append(data)
    def handle_endtag(self, tag):
        if tag in ('td','th') and self.cell is not None:
            self.row.append(' '.join(''.join(self.cell).split())); self.cell=None
        if tag == 'tr' and self.row is not None:
            self.rows.append(self.row); self.row=None
        if tag == 'title': self.in_title=False

def fetch(url):
    with urllib.request.urlopen(url, timeout=40) as r: text=r.read().decode('utf-8')
    parser=Tables(); parser.feed(text)
    return parser, text

def canonical(name):
    return ' '.join(unicodedata.normalize('NFKC',name).replace('’',"'").replace('–','-').replace('—','-').upper().split())

db=sqlite3.connect(DATA / 'locales' / 'en-US' / 'cards.cdb')
lookup={}
for card_id, name in db.execute('SELECT id,name FROM texts'):
    lookup.setdefault(canonical(name), []).append(card_id)
db.close()
unmatched={'genesys':[], 'tcg':[]}

genesys_url='https://www.yugioh-card.com/en/genesys/'
points,_=fetch(genesys_url)
point_rows=[(r[0],int(r[1])) for r in points.rows if len(r)>=2 and r[1].isdigit()]
if len(point_rows)<100: raise RuntimeError('Unexpected Genesys table; old data has been preserved.')
genesys=['# Official Konami Genesys points', '# '+genesys_url, '!Genesys · Official TCG · 100 points', '$genesys 100']
for name, value in point_rows:
    ids=lookup.get(canonical(name),[])
    if not ids: unmatched['genesys'].append(name)
    for card_id in ids: genesys.append(f'{card_id} $genesys {value} # {name}')

_,limited_index=fetch('https://www.yugioh-card.com/en/limited/')
links=re.findall(r'href=["\']([^"\']*/limited/list_[^"\']+)', limited_index)
links=[u for u in links if re.search(r'/list_\d{4}-\d{2}-\d{2}/*$',u)]
if not links: raise RuntimeError('Current TCG list link not found; old data has been preserved.')
tcg_url=urllib.parse.urljoin('https://www.yugioh-card.com',links[0])
tcg,tcg_html=fetch(tcg_url)
date_match=re.search(r'list_(\d{4}-\d{2}-\d{2})',tcg_url)
if not date_match: raise RuntimeError('TCG effective date unavailable.')
effective=date_match.group(1)
limits={'Forbidden':0,'Limited':1,'Semi-Limited':2,'Semi-limited':2,'Unlimited':3}
tcg_rows=[(r[1],limits[r[2]]) for r in tcg.rows if len(r)>=3 and r[2] in limits]
if len(tcg_rows)<80: raise RuntimeError('Unexpected TCG table; old data has been preserved.')
# This release-specific notice defers a change until the card's release.
deferred={}
if 'Perfectron Hydradrive Dragon' in tcg_html and datetime.date.today()<datetime.date(2026,11,11):
    previous_links=re.findall(r'href=["\']([^"\']*/limited/list_[^"\']+)',tcg_html)
    previous_links=[u for u in previous_links if re.search(r'/list_\d{4}-\d{2}-\d{2}/*$',u) and effective not in u]
    if previous_links:
        previous_url=urllib.parse.urljoin(tcg_url,previous_links[0])
        previous_url=re.sub(r'(?<!:)//+', '/', previous_url)
        previous,_=fetch(previous_url)
        deferred={canonical(r[1]):limits[r[2]] for r in previous.rows if len(r)>=3 and r[2] in limits}
        tcg_rows=[(name,deferred.get(canonical(name),3) if canonical(name)=='PERFECTRON HYDRADRIVE DRAGON' else value) for name,value in tcg_rows]
tcg_lines=['# Official Konami TCG Advanced snapshot', '# '+tcg_url, '!TCG Advanced · '+effective]
for name,value in tcg_rows:
    ids=lookup.get(canonical(name),[])
    if not ids: unmatched['tcg'].append(name)
    for card_id in ids: tcg_lines.append(f'{card_id} {value} # {name}')

def atomic(path,text):
    temp=path.with_suffix(path.suffix+'.tmp'); temp.write_text(text,encoding='utf-8'); temp.replace(path)

atomic(DATA/'lflist_genesys.conf','\n'.join(genesys)+'\n')
old=(DATA/'lflist.conf').read_text(encoding='utf-8-sig')
marker='# END GENERATED TCG SNAPSHOT\n'
if marker in old: old=old.split(marker,1)[1]
atomic(DATA/'lflist.conf','\n'.join(tcg_lines)+'\n'+marker+old)
metadata={'downloadedAt':datetime.datetime.now(datetime.timezone.utc).isoformat(),
          'tcgEffectiveDate':effective,'sources':{'tcg':tcg_url,'genesys':genesys_url},
          'genesysPointRows':len(point_rows),'tcgRows':len(tcg_rows),
          'unmatchedCardsNotInLocalDatabase':unmatched}
atomic(DATA/'format-data-sources.json',json.dumps(metadata,indent=2,ensure_ascii=False)+'\n')
print(json.dumps({'genesysPointRows':len(point_rows),'tcgRows':len(tcg_rows),'tcgEffectiveDate':effective,'unmatched':{k:len(v) for k,v in unmatched.items()}}))
