"""Decode every database card's actual illustration; closeups never count as art."""
import argparse
import concurrent.futures
import io
import json
from pathlib import Path
import sqlite3
import zipfile
from collections import Counter
from PIL import Image


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--runtime', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    root = args.runtime.resolve()
    source_file=root/'Data/card-image-source.json'
    source=json.loads(source_file.read_text(encoding='utf-8')) if source_file.exists() else {}
    source_only=source.get('provider')=='yugiohcardlist' and source.get('authoritative')
    source_rows={r['id']:r for r in source.get('cards',[])}
    cards = {}
    databases = [root/'Data/locales/en-US/cards.cdb', *sorted((root/'Expansions').glob('*.cdb'))]
    for database in databases:
        with sqlite3.connect(f'{database.as_uri()}?mode=ro', uri=True) as db:
            for code, name, kind, alias, ot in db.execute('select d.id,t.name,d.type,d.alias,d.ot from datas d join texts t on d.id=t.id'):
                cards[code] = dict(id=code, name=name, type=kind, alias=alias, ot=ot)
    archives = sorted([*(root/'Expansions').glob('*.zip'), *(root/'Expansions').glob('*.ypk')])
    zipped = {}
    for archive in archives:
        with zipfile.ZipFile(archive) as z:
            for name in z.namelist():
                normalized = name.lower().replace('\\', '/')
                if normalized.startswith(('art/', 'pics/')):
                    zipped[normalized] = (archive, name)
    files = {}
    for folder in ('Art', 'Art2'):
        directory = root/'Picture'/folder
        if not directory.exists():
            continue
        for file in directory.iterdir():
            if file.stem.isdigit() and file.suffix.lower() in ('.jpg', '.jpeg', '.png'):
                files[int(file.stem)] = file

    def check(card):
        code = card['id']
        result = dict(card)
        try:
            if source_only:
                full=root/f'Picture/FullCard/{code}.jpg'
                provenance=source_rows.get(code,{})
                if not full.exists() or provenance.get('status')!='ok':
                    result.update(status='missing',sourceUrl=provenance.get('url'))
                    return result
                body=full.read_bytes()
                import hashlib
                if hashlib.sha256(body).hexdigest()!=provenance.get('sha256'):
                    raise ValueError('Full card does not match its downloaded source hash')
                result.update(source=str(full.relative_to(root)),sourceUrl=provenance['url'])
            elif code in files:
                source = files[code]
                body = source.read_bytes()
                result['source'] = str(source.relative_to(root))
            else:
                match = next((zipped[f'{folder}/{code}.{extension}']
                    for folder in ('art', 'pics') for extension in ('png', 'jpg')
                    if f'{folder}/{code}.{extension}' in zipped), None)
                if match is None:
                    result['status'] = 'missing'
                    result['closeupExists'] = (root/f'Picture/Closeup/{code}.png').exists()
                    return result
                archive, entry = match
                with zipfile.ZipFile(archive) as z:
                    body = z.read(entry)
                result['source'] = str(archive.relative_to(root)) + ':' + entry
            with Image.open(io.BytesIO(body)) as im:
                im.load()
                result['size'] = list(im.size)
                result['status'] = 'ok'
                if min(im.size) < 64:
                    result['status'] = 'too-small'
                if 'A' in im.getbands() and im.getchannel('A').getextrema()[0] < 255:
                    result['status'] = 'transparent-illustration'
        except Exception as error:
            result.update(status='decode-error', error=str(error))
        return result

    rows = []
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
        for index, row in enumerate(pool.map(check, cards.values()), 1):
            rows.append(row)
            if index % 1000 == 0:
                print('DECODED', index, '/', len(cards), flush=True)
    summary = dict(total=len(rows), statuses=dict(Counter(r['status'] for r in rows)),
        tokens=sum(bool(r['type'] & 0x4000) for r in rows),
        problems=[r for r in rows if r['status'] != 'ok'])
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(dict(summary=summary, cards=rows), indent=2), encoding='utf-8')
    print(json.dumps({k:v for k,v in summary.items() if k!='problems'}, indent=2), flush=True)


if __name__ == '__main__':
    main()
