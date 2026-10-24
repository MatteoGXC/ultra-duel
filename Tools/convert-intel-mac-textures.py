"""Build Intel-compatible overrides for Apple-only ASTC asset-bundle textures.

Build-time tool only. The original archive is preserved. Object IDs, materials,
mesh references and bundle names remain unchanged; ASTC textures become BC3.
Requires UnityPy==1.25.4.
"""
import argparse,collections,concurrent.futures,json,os
from pathlib import Path
import UnityPy
from UnityPy.enums import TextureFormat

def convert(task):
    source,destination,relative=task
    with source.open('rb') as stream:
        if stream.read(7)!=b'UnityFS':return None
    env=UnityPy.load(str(source));changes=[]
    for obj in env.objects:
        if obj.type.name!='Texture2D':continue
        texture=obj.read();format=TextureFormat(texture.m_TextureFormat)
        if not format.name.startswith('ASTC'):continue
        if 'HDR' in format.name:raise ValueError('HDR ASTC requires a separate conversion: '+str(source))
        image=texture.image
        width,height=texture.m_Width,texture.m_Height
        texture.set_image(image,target_format=TextureFormat.DXT5,mipmap_count=texture.m_MipCount or 1)
        assert texture.m_Width==width and texture.m_Height==height
        texture.save()
        changes.append(dict(name=texture.m_Name,width=width,height=height,original=format.name,converted='DXT5'))
    if not changes:return None
    destination.parent.mkdir(parents=True,exist_ok=True)
    destination.write_bytes(env.file.save(packer='lz4'))
    # Parse the actual saved bundle, including its inline image data.
    check=UnityPy.load(str(destination))
    remaining=[o.read().m_Name for o in check.objects if o.type.name=='Texture2D' and TextureFormat(o.read().m_TextureFormat).name.startswith('ASTC')]
    assert not remaining,(relative,remaining)
    return dict(path=relative,textures=changes,sourceBytes=source.stat().st_size,convertedBytes=destination.stat().st_size)

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--source',type=Path,required=True);parser.add_argument('--output',type=Path,required=True);parser.add_argument('--workers',type=int,default=2);args=parser.parse_args()
    assert UnityPy.__version__=='1.25.4'
    files=[p for p in args.source.rglob('*') if p.is_file() and 'MDPro3' not in p.relative_to(args.source).parts]
    tasks=[(p,args.output/p.relative_to(args.source),p.relative_to(args.source).as_posix()) for p in files]
    rows=[]
    with concurrent.futures.ProcessPoolExecutor(max_workers=args.workers) as pool:
        for index,row in enumerate(pool.map(convert,tasks,chunksize=4),1):
            if row:rows.append(row)
            if index%1000==0:print('INTEL_TEXTURES',index,'/',len(files),'converted bundles',len(rows),flush=True)
    args.output.mkdir(parents=True,exist_ok=True)
    report=dict(converter='UnityPy 1.25.4',bundles=len(rows),textures=sum(len(r['textures']) for r in rows),convertedBytes=sum(r['convertedBytes'] for r in rows),files=rows)
    (args.output/'intel-texture-conversion.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print('INTEL_COMPATIBLE_TEXTURE_OVERRIDES',report['bundles'],report['textures'],report['convertedBytes'],flush=True)

if __name__=='__main__':main()
