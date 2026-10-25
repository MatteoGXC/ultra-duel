"""Package verified Intel texture overrides with the unchanged Mac artwork."""
import argparse,hashlib,json,tarfile
from pathlib import Path
import zstandard

def digest(path):
    h=hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda:stream.read(4*1024*1024),b''):h.update(block)
    return h.hexdigest()

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--source',type=Path,required=True);parser.add_argument('--overrides',type=Path,required=True);parser.add_argument('--output',type=Path,required=True);args=parser.parse_args()
    report=json.loads((args.overrides/'intel-texture-conversion.json').read_text(encoding='utf-8'))
    assert report['textures']>1000
    args.output.mkdir(parents=True,exist_ok=True);archive=args.output/'intel-runtime.tar.zst'
    files=[p for p in args.source.rglob('*') if p.is_file() and 'MDPro3' not in p.relative_to(args.source).parts]
    def normalize(info):
        info.mtime=0;info.uid=info.gid=0;info.uname=info.gname='';info.mode=0o755 if info.isdir() else 0o644;return info
    with archive.open('wb') as raw,zstandard.ZstdCompressor(level=9,threads=3).stream_writer(raw) as encoded,tarfile.open(fileobj=encoded,mode='w|') as output:
        for folder in ('Picture','StandaloneOSX'):output.add(args.source/folder,arcname='./'+folder,recursive=False,filter=normalize)
        for index,source in enumerate(sorted(files),1):
            relative=source.relative_to(args.source);override=args.overrides/relative
            output.add(override if override.is_file() else source,arcname='./'+relative.as_posix(),recursive=False,filter=normalize)
            if index%10000==0:print('INTEL_CONTENT_PACKED',index,'/',len(files),flush=True)
    parts=[]
    with archive.open('rb') as stream:
        index=0
        while True:
            data=stream.read(1400*1024*1024)
            if not data:break
            index+=1;part=args.output/f'Ultra-Duel-Intel-Content-{index:02}.bin';part.write_bytes(data)
            parts.append(dict(name=part.name,size=part.stat().st_size,sha256=hashlib.sha256(data).hexdigest(),url='https://github.com/MatteoGXC/ultra-duel/releases/download/macos-v0.1.8/'+part.name))
    manifest=dict(version='0.1.8',url=parts[0]['url'],size=archive.stat().st_size,sha256=digest(archive),parts=parts,textureCompatibility=dict(architecture='x86_64',convertedBundles=report['bundles'],convertedTextures=report['textures'],format='DXT5'))
    (args.output/'runtime.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
    print('INTEL_RUNTIME_READY',manifest['size'],manifest['sha256'],flush=True)

if __name__=='__main__':main()
