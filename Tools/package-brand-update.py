"""Cumulative Ultra Duel update against the original public Windows build."""
import hashlib
from datetime import datetime, timezone
import json
import subprocess
import shutil
import zipfile
import zlib
from runtime_layout import canonical_path
from build_source import provenance
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT.parent / 'release-work/windows-v0.1.0'
VERSION = '0.1.15'
OUT = ROOT.parent / ('release-work/windows-v'+VERSION)
PLAYER = ROOT / 'release/Ultra-Duel'

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
        assemblySha256=sha(PLAYER/'Ultra-Duel_Data/Managed/Assembly-CSharp.dll'),
        nativeServerSha256=sha(PLAYER/'Ultra-Duel_Data/Plugins/x86_64/ygoserver.dll'))
    info.update(provenance(ROOT))
    info_path.write_text(json.dumps(info, indent=2)+'\n')
    (PLAYER/'Data/installed-version.json').write_text(json.dumps(dict(version=VERSION, sourceCommit=commit, runtimeLayout='ultra-v1'))+'\n')
    baseline = {}
    for part in json.loads((BASE/'windows-manifest.json').read_text())['parts']:
        path=BASE/part['name']
        with zipfile.ZipFile(path) as archive:
            baseline.update({canonical_path(e.filename): (e.file_size, e.CRC) for e in archive.infolist()})
    if not baseline:
        raise RuntimeError('Original release archives are required for a cumulative patch.')
    names = ['Ultra-Duel.exe', 'Ultra-Duel-Migration.dll', 'UnityPlayer.dll', 'UnityCrashHandler64.exe',
             'build-info.json', 'Data/installed-version.json', 'Ultra-Duel_Data', 'MonoBleedingEdge', 'D3D12',
             'Update-Ultra-Duel.ps1', 'Update-Ultra-Duel.cmd',
             'README.txt']
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
    for name in ('Rock.png','Paper.png','Scissors.png'):
        source=ROOT/'Picture/DIY'/name;target=PLAYER/'Picture/DIY'/name
        if source.is_file():
            target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,target)
        if not target.is_file():raise RuntimeError('Missing RPS artwork: '+name)
        names.append('Picture/DIY/'+name)
    names=list(dict.fromkeys(names))
    changed = []
    for name in names:
        path = PLAYER / name
        for source in ([path] if path.is_file() else path.rglob('*')):
            if not source.is_file():
                continue
            relative = source.relative_to(PLAYER).as_posix()
            # Master Duel runtime content is unchanged; freshly built menu
            # dependencies must accompany the new Addressables catalog.
            if relative.endswith(('.pdb', '.mdb')):
                continue
            crc = 0
            with source.open('rb') as stream:
                for block in iter(lambda: stream.read(4*1024*1024), b''):
                    crc = zlib.crc32(block, crc)
            boot_runtime=relative.startswith('Ultra-Duel_Data/') and not relative.startswith('Ultra-Duel_Data/StandaloneWindows64/')
            # The Unity player bytes can match the historical executable even
            # though an installed Ultra-Duel.exe is still the old tiny launcher.
            # Always replace that launcher during the branding migration.
            if relative == 'Ultra-Duel.exe' or boot_runtime or baseline.get(relative) != (source.stat().st_size, crc):
                changed.append((source, relative))
    archive_path = OUT / 'Ultra-Duel-Update.zip'
    with zipfile.ZipFile(archive_path, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
        for source, relative in changed:
            archive.write(source, relative)
    commit = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip()
    manifest = dict(version=VERSION, runtimeLayout='ultra-v1', minimumVersion='0.1.0', sourceCommit=commit,
                    asset=archive_path.name, sha256=sha(archive_path),
                    files=[dict(path=relative, sha256=sha(source)) for source, relative in changed])
    manifest.update(provenance(ROOT))
    (OUT/'update-manifest.json').write_text(json.dumps(manifest, indent=2)+'\n')
    # Fresh installations use new, canonically named full archives. Historical
    # archives above are read only to calculate a small backward-compatible patch.
    windows=json.loads((OUT/'full-windows-manifest.json').read_text())
    if windows['version']!=VERSION or windows.get('runtimeLayout')!='ultra-v1':
        raise RuntimeError('Build the matching canonical full Windows package first.')
    (OUT/'windows-manifest.json').write_text(json.dumps(windows,indent=2)+'\n')
    with zipfile.ZipFile(OUT/'Ultra-Duel-Updater.zip','w',zipfile.ZIP_DEFLATED) as archive:
        for name in ('Update-Ultra-Duel.ps1','Update-Ultra-Duel.cmd','Ultra-Duel-Migration.dll'):
            archive.write(PLAYER/name, name)
        archive.writestr('README.txt', 'Ultra Duel updater\n\nExtract all files into your existing game folder. Older installations are migrated automatically.\nClose the game, then double-click Update-Ultra-Duel.cmd.\nOnly changed game files download. Your decks and settings are preserved.\n')
    print('PATCH_READY', len(changed), 'files;', round(archive_path.stat().st_size/1048576, 1), 'MB')

if __name__ == '__main__':
    main()
