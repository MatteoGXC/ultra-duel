"""Prepare the private Mac verification input from a built player and clean defaults."""
import argparse
import json
from pathlib import Path
import zipfile

ROOT = Path(__file__).resolve().parents[1]

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--baseline', type=Path, required=True)
    parser.add_argument('--runtime', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--player', type=Path, default=ROOT/'release/Mac-arm64/Ultra-Duel-Player.app')
    parser.add_argument('--intel-textures', type=Path)
    args = parser.parse_args()
    player = args.player
    bundles = ROOT/'Platforms/StandaloneOSX/MasterDuel/Built-in'
    for name in ('shaders', 'sprites', 'materials', 'outduel', 'textures', 'duel'):
        assert (bundles/name).is_file(), 'Missing native macOS bundle: '+name
    with zipfile.ZipFile(args.output, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as target:
        with zipfile.ZipFile(args.baseline) as baseline:
            for entry in baseline.infolist():
                name = entry.filename
                if not name.startswith('Defaults/') or '/ExtendedArt/' in name or name.endswith('extended-art-audit.json'):
                    continue
                target.writestr(name, baseline.read(entry))
        for file in player.rglob('*'):
            if file.is_file():
                target.write(file, 'Player.app/'+file.relative_to(player).as_posix())
        for file in bundles.iterdir():
            if file.is_file() and not file.name.endswith('.manifest'):
                target.write(file, 'Defaults/StandaloneOSX/MasterDuel/Built-in/'+file.name)
        menus = ROOT/'Platforms/StandaloneOSX/MDPro3'
        assert list(menus.glob('*_monoscripts.bundle')), 'Missing freshly built Mac menu dependencies'
        for file in menus.rglob('*'):
            if file.is_file():
                target.write(file, 'Defaults/StandaloneOSX/MDPro3/'+file.relative_to(menus).as_posix())
        audit = json.loads((ROOT/'Data/card-update-audit.json').read_text())
        for row in audit['art']:
            file = ROOT/'Picture/Art'/f"{row['id']}.jpg"
            if file.is_file():
                target.write(file, 'Defaults/Picture/Art/'+file.name)
        if args.intel_textures:
            report=json.loads((args.intel_textures/'intel-texture-conversion.json').read_text(encoding='utf-8'))
            assert report['textures']>1000 and report['bundles']>100
            for file in (args.intel_textures/'StandaloneOSX').rglob('*'):
                if file.is_file():target.write(file,'Defaults/'+file.relative_to(args.intel_textures).as_posix())
            target.write(args.intel_textures/'intel-texture-conversion.json','Defaults/Data/intel-texture-conversion.json')
        target.writestr('runtime.json', json.dumps(json.loads(args.runtime.read_text()), indent=2))
    print('MAC_INPUT_READY', args.output, args.output.stat().st_size)

if __name__ == '__main__':
    main()
