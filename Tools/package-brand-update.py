"""Cumulative Ultra Duel branding patch against the original public Windows build."""
import hashlib
import json
import subprocess
import zipfile
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT.parent / 'release-work/windows-v0.1.0'
OUT = ROOT.parent / 'release-work/windows-v0.1.1'
PLAYER = ROOT / 'release/Solo-mode-update'

def sha(path):
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(4*1024*1024), b''):
            digest.update(block)
    return digest.hexdigest()

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    baseline = {}
    for path in sorted(BASE.glob('Matteo-Duel-Windows-*.zip')):
        with zipfile.ZipFile(path) as archive:
            baseline.update({e.filename: (e.file_size, e.CRC) for e in archive.infolist()})
    if not baseline:
        raise RuntimeError('Original release archives are required for a cumulative patch.')
    names = ['Matteo-Duel.exe', 'Ultra-Duel.exe', 'UnityPlayer.dll', 'UnityCrashHandler64.exe',
             'build-info.json', 'Matteo-Duel_Data', 'MonoBleedingEdge', 'D3D12',
             'Update-Ultra-Duel.ps1', 'Update-Ultra-Duel.cmd',
             'Update-Matteo-Duel.ps1', 'Update-Matteo-Duel.cmd']
    changed = []
    for name in names:
        path = PLAYER / name
        for source in ([path] if path.is_file() else path.rglob('*')):
            if not source.is_file():
                continue
            relative = source.relative_to(PLAYER).as_posix()
            # These multi-gigabyte bundles are unchanged by a player recompile.
            if relative.startswith('Matteo-Duel_Data/StandaloneWindows64/'):
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
    manifest = dict(version='0.1.1', minimumVersion='0.1.0', sourceCommit=commit,
                    asset=archive_path.name, sha256=sha(archive_path),
                    files=[dict(path=relative, sha256=sha(source)) for source, relative in changed])
    (OUT/'update-manifest.json').write_text(json.dumps(manifest, indent=2)+'\n')
    windows = json.loads((BASE/'windows-manifest.json').read_text())
    windows.update(version='0.1.1', buildCommit=commit)
    for part in windows['parts']:
        part['url'] = part['url'].replace('/matteo-duel/', '/ultra-duel/')
    windows['parts'].append(dict(name=archive_path.name, size=archive_path.stat().st_size,
        sha256=manifest['sha256'],
        url='https://github.com/MatteoGXC/ultra-duel/releases/download/v0.1.1/'+archive_path.name))
    windows['installedBytes'] += sum(p.stat().st_size for p, _ in changed)
    (OUT/'windows-manifest.json').write_text(json.dumps(windows, indent=2)+'\n')
    with zipfile.ZipFile(OUT/'Ultra-Duel-Updater.zip','w',zipfile.ZIP_DEFLATED) as archive:
        for name in ('Update-Ultra-Duel.ps1','Update-Ultra-Duel.cmd'):
            archive.write(ROOT/'Tools'/name, name)
        archive.writestr('README.txt', 'Ultra Duel updater\n\nExtract beside Matteo-Duel.exe in your existing game folder.\nClose the game, then double-click Update-Ultra-Duel.cmd.\nOnly changed game files download. Your decks and settings are preserved.\n')
    print('PATCH_READY', len(changed), 'files;', round(archive_path.stat().st_size/1048576, 1), 'MB')

if __name__ == '__main__':
    main()
