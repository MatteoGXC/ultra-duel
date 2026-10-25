"""Cumulative Ultra Duel update against the original public Windows build."""
import hashlib
from datetime import datetime, timezone
import json
import subprocess
import zipfile
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT.parent / 'release-work/windows-v0.1.0'
VERSION = '0.1.9'
OUT = ROOT.parent / ('release-work/windows-v'+VERSION)
PLAYER = ROOT / 'release/Solo-mode-update'

def sha(path):
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(4*1024*1024), b''):
            digest.update(block)
    return digest.hexdigest()

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    commit = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip()
    info_path = PLAYER/'build-info.json'
    info = json.loads(info_path.read_text())
    info.update(version=VERSION, commit=commit, builtAt=datetime.now(timezone.utc).isoformat(),
        assemblySha256=sha(PLAYER/'Matteo-Duel_Data/Managed/Assembly-CSharp.dll'),
        nativeServerSha256=sha(PLAYER/'Matteo-Duel_Data/Plugins/x86_64/ygoserver.dll'))
    info['checks'].update(windows017Menus='passed', nativeUnevenTag='Windows and Linux TCP tests passed')
    info_path.write_text(json.dumps(info, indent=2)+'\n')
    (PLAYER/'Data/installed-version.json').write_text(json.dumps(dict(version=VERSION, sourceCommit=commit))+'\n')
    baseline = {}
    for path in sorted(BASE.glob('Matteo-Duel-Windows-*.zip')):
        with zipfile.ZipFile(path) as archive:
            baseline.update({e.filename: (e.file_size, e.CRC) for e in archive.infolist()})
    if not baseline:
        raise RuntimeError('Original release archives are required for a cumulative patch.')
    names = ['Matteo-Duel.exe', 'Ultra-Duel.exe', 'UnityPlayer.dll', 'UnityCrashHandler64.exe',
             'build-info.json', 'Data/installed-version.json', 'Matteo-Duel_Data', 'MonoBleedingEdge', 'D3D12',
             'Update-Ultra-Duel.ps1', 'Update-Ultra-Duel.cmd',
             'Update-Matteo-Duel.ps1', 'Update-Matteo-Duel.cmd']
    names += ['Data/script.zip', 'Data/lflist.conf', 'Data/lflist_genesys.conf',
              'Data/format-data-sources.json', 'Data/card-update-audit.json',
              'Data/pack/zz-ultra-tcg.db', 'Data/ultra-services.json', 'Data/burst-protocol-index.json',
              'Data/solo-published-decks.json', 'Data/solo-decks-audit.json', 'Data/shop-catalog.json', 'Data/card-archetype-audit.json']
    names += [p.relative_to(PLAYER).as_posix() for p in (PLAYER/'Data/locales').glob('*/cards.cdb')]
    names += [p.relative_to(PLAYER).as_posix() for p in (PLAYER/'Data/locales').glob('*/strings.conf')]
    names += ['Data/locales/en-US/translation.conf']
    names += ['Data/betb-update-audit.json','Data/beyond-the-brave-index.json']
    if (ROOT/'Data/beyond-the-brave-index.json').exists():
        names += ['Picture/Art/'+str(row['id'])+'.jpg' for row in json.loads((ROOT/'Data/beyond-the-brave-index.json').read_text(encoding='utf-8')) if (PLAYER/'Picture/Art'/f"{row['id']}.jpg").exists()]
    audit = json.loads((ROOT/'Data/card-update-audit.json').read_text(encoding='utf-8'))
    names += ['Picture/Art/'+str(row['id'])+'.jpg' for row in audit['art']
              if (PLAYER/'Picture/Art'/f"{row['id']}.jpg").is_file()]
    if (PLAYER/'Expansions/ultra-card-art.zip').exists(): names += ['Expansions/ultra-card-art.zip']
    changed = []
    for name in names:
        path = PLAYER / name
        for source in ([path] if path.is_file() else path.rglob('*')):
            if not source.is_file():
                continue
            relative = source.relative_to(PLAYER).as_posix()
            # Master Duel runtime content is unchanged; freshly built menu
            # dependencies must accompany the new Addressables catalog.
            if relative.startswith('Matteo-Duel_Data/StandaloneWindows64/') and not relative.startswith('Matteo-Duel_Data/StandaloneWindows64/MDPro3/'):
                continue
            if relative.endswith(('.pdb', '.mdb')):
                continue
            crc = 0
            with source.open('rb') as stream:
                for block in iter(lambda: stream.read(4*1024*1024), b''):
                    crc = zlib.crc32(block, crc)
            if baseline.get(relative) != (source.stat().st_size, crc):
                changed.append((source, relative))
    archive_path = OUT / 'Ultra-Duel-Update.zip'
    with zipfile.ZipFile(archive_path, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
        for source, relative in changed:
            archive.write(source, relative)
    commit = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip()
    manifest = dict(version=VERSION, minimumVersion='0.1.0', sourceCommit=commit,
                    asset=archive_path.name, sha256=sha(archive_path),
                    files=[dict(path=relative, sha256=sha(source)) for source, relative in changed])
    (OUT/'update-manifest.json').write_text(json.dumps(manifest, indent=2)+'\n')
    windows = json.loads((BASE/'windows-manifest.json').read_text())
    windows.update(version=VERSION, buildCommit=commit)
    for part in windows['parts']:
        part['url'] = part['url'].replace('/matteo-duel/', '/ultra-duel/')
    windows['parts'].append(dict(name=archive_path.name, size=archive_path.stat().st_size,
        sha256=manifest['sha256'],
        url='https://github.com/MatteoGXC/ultra-duel/releases/download/v'+VERSION+'/'+archive_path.name))
    windows['installedBytes'] += sum(p.stat().st_size for p, _ in changed)
    (OUT/'windows-manifest.json').write_text(json.dumps(windows, indent=2)+'\n')
    with zipfile.ZipFile(OUT/'Ultra-Duel-Updater.zip','w',zipfile.ZIP_DEFLATED) as archive:
        for name in ('Update-Ultra-Duel.ps1','Update-Ultra-Duel.cmd'):
            archive.write(ROOT/'Tools'/name, name)
        archive.writestr('README.txt', 'Ultra Duel updater\n\nExtract beside Matteo-Duel.exe in your existing game folder.\nClose the game, then double-click Update-Ultra-Duel.cmd.\nOnly changed game files download. Your decks and settings are preserved.\n')
    print('PATCH_READY', len(changed), 'files;', round(archive_path.stat().st_size/1048576, 1), 'MB')

if __name__ == '__main__':
    main()
