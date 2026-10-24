"""Assemble and ad-hoc sign an Apple Silicon app on a real macOS runner."""
import argparse,json,os,plistlib,shutil,stat,subprocess,zipfile
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
def run(*args):subprocess.run([str(a) for a in args],check=True)
def main():
    p=argparse.ArgumentParser();p.add_argument('--input',type=Path,required=True);p.add_argument('--output',type=Path,required=True);a=p.parse_args()
    assert os.uname().machine=='arm64'
    app=a.output/'Ultra Duel.app';resources=app/'Contents/Resources';resources.mkdir(parents=True)
    with zipfile.ZipFile(a.input) as archive:
        for entry in archive.infolist():
            relative=Path(entry.filename);assert not relative.is_absolute() and '..' not in relative.parts
            target=resources/relative
            if entry.is_dir():target.mkdir(parents=True,exist_ok=True)
            else:
                target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(archive.read(entry))
    player=resources/'Player.app'
    for file in player.rglob('*'):
        if file.is_file() and file.read_bytes()[:4] in (b'\xcf\xfa\xed\xfe',b'\xfe\xed\xfa\xcf',b'\xca\xfe\xba\xbe'):
            file.chmod(0o755)
    for file in (player/'Contents/MacOS').iterdir():file.chmod(0o755)
    tools=resources/'Tools';tools.mkdir()
    prefix=Path(subprocess.check_output(['brew','--prefix','zstd'],text=True).strip())
    shutil.copy2(prefix/'bin/zstd',tools/'zstd');shutil.copy2(prefix/'lib/libzstd.1.dylib',tools/'libzstd.1.dylib')
    deps=subprocess.check_output(['otool','-L',str(tools/'zstd')],text=True)
    for line in deps.splitlines()[1:]:
        dependency=line.strip().split(' (')[0]
        if 'libzstd.' in dependency:run('install_name_tool','-change',dependency,'@loader_path/libzstd.1.dylib',tools/'zstd')
    run('codesign','--force','--sign','-',tools/'libzstd.1.dylib');run('codesign','--force','--sign','-',tools/'zstd')
    run('codesign','--force','--deep','--sign','-',player)
    executable=app/'Contents/MacOS/Ultra Duel';executable.parent.mkdir()
    run('xcrun','swiftc','-swift-version','5','-O','-target','arm64-apple-macos11.0','-framework','Cocoa','-framework','CryptoKit',ROOT/'Tools/MacBootstrap.swift','-o',executable)
    info=dict(CFBundleExecutable='Ultra Duel',CFBundleName='Ultra Duel',CFBundleDisplayName='Ultra Duel',CFBundleIdentifier='com.ultraduel.launcher',CFBundlePackageType='APPL',CFBundleShortVersionString='0.1.5',CFBundleVersion='15',LSMinimumSystemVersion='11.0',NSHighResolutionCapable=True)
    (app/'Contents/Info.plist').write_bytes(plistlib.dumps(info))
    run('codesign','--force','--deep','--sign','-',app)
    run('codesign','--verify','--deep','--strict','--verbose=2',app)
    (a.output/'Applications').symlink_to('/Applications')
    (a.output/'READ ME.txt').write_text('Ultra Duel — Apple Silicon\n\nDrag Ultra Duel into Applications, then open it.\nThe first launch downloads 3.7 GB of verified artwork and duel fields.\nAllow 10 GB of free space during setup. Paused downloads resume.\n\nGame data, decks and settings are saved in ~/Library/Application Support/Ultra Duel.\n\nThis independent build is ad-hoc signed, not Apple notarized. macOS may require Privacy & Security > Open Anyway.\n')
    print('MAC_APP_ASSEMBLED',app)
if __name__=='__main__':main()
