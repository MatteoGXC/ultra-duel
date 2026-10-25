"""Run against isolated service/native workers; never touches production accounts or rooms."""
import argparse,json,os,secrets,socket,ssl,struct,subprocess,sys,tempfile,time,urllib.request,urllib.error
from pathlib import Path


def main():
    parser=argparse.ArgumentParser();parser.add_argument('--service',type=Path,required=True);args=parser.parse_args()
    with tempfile.TemporaryDirectory(prefix='ultra-tables-') as temporary:
        with socket.socket() as probe:probe.bind(('127.0.0.1',0));port=probe.getsockname()[1]
        room_first=21000+secrets.randbelow(1000)
        env=dict(os.environ,ULTRA_STATE=temporary,ULTRA_HOST='127.0.0.1',ULTRA_PORT=str(port),ULTRA_ROOM_FIRST=str(room_first))
        log=Path(temporary)/'server.log';stream=log.open('wb')
        proc=subprocess.Popen([sys.executable,'-u',str(args.service.resolve())],env=env,stdout=stream,stderr=stream)
        context=ssl._create_unverified_context();clients=[];rid=None;owner=None
        def api(path,body=None,token='',method=None,expected=200):
            request=urllib.request.Request(f'https://127.0.0.1:{port}'+path,headers={'Content-Type':'application/json','Authorization':'Bearer '+token},data=json.dumps(body).encode() if body is not None else None,method=method or ('POST' if body is not None else 'GET'))
            try:
                with urllib.request.urlopen(request,context=context,timeout=15) as response:status=response.status;data=json.load(response)
            except urllib.error.HTTPError as error:status=error.code;data=json.load(error)
            assert status==expected,(path,status,data)
            return data
        def send(client,op,data=b''):client.sendall(struct.pack('<HB',len(data)+1,op)+data)
        def frames(client):
            buffer=bytearray();result=[]
            while True:
                try:
                    part=client.recv(65536)
                    if not part:break
                    buffer.extend(part)
                except socket.timeout:break
            while len(buffer)>=2:
                size=struct.unpack_from('<H',buffer)[0]
                assert len(buffer)>=size+2
                result.append(bytes(buffer[2:2+size]));del buffer[:2+size]
            return result
        def connect(table,secret=None):
            client=socket.create_connection(('127.0.0.1',table['port']),timeout=5);client.settimeout(.3);clients.append(client)
            send(client,0x10,'Table Fixture'.encode('utf-16-le').ljust(40,b'\0'))
            send(client,0x12,struct.pack('<HBBI',0x1362,204,204,0)+(secret or table['secret']).encode('utf-16-le').ljust(40,b'\0'))
            return client,frames(client)
        try:
            for _ in range(120):
                if proc.poll() is not None:raise AssertionError(log.read_text())
                try:api('/health');break
                except (OSError,urllib.error.URLError):time.sleep(.1)
            else:raise AssertionError('Isolated service did not start')
            password=secrets.token_urlsafe(20)
            owner=api('/register',{'name':'TableOwner','password':password})
            friend=api('/register',{'name':'TableFriend','password':password})
            stranger=api('/register',{'name':'TableStranger','password':password})
            api('/friends/request',{'name':friend['name']},owner['token']);api('/friends/accept',{'id':owner['id']},friend['token'])
            room=api('/rooms',{'format':'TCG'},owner['token']);rid=room['id']
            tables=[]
            for number in range(1,4):
                table=api('/rooms/'+rid+'/join',{'table':number},friend['token']);tables.append(table)
                client,packets=connect(table)
                joined=next(p for p in packets if p[0]==0x12)
                assert struct.unpack_from('<H',joined,19)[0]==300,'Friend countdown was disabled'
            assert len({t['port'] for t in tables})==3
            assert len({t['secret'] for t in tables})==3
            api('/rooms/'+rid+'/watch',{'table':2},stranger['token'],expected=403)
            watch=api('/rooms/'+rid+'/watch',{'table':2},friend['token']);assert watch['spectate']
            watcher,packets=connect(watch);send(watcher,0x21)
            packets+=frames(watcher)
            assert any(p[0]==0x13 and p[1]&15==7 for p in packets),'Watch did not release its player seat'
            bad,packets=connect(tables[1],tables[0]['secret']);assert not any(p[0]==0x12 for p in packets),'Cross-table secret was accepted'
            view=api('/rooms/'+rid,token=friend['token']);assert len(view['tables'])==3 and all(t['open'] for t in view['tables'])
            print('PASS three independent native tables, distinct ports/secrets, five-minute handshake, friend-only spectating, observer seat and cross-table secret rejection',flush=True)
        finally:
            for client in clients:client.close()
            if rid and owner:
                try:api('/rooms/'+rid,token=owner['token'],method='DELETE')
                except Exception:pass
            proc.terminate();proc.wait(timeout=10);stream.close()
            if proc.returncode not in (0,-15):print(log.read_text(errors='replace')[-2000:])

if __name__=='__main__':main()
