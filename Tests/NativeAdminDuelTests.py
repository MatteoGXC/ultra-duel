"""Check explicit admin commands against a real two-client native duel."""
import ctypes, json, os, select, shutil, socket, struct, subprocess, sys, tempfile, threading, time
from pathlib import Path
ROOT=Path(os.environ.get('ULTRA_TEST_CONTENT',Path(__file__).resolve().parents[1]))

def child(native,runtime,port):
    os.chdir(runtime)
    suffix='.dylib' if sys.platform=='darwin' else '.so'
    handle=os.add_dll_directory(str(native)) if os.name=='nt' else None
    ctypes.CDLL(str(native/('ocgcore.dll' if os.name=='nt' else 'libocgcore'+suffix)),mode=ctypes.RTLD_GLOBAL)
    server=ctypes.CDLL(str(native/('ygoserver.dll' if os.name=='nt' else 'libygoserver'+suffix)))
    if hasattr(server,'admin_duel_action'):
        server.admin_duel_action.argtypes=[ctypes.c_char_p,ctypes.c_int,ctypes.c_int,ctypes.c_int]
    def commands():
        for line in sys.stdin:
            row=json.loads(line)
            result=server.admin_duel_action(row['name'].encode(),row['op'],row['target'],row['value'])
            Path(row['reply']).write_text(str(result))
    threading.Thread(target=commands,daemon=True).start()
    server.start_server.argtypes=[ctypes.c_char_p]
    server.start_server(f"{port} 0 1 0 7 T T 8000 5 1 {3 if os.environ.get('ULTRA_TIMER_TEST') else 0} 0".encode())

