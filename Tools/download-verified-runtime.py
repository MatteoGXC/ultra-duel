"""Fetch immutable runtime content in resumable chunks, then verify the whole archive."""
import argparse,concurrent.futures,hashlib,json,time,urllib.request
from pathlib import Path

def digest(path):
    h=hashlib.sha256()
    with path.open('rb') as stream:
        for part in iter(lambda:stream.read(4*1024*1024),b''):h.update(part)
    return h.hexdigest()

def main():
    p=argparse.ArgumentParser();p.add_argument('manifest',type=Path);p.add_argument('output',type=Path);a=p.parse_args();m=json.loads(a.manifest.read_text())
    if a.output.exists() and a.output.stat().st_size==m['size'] and digest(a.output)==m['sha256']:print('VERIFIED_CACHE');return
    folder=a.output.with_suffix('.chunks');folder.mkdir(parents=True,exist_ok=True);chunk=8*1024*1024
    def fetch(index):
        start=index*chunk;end=min(m['size'],start+chunk)-1;target=folder/str(index)
        if target.exists() and target.stat().st_size==end-start+1:return target
        for attempt in range(5):
            try:
                request=urllib.request.Request(m['url'],headers={'Range':f'bytes={start}-{end}','User-Agent':'Ultra-Duel-verified-Mac-build'})
                with urllib.request.urlopen(request,timeout=90) as r:
                    assert r.status==206 and r.headers.get('Content-Range')==f'bytes {start}-{end}/{m["size"]}'
                    body=r.read(end-start+2);assert len(body)==end-start+1;target.write_bytes(body);return target
            except Exception:
                if attempt==4:raise
                time.sleep(2**attempt)
    count=(m['size']+chunk-1)//chunk
    with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
        paths=[]
        for index,path in enumerate(pool.map(fetch,range(count))):
            paths.append(path)
            if index%32==0:print('RUNTIME_DOWNLOAD',round((index+1)/count*100),'%',flush=True)
    a.output.parent.mkdir(parents=True,exist_ok=True)
    with a.output.open('wb') as stream:
        for part in paths:
            stream.write(part.read_bytes());part.unlink()
    assert digest(a.output)==m['sha256'],'Runtime checksum failed'
    print('VERIFIED_RUNTIME',m['size'],m['sha256'])
if __name__=='__main__':main()
