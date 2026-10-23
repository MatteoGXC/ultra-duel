"""Validate/reproduce published Solo lists without changing a single card.

The checked-in source snapshots contain exact Main/Extra/Side arrays from public
Konami Neuron recipes. Updating a recipe requires an explicit new source snapshot;
this tool never repairs, fills, trims, or optimizes a list.
"""
import collections
import hashlib
import json
from pathlib import Path
import sqlite3

ROOT = Path(__file__).resolve().parents[1]

def validate(row, cards, limits, points):
    errors = []
    parts = [row[k] for k in ('main', 'extra', 'side')]
    ids = sum(parts, [])
    if not 40 <= len(parts[0]) <= 60 or any(len(p) > 15 for p in parts[1:]): errors.append('deck size')
    if any(i not in cards for i in ids): return ['unknown card'], 0
    counts = collections.Counter(cards[i][2] or i for i in ids)
    genesys = row['mode'] == 'Genesys'
    if any(n > (3 if genesys else limits.get(i, 3)) for i, n in counts.items()): errors.append('copy limit')
    if any(not cards[i][1] & 2 for i in ids): errors.append('non-TCG card')
    if any(cards[i][3] & 0x4000 for i in ids): errors.append('token')
    if any(cards[i][3] & 0x4802040 for i in parts[0]) or any(not cards[i][3] & 0x4802040 for i in parts[1]): errors.append('wrong section')
    if genesys and any(cards[i][3] & 0x5000000 for i in ids): errors.append('Link/Pendulum')
    total = sum(points.get(cards[i][2] or i, 0) for i in ids)
    if genesys and total > row['cap']: errors.append('point cap')
    return errors, total

def main():
    db = sqlite3.connect(ROOT / 'Data/locales/en-US/cards.cdb')
    cards = {r[0]: r for r in db.execute('select id,ot,alias,type from datas')}
    limits, points = {}, {}
    started = False
    for line in (ROOT / 'Data/lflist.conf').read_text(encoding='utf-8-sig').splitlines():
        if line.startswith('!'):
            if started: break
            started = True
        p = line.split()
        if started and p and p[0].isdigit(): limits[int(p[0])] = int(p[1])
    for line in (ROOT / 'Data/lflist_genesys.conf').read_text(encoding='utf-8-sig').splitlines():
        p = line.split()
        if p and p[0].isdigit() and len(p)>2 and p[1] == '$genesys': points[int(p[0])] = int(p[2])
    catalog = json.loads((ROOT / 'Data/solo-published-decks.json').read_text(encoding='utf-8'))
    executors = {line.split('|')[0] for line in (ROOT / 'Data/solo-executors.conf').read_text().splitlines() if '|' in line}
    audit = []
    for row in catalog['decks']:
        if row['executor'] not in executors: raise ValueError('Unknown executor: ' + row['executor'])
        if row['mode'] not in ('TCG', 'Genesys') or row['difficulty'] not in ('Casual', 'Standard', 'Hard'):
            raise ValueError('Invalid mode/difficulty: ' + row['name'])
        errors, total = validate(row, cards, limits, points)
        if errors: raise ValueError(row['name'] + ': ' + ', '.join(errors))
        if total != row['points']: raise ValueError('Point snapshot changed: ' + row['name'])
        text = '# Published list: ' + row['source'] + '\n# Author title: ' + row['title'] + '\n'
        for heading, key in [('#main','main'), ('#extra','extra'), ('!side','side')]:
            text += heading + '\n' + ''.join(str(i)+'\n' for i in row[key])
        path = ROOT / 'Data' / row['file']
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding='utf-8')
        audit.append(dict(file=row['file'], mode=row['mode'], points=total,
                          source=row['source'], difficulty=row['difficulty'],
                          sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
    (ROOT / 'Data/solo-decks-audit.json').write_text(json.dumps({'decks':audit}, indent=2)+'\n', encoding='utf-8')
    print(f'{len(audit)} published lists validated and reproduced unchanged.')

if __name__ == '__main__': main()