def run(native):
    with tempfile.TemporaryDirectory(prefix='ultra-admin-') as directory:
        runtime=Path(directory);(runtime/'data').mkdir();(runtime/'Data').mkdir(exist_ok=True)
        shutil.copy2(ROOT/'Data/locales/en-US/cards.cdb',runtime/'cards.cdb')
        shutil.copy2(ROOT/'Data/script.zip',runtime/'data/script.zip')
        for name in ('lflist.conf','lflist_genesys.conf'):shutil.copy2(ROOT/'Data'/name,runtime/'Data'/name)
        with socket.socket() as probe:probe.bind(('127.0.0.1',0));port=probe.getsockname()[1]
        proc=subprocess.Popen([sys.executable,__file__,'--server',str(native),directory,str(port)],stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True)
        clients=[];buffers=[];calls=0
        def send(i,op,data=b''):clients[i].sendall(struct.pack('<HB',len(data)+1,op)+data)
        def command(name,op,target=0,value=0):
            nonlocal calls
            calls+=1;reply=runtime/f'result-{calls}.txt'
            proc.stdin.write(json.dumps(dict(name=name,op=op,target=target,value=value,reply=str(reply)))+'\n');proc.stdin.flush()
            deadline=time.monotonic()+6
            while time.monotonic()<deadline:
                if reply.exists():return int(reply.read_text())
                time.sleep(.01)
            raise AssertionError('Admin command timed out')
        try:
            for i in range(3 if os.environ.get('ULTRA_SPECTATOR_TEST') else 2):
                for _ in range(600):
                    client=socket.socket()
                    try:client.connect(('127.0.0.1',port));break
                    except OSError:client.close();time.sleep(.05)
                else:raise AssertionError('Server did not start')
                clients.append(client);buffers.append(bytearray())
                send(i,0x10,('Player '+str(i)).encode('utf-16-le').ljust(40,b'\0'))
                send(i,0x12,struct.pack('<HBBI',0x1362,204,204,0)+bytes(40))
            ready=set();confirmed=set();started=False;stage=0;lp=[];moves=[];timer_seen=False;observer_seen=False;zero_seen=False;defense_seen=False
            deadline=time.monotonic()+35
            while time.monotonic()<deadline:
                for client in select.select(clients,[],[],.2)[0]:
                    i=clients.index(client);data=client.recv(65536)
                    if not data:continue
                    buffers[i].extend(data);buffer=buffers[i]
                    while len(buffer)>=2:
                        size=struct.unpack_from('<H',buffer)[0]
                        if len(buffer)<size+2:break
                        f=bytes(buffer[2:2+size]);del buffer[:2+size];op=f[0]
                        if op==2:raise AssertionError(('Native room error',f.hex()))
                        if op==0x13 and i==2:
                            assert (f[1]&15)==7,('Third client was not an observer',f.hex())
                            observer_seen=True
                        if op==0x13 and i<2 and i not in ready:
                            code=69247929 if os.environ.get('ULTRA_ADMIN_EXTENDED') else 89631139
                            send(i,2,struct.pack('<II',40,0)+struct.pack('<40I',*([code]*40)));send(i,0x22);ready.add(i)
                        if op==0x21 and i==0 and (f[1]&15)==9:
                            confirmed.add(f[1]>>4)
                            if len(confirmed)==2 and not started:send(0,0x25);started=True
                        if op==3:send(i,3,bytes([1 if i==0 else 3]))
                        if op==4:send(i,4,b'\x01')
                        if op==0x18:
                            timer_seen=True
                            assert struct.unpack_from('<H',f,3)[0]>0,('Invalid countdown',f.hex())
                            send(i,0x15)
                        if op!=1:continue
                        if os.environ.get('ULTRA_TIMER_TEST'):
                            if f[1]==5:
                                assert timer_seen and f[3]==3,('Missing timer or timeout result',f.hex())
                                print('PASS native countdown packets, confirmation and automatic timeout loss')
                                return
                            continue
                        message=f[1]
                        if message==1:raise AssertionError(('Engine rejected a response',f.hex()))
                        if os.environ.get('ULTRA_ADMIN_EXTENDED'):
                            if os.environ.get('ULTRA_ADMIN_TRACE'):
                                print('TRACE',i,stage,message,f[:24].hex(),flush=True)
                            if message==16:
                                send(i,1,struct.pack('<i',-1))
                                continue
                            if message==50:moves.append((i,f))
                            if message==6 and i==0 and f[2:4]==bytes((1,4)) and len(f)>=12:
                                size,flags=struct.unpack_from('<II',f,4)
                                if size>8 and flags&0x100:
                                    offset=12+4*(flags&0xff).bit_count()
                                    if struct.unpack_from('<i',f,offset)[0]==0:zero_seen=True
                            if message==53 and i==0:
                                assert f[10]==4,('Monster did not enter face-up Defense',f.hex())
                                defense_seen=True
                            if message==18:
                                if i==1:
                                    assert command('Player 0',6,target=1)==2,'Admin action interrupted a zone selection'
                                    send(i,1,bytes((1,4,0)))
                                continue
                            if message==11:
                                if i==0 and stage<6:
                                    if stage==0:
                                        assert command('Player 0',7,target=1,value=7)==3
                                        assert command('Player 0',8,target=1,value=0)==3
                                        assert command('Player 0',9,target=1)==2
                                    assert command('Player 0',3,value=55144522)==0
                                    stage+=1
                                elif i==0 and stage==6:
                                    added=[frame for who,frame in moves if who==0 and frame[7]==0 and frame[11]==2]
                                    assert len(added)==6,('Repeated additions did not arrive',len(added))
                                    assert command('Player 1',4,target=1)==0;stage=16
                                elif i==0 and stage==16:
                                    send(i,1,struct.pack('<I',7));stage=7
                                elif i==1 and stage==7:
                                    assert f[3]>0,'No summonable monster'
                                    send(i,1,struct.pack('<I',0));stage=8
                                elif i==1 and stage==8:
                                    assert command('Player 0',8,target=1,value=0)==0;stage=9
                                elif i==1 and stage==9:
                                    assert command('Player 0',7,target=1,value=0)==0;stage=10
                                elif i==1 and stage==10:
                                    assert zero_seen and defense_seen,'Missing real 0 ATK or Defense update'
                                    if os.environ.get('ULTRA_ADMIN_FIELD_ONLY'):
                                        assert command('Player 0',5,target=1)==0;stage=11
                                    else:
                                        assert command('Player 0',9,target=1)==0;stage=12
                                elif i==1 and stage==11:
                                    assert any(who==0 and frame[7]==4 and frame[11]==16 for who,frame in moves),'Field did not clear'
                                    send(i,1,struct.pack('<I',6));stage=13
                                elif i==0 and stage==12:
                                    assert command('Player 0',6,target=1)==0;stage=14
                                elif i==0 and stage==14:
                                    cleared=[frame for who,frame in moves if who==0 and frame[6]==1 and frame[11]==16]
                                    assert len(cleared)==6,('Hand and field not fully cleared',len(cleared))
                                    assert command('Player 0',1)==0;stage=15
                            elif message==10 and i==1 and stage==13:
                                assert command('Player 0',9,target=1)==0;stage=12
                            elif message==5:
                                assert stage==15,(stage,f.hex())
                                print('PASS six repeated additions, opponent-turn 0 ATK / Defense, clear field / hand+field, force end Main/Battle Phase, and rejection during a pending selection')
                                return
                            continue
                        if message==94 and i==0:lp.append((f[2],struct.unpack_from('<i',f,3)[0]))
                        if message==50:
                            moves.append((i,f))
                            if i in (1,2) and f[10]==0 and f[11]==2:
                                assert struct.unpack_from('<I',f,2)[0]==0,'Added card leaked to opponent'
                        if message==11 and i==0:
                            if stage==0:
                                assert not lp and not moves,'Admin changes occurred without an explicit command'
                                assert command('Not Seated',2,value=10000)==4
                                assert command('Player 1',2,value=10000)==2
                                assert command('Player 0',2,value=-1)==3
                                assert command('Player 0',2,value=12345)==0
                            elif stage==1:
                                assert lp==[(0,12345)],lp
                                assert command('Player 0',3,value=55144522)==0
                            elif stage==2:
                                offset=3
                                for _ in range(5):offset+=1+f[offset]*7
                                count=f[offset];offset+=1
                                activatable=[struct.unpack_from('<I',f,offset+n*11)[0] for n in range(count)]
                                assert 55144522 in activatable,('Added spell was not usable',activatable)
                                own=[frame for who,frame in moves if who==0 and frame[11]==2]
                                assert any(struct.unpack_from('<I',frame,2)[0]==55144522 for frame in own)
                                assert command('Player 0',4,target=1)==0
                            elif stage==3:
                                discarded=[frame for who,frame in moves if who==0 and frame[7]==2 and frame[11]==16]
                                assert len(discarded)==5,('Opponent hand did not move to GY',len(discarded))
                                assert command('Player 0',1)==0
                            stage+=1
                        if message==5:
                            assert stage==4 and f[2:]==b'\x00\x00',(stage,f.hex())
                            if os.environ.get('ULTRA_SPECTATOR_TEST'):assert observer_seen,'Spectator role not received'
                            print('PASS explicit owner commands: no automatic changes, seat/turn checks, LP update, usable added card, clear hand, and win')
                            return
            raise AssertionError(('Admin duel stalled',stage,lp))
        finally:
            for client in clients:client.close()
            if proc.poll() is None:proc.terminate()
            output=proc.communicate(timeout=5)[0]
            if output.strip():print(output[-1800:])

if __name__=='__main__':
    if sys.argv[1]=='--server':child(Path(sys.argv[2]),sys.argv[3],int(sys.argv[4]))
    else:run(Path(sys.argv[1]).resolve())
