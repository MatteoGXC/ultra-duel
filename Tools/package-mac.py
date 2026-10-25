"""Assemble and ad-hoc sign a native Intel or Apple Silicon app on macOS."""
import argparse,json,os,plistlib,shutil,stat,subprocess,zipfile
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
def run(*args):subprocess.run([str(a) for a in args],check=True)
def main():
    p=argparse.ArgumentParser();p.add_argument('--input',type=Path,required=True);p.add_argument('--output',type=Path,required=True);a=p.parse_args()
    architecture=os.uname().machine
    assert architecture in ('arm64','x86_64')
    platform_name='Apple Silicon' if architecture=='arm64' else 'Intel'
    app=a.output/'Ultra Duel.app';resources=app/'Contents/Resources';resources.mkdir(parents=True)
    with zipfile.ZipFile(a.input) as archive:
        for entry in archive.infolist():
            relative=Path(entry.filename);assert not relative.is_absolute() and '..' not in relative.parts
            target=resources/relative
            if entry.is_dir():target.mkdir(parents=True,exist_ok=True)
            else:
                target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(archive.read(entry))
    player=resources/'Player.app'
    run('lipo',player/'Contents/MacOS/Ultra Duel','-verify_arch',architecture)
    for name in ('libocgcore.dylib','libygoserver.dylib'):
        run('lipo',player/'Contents/Plugins'/name,'-verify_arch',architecture)
    for file in player.rglob('*'):
        if file.is_file() and file.read_bytes()[:4] in (b'\xcf\xfa\xed\xfe',b'\xfe\xed\xfa\xcf',b'\xca\xfe\xba\xbe'):
            file.chmod(0o755)
    for file in (player/'Contents/MacOS').iterdir():file.chmod(0o755)
    tools=resources/'Tools';tools.mkdir()
    prefix=Path(subprocess.check_output(['brew','--prefix','zstd'],text=True).strip())
    shutil.copy2(prefix/'bin/zstd',tools/'zstd');shutil.copy2(prefix/'lib/libzstd.1.dylib',tools/'libzstd.1.dylib')
    queue=[tools/'zstd',tools/'libzstd.1.dylib'];seen=set()
    while queue:
        binary=queue.pop()
        if binary.name in seen:continue
        seen.add(binary.name)
        deps=subprocess.check_output(['otool','-L',str(binary)],text=True)
        if binary.suffix=='.dylib':run('install_name_tool','-id','@loader_path/'+binary.name,binary)
        for line in deps.splitlines()[1:]:
            dependency=line.strip().split(' (')[0]
            if dependency.startswith('@rpath/'):
                name=Path(dependency).name
                if name==binary.name:continue
                local=tools/name
                if not local.exists():shutil.copy2(Path(subprocess.check_output(['brew','--prefix'],text=True).strip())/'lib'/name,local)
                run('install_name_tool','-change',dependency,'@loader_path/'+name,binary);queue.append(local);continue
            if not dependency.startswith(('/opt/homebrew/','/usr/local/')):continue
            name=Path(dependency).name
            if name==binary.name:continue
            local=tools/name
            if not local.exists():shutil.copy2(dependency,local)
            run('install_name_tool','-change',dependency,'@loader_path/'+name,binary);queue.append(local)
    for binary in tools.iterdir():
        dependencies=subprocess.check_output(['otool','-L',str(binary)],text=True)
        assert '/opt/homebrew/' not in dependencies and '/usr/local/' not in dependencies and '@rpath/' not in dependencies,dependencies
        run('codesign','--force','--sign','-',binary)
    run('codesign','--force','--deep','--sign','-',player)
    executable=app/'Contents/MacOS/Ultra Duel';executable.parent.mkdir()
    run('xcrun','swiftc','-swift-version','5','-O','-target',architecture+'-apple-macos11.0','-framework','Cocoa','-framework','CryptoKit',ROOT/'Tools/MacBootstrap.swift','-o',executable)
    info=dict(CFBundleExecutable='Ultra Duel',CFBundleName='Ultra Duel',CFBundleDisplayName='Ultra Duel',CFBundleIdentifier='com.ultraduel.launcher',CFBundlePackageType='APPL',CFBundleShortVersionString=json.loads((resources/'runtime.json').read_text())['version'],CFBundleVersion=json.loads((resources/'runtime.json').read_text())['version'].replace('.',''),LSMinimumSystemVersion='11.0',NSHighResolutionCapable=True)
    (app/'Contents/Info.plist').write_bytes(plistlib.dumps(info))
    if (ROOT/'LICENSE').exists():shutil.copy2(ROOT/'LICENSE',resources/'LICENSE.txt')
    run('codesign','--force','--deep','--sign','-',app)
    run('codesign','--verify','--deep','--strict','--verbose=2',app)
    (a.output/'Applications').symlink_to('/Applications')
    (a.output/'READ ME.txt').write_text('Ultra Duel — '+platform_name+'\n\nDrag Ultra Duel into Applications, then open it.\nThe first launch downloads approximately 4 GB of verified artwork and duel fields.\nAllow 10 GB of free space during setup. Paused downloads resume.\n\nGame data, decks and settings are saved in ~/Library/Application Support/Ultra Duel.\n\nThis independent build is ad-hoc signed, not Apple notarized. macOS may require Privacy & Security > Open Anyway.\n')
    print('MAC_APP_ASSEMBLED',app)
if __name__=='__main__':main()
