"""Package the tested portable player without personal decks/accounts or debug data."""
import concurrent.futures
import hashlib
import json
import os
import argparse
from pathlib import Path
import zipfile
import zlib

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'release/MDPro3-TCG-Genesys'
OUTPUT = ROOT.parent / 'release-work/windows-v0.1.0'
LIMIT = 1400 * 1024 * 1024  # Below GitHub's per-asset limit, even without compression.

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--resume', action='store_true')
    parser.add_argument('--crt-dir', type=Path, required=True, help='Visual Studio redistributable x64 CRT directory')
    args = parser.parse_args()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    entries = []
    roots = ['Matteo-Duel.exe', 'UnityPlayer.dll', 'UnityCrashHandler64.exe',
             'MonoBleedingEdge', 'D3D12', 'Matteo-Duel_Data', 'Data',
             'Expansions', 'Picture/Art', 'Picture/Closeup', 'LICENSE', 'README.txt', 'build-info.json']
    excluded = {'Data/config.conf', 'Data/mycard_apps.json'}
    for name in roots:
        base = SOURCE / name
        files = [base] if base.is_file() else (Path(p)/f for p, ds, fs in os.walk(base) for f in fs)
        for path in files:
            rel = path.relative_to(SOURCE).as_posix()
            if rel in excluded or rel.startswith(('Data/Windbot/Decks/TCG/', 'Data/Windbot/Decks/Genesys/')): continue
            if path.suffix.lower() in ('.log', '.bak', '.tmp'): continue
            entries.append((path, rel, path.stat().st_size))
    # Only the three project starter decks; never copy the owner's personal library.
    for path in sorted((ROOT/'Deck').glob('Starter - *.ydk')):
        entries.append((path, 'Deck/'+path.name, path.stat().st_size))
    entries.sort(key=lambda x: x[1])
    groups = [[]]
    size = 0
    for item in entries:
        if size + item[2] > LIMIT:
            groups.append([]); size = 0
        groups[-1].append(item); size += item[2]
    crt = [(p, p.name, p.stat().st_size) for p in sorted(args.crt_dir.glob('*.dll'))]
    if not any(p[1] == 'vcruntime140.dll' for p in crt): raise ValueError('Missing redistributable x64 CRT')
    quickstart = OUTPUT/'README-FIRST.txt'
    quickstart.write_text('Ultra Duel for Windows\n\nRun Matteo-Duel.exe from this folder. Keep all runtime and asset folders together.\n'
        'My Decks opens your library; each deck has its own saved format. Solo offers TCG and Genesys practice.\n'
        'Solo works offline. Remote multiplayer hosting is not ready: the current room server accepts only localhost connections.\n'
        'Source, license, updates: https://github.com/MatteoGXC/ultra-duel\n', encoding='utf-8')
    crt.append((quickstart, quickstart.name, quickstart.stat().st_size))
    groups.append(crt)
    entries.extend(crt)
    config = ('Language->en-US\nCardLanguage->en-US\nPrereleaseLanguage->en-US\n'
              'DuelFormat->TCG\nGenesysPointCap->100\nDeckInUse->Starter - TCG\nDuelPlayerName0->Player\n')
    def package(pair):
        index, files = pair
        name = f'Matteo-Duel-Windows-{index+1:02}.zip'
        path = OUTPUT/name
        if args.resume and path.exists():
            with zipfile.ZipFile(path) as archive:
                expected = {}
                for source, rel, size in files:
                    crc = 0
                    with source.open('rb') as stream:
                        for block in iter(lambda:stream.read(4*1024*1024), b''): crc = zlib.crc32(block, crc)
                    expected[rel] = (size, crc)
                if index == 0: expected['Data/config.conf'] = (len(config.encode()), zlib.crc32(config.encode()))
                if {e.filename: (e.file_size, e.CRC) for e in archive.infolist()} != expected:
                    raise ValueError('Existing archive does not match this package: '+name)
        else:
            with zipfile.ZipFile(path, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=1, allowZip64=True) as archive:
                if index == 0: archive.writestr('Data/config.conf', config)
                for source, rel, _ in files: archive.write(source, rel)
        digest = hashlib.sha256()
        with path.open('rb') as stream:
            for block in iter(lambda: stream.read(4*1024*1024), b''): digest.update(block)
        digest = digest.hexdigest()
        print(name, path.stat().st_size, digest, flush=True)
        return dict(name=name, size=path.stat().st_size, sha256=digest,
                    url='https://github.com/MatteoGXC/ultra-duel/releases/download/v0.1.0/'+name)
    with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
        parts = list(pool.map(package, enumerate(groups)))
    manifest = dict(version='0.1.0', buildCommit=json.loads((SOURCE/'build-info.json').read_text())['commit'],
                    installedBytes=sum(i[2] for i in entries), fileCount=len(entries)+1, parts=parts)
    (OUTPUT/'windows-manifest.json').write_text(json.dumps(manifest, indent=2)+'\n', encoding='utf-8')
    print('PACKAGE_COMPLETE', manifest['fileCount'], 'files',len(parts),'archives',flush=True)

if __name__ == '__main__': main()
