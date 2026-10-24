"""Play the reported Branded hand through the production TCP/script-archive reader."""
import json,os,select,shutil,socket,struct,subprocess,sys,tempfile,time
from pathlib import Path
from NativeAdminDuelTests import child
ROOT=Path(__file__).resolve().parents[1]

def run(native,content):
    with tempfile.TemporaryDirectory(prefix='ultra-branded-') as directory:
        root=Path(directory);(root/'data').mkdir();(root/'Data').mkdir(exist_ok=True)
        shutil.copy2(content/'cards.cdb' if (content/'cards.cdb').exists() else content/'Data/locales/en-US/cards.cdb',root/'cards.cdb')
        shutil.copy2(content/'data/script.zip' if (content/'data/script.zip').exists() else content/'Data/script.zip',root/'data/script.zip')
        for name in ('lflist.conf','lflist_genesys.conf'):shutil.copy2(content/'Data'/name,root/'Data'/name)
        with socket.socket() as probe:probe.bind(('127.0.0.1',0));port=probe.getsockname()[1]
        proc=subprocess.Popen([sys.executable,__file__,'--server',str(native),directory,str(port)],stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True)
        clients=[];buffers=[]
        def send(i,op,data=b''):clients[i].sendall(struct.pack('<HB',len(data)+1,op)+data)
        def idle_entries(frame):
            offset=3;groups=[]
            for width in (7,7,7,7,7,11):
                count=frame[offset];offset+=1
                groups.append([struct.unpack_from('<I',frame,offset+n*width)[0] for n in range(count)])
                offset+=count*width
            return groups
        try:
            for i in range(2):
                for _ in range(600):
                    client=socket.socket()
                    try:client.connect(('127.0.0.1',port));break
                    except OSError:client.close();time.sleep(.05)
                else:raise AssertionError('Native room did not start')
                clients.append(client);buffers.append(bytearray())
                send(i,0x10,('Player '+str(i)).encode('utf-16-le').ljust(40,b'\0'))
                send(i,0x12,struct.pack('<HBBI',0x1362,204,204,0)+bytes(40))
            ready=set();confirmed=set();started=False;summoned=False;triggered=False;searched=False
            hand=[62962630,25451383,34995106,36637374,36577931]
            deck=hand+[89631139]*30+hand+[72272462]
            deadline=time.monotonic()+35
            while time.monotonic()<deadline:
                for client in select.select(clients,[],[],.2)[0]:
                    i=clients.index(client);data=client.recv(65536)
                    if not data:continue
                    buffers[i].extend(data);b=buffers[i]
                    while len(b)>=2:
                        size=struct.unpack_from('<H',b)[0]
                        if len(b)<size+2:break
                        f=bytes(b[2:2+size]);del b[:2+size];op=f[0]
                        if op==2:raise AssertionError(('Native room error',f.hex()))
                        if op==0x13 and i not in ready:
                            send(i,2,struct.pack('<II',41,0)+struct.pack('<41I',*deck));send(i,0x22);ready.add(i)
                        if op==0x21 and i==0 and (f[1]&15)==9:
                            confirmed.add(f[1]>>4)
                            if len(confirmed)==2 and not started:send(0,0x25);started=True
                        if op==3:send(i,3,bytes([1 if i==0 else 3]))
                        if op==4:send(i,4,b'\x01')
                        if op!=1:continue
                        message=f[1]
                        if message==1:raise AssertionError(('Engine rejected response',f.hex()))
                        if message==11 and i==0:
                            groups=idle_entries(f)
                            assert {34995106,36637374}<=set(groups[5]),('Branded spells unavailable',groups[5])
                            if not summoned:
                                assert 62962630 in groups[0],groups[0]
                                send(0,1,struct.pack('<i',groups[0].index(62962630)<<16));summoned=True
                            else:
                                assert triggered and searched,('Aluber search did not resolve',triggered,searched)
                                print('PASS production friend-duel engine: Branded in White, Branded Opening, Aluber Normal Summon search and usable cards after resolution')
                                return
                        if message==12:
                            code=struct.unpack_from('<I',f,3)[0]
                            answer=int(i==0 and code==62962630)
                            if answer:triggered=True
                            send(i,1,struct.pack('<i',answer))
                        if message==18:send(i,1,bytes([i,4,0]))
                        if message==19:send(i,1,struct.pack('<i',1))
                        if message==16:send(i,1,struct.pack('<i',-1))
                        if message==15:send(i,1,bytes([f[4]])+bytes(range(f[4])))
                        if message==50 and i==0 and f[7]==1 and f[11]==2 and struct.unpack_from('<I',f,2)[0] in (34995106,36637374):searched=True
            raise AssertionError(('Branded duel stalled',summoned,triggered,searched))
        finally:
            for c in clients:c.close()
            if proc.poll() is None:proc.terminate()
            text=proc.communicate(timeout=5)[0]
            if text.strip():print(text[-1200:])

if __name__=='__main__':
    if sys.argv[1]=='--server':child(Path(sys.argv[2]),sys.argv[3],int(sys.argv[4]))
    else:run(Path(sys.argv[1]).resolve(),Path(sys.argv[2]).resolve() if len(sys.argv)>2 else ROOT)
