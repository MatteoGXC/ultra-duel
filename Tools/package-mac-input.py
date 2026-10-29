"""Prepare the private Mac verification input from a built player and clean defaults."""
import argparse
import hashlib
import json
import subprocess
from pathlib import Path
import zipfile
from build_source import provenance

ROOT = Path(__file__).resolve().parents[1]

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--baseline', type=Path, required=True)
    parser.add_argument('--runtime', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--player', type=Path, default=ROOT/'release/Mac-arm64/Ultra-Duel-Player.app')
    parser.add_argument('--intel-textures', type=Path)
    args = parser.parse_args()
    runtime=json.loads(args.runtime.read_text(encoding='utf-8'))
    player = args.player
    bundles = ROOT/'Platforms/StandaloneOSX/MasterDuel/Built-in'
    for name in ('shaders', 'sprites', 'materials', 'outduel', 'textures', 'duel'):
        assert (bundles/name).is_file(), 'Missing native macOS bundle: '+name
    files = {}
    def add(file, name):
        if file.is_file(): files[name] = file
    for file in player.rglob('*'):
        add(file, 'Player.app/'+file.relative_to(player).as_posix())
    for file in bundles.iterdir():
        if not file.name.endswith('.manifest'):
            add(file, 'Defaults/StandaloneOSX/MasterDuel/Built-in/'+file.name)
    menus = ROOT/'Platforms/StandaloneOSX/MDPro3'
    assert list(menus.glob('*_monoscripts.bundle')), 'Missing freshly built Mac menu dependencies'
    for file in menus.rglob('*'):
        add(file, 'Defaults/StandaloneOSX/MDPro3/'+file.relative_to(menus).as_posix())
    # Overlay current, tracked game data without copying a developer's account or settings.
    for name in subprocess.check_output(['git','ls-files','Data'],cwd=ROOT,text=True).splitlines():
        add(ROOT/name, 'Defaults/'+name)
    for deck in (ROOT/'Deck').glob('Starter - *.ydk'):
        add(deck,'Defaults/Deck/'+deck.name)
    if not runtime.get('artwork'):
        for file in (ROOT/'Picture/FullCard').glob('*.jpg'):
            add(file, 'Defaults/Picture/FullCard/'+file.name)
    for name in ('card-image-source.json', 'card-image-missing.json'):
        add(ROOT/'Data'/name, 'Defaults/Data/'+name)
    audit = json.loads((ROOT/'Data/card-update-audit.json').read_text(encoding='utf-8'))
    card_ids = {row['id'] for row in audit['art'] if (ROOT/'Picture/Art'/f"{row['id']}.jpg").is_file()}
    card_ids.update(row['id'] for row in json.loads((ROOT/'Data/beyond-the-brave-index.json').read_text(encoding='utf-8')))
    for code in card_ids:
        file = ROOT/'Picture/Art'/f"{code}.jpg"
        assert file.is_file(), 'Missing normal artwork: '+str(code)
        add(file, 'Defaults/Picture/Art/'+file.name)
    add(ROOT/'Expansions/ultra-card-art.zip','Defaults/Expansions/ultra-card-art.zip')
    for choice in ('Rock','Paper','Scissors'):
        file=ROOT/'Picture/DIY'/f'{choice}.png'
        assert file.is_file(), 'Missing RPS artwork: '+choice
        add(file, 'Defaults/Picture/DIY/'+file.name)
    if args.intel_textures:
        report=json.loads((args.intel_textures/'intel-texture-conversion.json').read_text(encoding='utf-8'))
        assert report['textures']>1000 and report['bundles']>100
        for file in (args.intel_textures/'StandaloneOSX').rglob('*'):
            add(file,'Defaults/'+file.relative_to(args.intel_textures).as_posix())
        add(args.intel_textures/'intel-texture-conversion.json','Defaults/Data/intel-texture-conversion.json')
    with zipfile.ZipFile(args.output, 'w', zipfile.ZIP_DEFLATED, compresslevel=1) as target:
        with zipfile.ZipFile(args.baseline) as baseline:
            copied=set()
            for entry in baseline.infolist():
                name=entry.filename
                if runtime.get('artwork') and name.startswith('Defaults/Picture/FullCard/'):
                    continue
                if not name.startswith('Defaults/') or '/ExtendedArt/' in name or name.endswith('extended-art-audit.json') or name in files or name in copied or name in ('Defaults/build-info.json','Defaults/Data/installed-version.json','Defaults/README.txt','Defaults/README-FIRST.txt'):
                    continue
                target.writestr(name,baseline.read(entry));copied.add(name)
        for name,file in sorted(files.items()):target.write(file,name)
        readme='Ultra Duel for macOS\n\nOpen Ultra Duel.app from Applications. Decks and settings remain in ~/Library/Application Support/Ultra Duel. Use the Windows installer only on Windows.\n'
        target.writestr('Defaults/README.txt',readme)
        target.writestr('Defaults/README-FIRST.txt',readme)
        runtime=json.loads(args.runtime.read_text(encoding='utf-8'))
        commit=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip()
        assembly=player/'Contents/Resources/Data/Managed/Assembly-CSharp.dll'
        info=dict(version=runtime['version'],commit=commit,application='Ultra Duel',executable='Player.app/Contents/MacOS/Ultra Duel',platform='macOS',assemblySha256=hashlib.sha256(assembly.read_bytes()).hexdigest())
        info.update(provenance(ROOT))
        target.writestr('Defaults/build-info.json',json.dumps(info,indent=2))
        target.writestr('Defaults/Data/installed-version.json',json.dumps(dict(version=runtime['version'],sourceCommit=commit)))
        target.writestr('runtime.json',json.dumps(runtime,indent=2))
    print('MAC_INPUT_READY', args.output, args.output.stat().st_size)

if __name__ == '__main__':
    main()
