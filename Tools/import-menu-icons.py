"""Vendor the menu's licensed SVG sources and render crisp transparent UI textures.

Requires resvg-py; pass --renderer-path for a separate tools-only installation.
The checked-in sources.json pins both upstream repositories for reproducible reruns.
"""
import argparse
import hashlib
import json
from pathlib import Path
import sys
import urllib.request

parser = argparse.ArgumentParser()
parser.add_argument('--renderer-path')
args = parser.parse_args()
if args.renderer_path:
    sys.path.insert(0, args.renderer_path)
import resvg_py

root = Path(__file__).resolve().parents[1]
sources = root / 'Tools/MenuIconSources'
textures = root / 'Assets/Resources/Ultra/MenuIcons'
sources.mkdir(exist_ok=True)
textures.mkdir(parents=True, exist_ok=True)
def fetch(url):
    request = urllib.request.Request(url, headers={'User-Agent': 'Ultra-Duel-menu-assets'})
    with urllib.request.urlopen(request, timeout=45) as response:
        return response.read()
lock_path = sources / 'sources.json'
if lock_path.exists():
    lock = json.loads(lock_path.read_text())
else:
    lock = {repo: json.loads(fetch(f'https://api.github.com/repos/{repo}/commits/HEAD'))['sha']
            for repo in ('lucide-icons/lucide', 'primer/octicons')}
    lock_path.write_text(json.dumps(lock, indent=2) + '\n')
icons = ['settings', 'image', 'history', 'clapperboard', 'scaling', 'puzzle',
         'download', 'cloud-upload', 'github', 'log-out', 'power', 'chevron-right']
for icon in icons:
    repo = 'primer/octicons' if icon == 'github' else 'lucide-icons/lucide'
    relative = 'icons/mark-github-24.svg' if icon == 'github' else f'icons/{"clock-arrow-up" if icon == "history" else icon}.svg'
    path = sources / (icon + '.svg')
    if not path.exists():
        path.write_bytes(fetch(f'https://raw.githubusercontent.com/{repo}/{lock[repo]}/{relative}'))
    svg = path.read_text(encoding='utf-8').replace('currentColor', '#ffffff')
    if icon == 'github':
        svg = svg.replace('<svg ', '<svg fill="#ffffff" ', 1)
    (textures / (icon + '.png')).write_bytes(resvg_py.svg_to_bytes(svg_string=svg, width=192, height=192))
    # UI-only textures: preserve transparency and disable lossy compression/mips.
    meta = (root / 'Assets/Resources/Ultra/UserBackIcon.png.meta').read_text()
    import re
    meta = re.sub(r'guid: [a-f0-9]+', 'guid: ' + hashlib.md5(('Ultra/MenuIcons/' + icon).encode()).hexdigest(), meta, count=1)
    meta = meta.replace('enableMipMap: 1', 'enableMipMap: 0').replace('textureCompression: 1', 'textureCompression: 0')
    meta = meta.replace('alphaIsTransparency: 0', 'alphaIsTransparency: 1').replace('nPOTScale: 1', 'nPOTScale: 0')
    (textures / (icon + '.png.meta')).write_text(meta)
licenses = []
for repo in lock:
    path = sources / (repo.split('/')[-1] + '-LICENSE.txt')
    if not path.exists():
        path.write_bytes(fetch(f'https://raw.githubusercontent.com/{repo}/{lock[repo]}/LICENSE'))
    licenses.append(f'{repo}\nhttps://github.com/{repo}/tree/{lock[repo]}\n\n' + path.read_text(encoding='utf-8'))
(root / 'Data/MenuIcons-LICENSES.txt').write_text('\n\n'.join(licenses), encoding='utf-8')
print(f'Imported {len(icons)} licensed menu icons.')
