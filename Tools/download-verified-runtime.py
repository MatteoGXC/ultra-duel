"""Fetch immutable runtime content in resumable chunks, then verify the whole archive."""
import argparse,concurrent.futures,hashlib,json,time,urllib.request,threading,os
from pathlib import Path

def digest(path):
    h=hashlib.sha256()
    with path.open('rb') as stream:
        for part in iter(lambda:stream.read(4*1024*1024),b''):h.update(part)
    return h.hexdigest()

def versioned_parts(manifest,output):
    folder=output.with_suffix('.chunks');folder.mkdir(parents=True,exist_ok=True)
    class Redirect(urllib.request.HTTPRedirectHandler):
        def redirect_request(self,*args,**kwargs):
            result=super().redirect_request(*args,**kwargs)
            if result:result.remove_header('Authorization')
            return result
    opener=urllib.request.build_opener(Redirect())
    def fetch(part):
        assert Path(part['name']).name==part['name']
        target=folder/part['name']
        if target.exists() and target.stat().st_size==part['size'] and digest(target)==part['sha256']:return target
        for attempt in range(5):
            try:
                offset=target.stat().st_size if target.exists() else 0
                if offset>=part['size']:target.unlink();offset=0
                headers={'User-Agent':'Ultra-Duel-Mac-content-check','Accept':'application/octet-stream'};url=part['url']
                if os.environ.get('GH_TOKEN') and part.get('assetApi'):
                    url=part['assetApi'];assert url.startswith('https://api.github.com/repos/MatteoGXC/ultra-duel/releases/assets/')
                    headers['Authorization']='Bearer '+os.environ['GH_TOKEN']
                if offset:headers['Range']=f'bytes={offset}-'
                with opener.open(urllib.request.Request(url,headers=headers),timeout=90) as response:
                    if response.status==200:offset=0
                    else:assert response.status==206 and response.headers.get('Content-Range')==f'bytes {offset}-{part["size"]-1}/{part["size"]}'
                    with target.open('ab' if offset else 'wb') as stream:
                        while data:=response.read(4*1024*1024):
                            stream.write(data);offset+=len(data);assert offset<=part['size']
                assert target.stat().st_size==part['size'] and digest(target)==part['sha256']
                print('VERIFIED_CONTENT_PART',part['name'],flush=True);return target
            except Exception:
                if attempt==4:raise
                time.sleep(2**attempt)
    with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:paths=list(pool.map(fetch,manifest['parts']))
    with output.open('wb') as stream:
        for part in paths:
            with part.open('rb') as source:
                while data:=source.read(4*1024*1024):stream.write(data)
    assert digest(output)==manifest['sha256']
    for part in paths:part.unlink()
    print('VERIFIED_VERSIONED_RUNTIME',manifest['size'],manifest['sha256'],flush=True)

def main():
    p=argparse.ArgumentParser();p.add_argument('manifest',type=Path);p.add_argument('output',type=Path);a=p.parse_args();m=json.loads(a.manifest.read_text())
    if a.output.exists() and a.output.stat().st_size==m['size'] and digest(a.output)==m['sha256']:print('VERIFIED_CACHE');return
    if m.get('parts'):versioned_parts(m,a.output);return
    with urllib.request.urlopen(urllib.request.Request(m['url'],method='HEAD'),timeout=30) as response:
        assert int(response.headers['Content-Length'])==m['size'],'Runtime archive size changed'
        etag=response.headers['ETag']
    folder=a.output.with_suffix('.chunks');folder.mkdir(parents=True,exist_ok=True);chunk=8*1024*1024
    stopped=threading.Event()
    def fetch(index):
        start=index*chunk;end=min(m['size'],start+chunk)-1;target=folder/str(index)
        if target.exists() and target.stat().st_size==end-start+1:return target
        for attempt in range(5):
            if stopped.is_set():raise RuntimeError('Download cancelled after another range failed')
            try:
                request=urllib.request.Request(m['url'],headers={'Range':f'bytes={start}-{end}','If-Match':etag})
                with urllib.request.urlopen(request,timeout=45) as r:
                    assert r.status==206 and r.headers.get('Content-Range')==f'bytes {start}-{end}/{m["size"]}'
                    body=r.read(end-start+2);assert len(body)==end-start+1;target.write_bytes(body);return target
            except Exception:
                if attempt==4:raise
                time.sleep(2**attempt)
    count=(m['size']+chunk-1)//chunk
    pool=concurrent.futures.ThreadPoolExecutor(max_workers=8)
    try:
        paths=[]
        for index,path in enumerate(pool.map(fetch,range(count))):
            paths.append(path)
            if index%32==0:print('RUNTIME_DOWNLOAD',round((index+1)/count*100),'%',flush=True)
    except BaseException:
        stopped.set();pool.shutdown(wait=True,cancel_futures=True);raise
    else:pool.shutdown()
    a.output.parent.mkdir(parents=True,exist_ok=True)
    with a.output.open('wb') as stream:
        for part in paths:
            stream.write(part.read_bytes());part.unlink()
    assert digest(a.output)==m['sha256'],'Runtime checksum failed'
    print('VERIFIED_RUNTIME',m['size'],m['sha256'])
if __name__=='__main__':main()
