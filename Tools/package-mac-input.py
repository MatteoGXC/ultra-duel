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
    args = parser.parse_args()
    player = ROOT/'release/Mac-arm64/Ultra-Duel-Player.app'
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
        audit = json.loads((ROOT/'Data/card-update-audit.json').read_text())
        for row in audit['art']:
            file = ROOT/'Picture/Art'/f"{row['id']}.jpg"
            if file.is_file():
                target.write(file, 'Defaults/Picture/Art/'+file.name)
        target.writestr('runtime.json', json.dumps(json.loads(args.runtime.read_text()), indent=2))
    print('MAC_INPUT_READY', args.output, args.output.stat().st_size)

if __name__ == '__main__':
    main()
