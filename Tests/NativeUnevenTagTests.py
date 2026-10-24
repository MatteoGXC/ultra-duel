"""Play actual TCP tag turns with three players, including both turn orders."""
import argparse
import collections
import ctypes
import os
from pathlib import Path
import select
import shutil
import socket
import struct
import subprocess
import sys
import tempfile
import time

ROOT = Path(os.environ.get('ULTRA_TEST_CONTENT',Path(__file__).resolve().parents[1]))

def server(native, runtime, port):
    os.chdir(runtime)
    print('Starting native tag server', port, flush=True)
    handle = os.add_dll_directory(str(native)) if os.name=='nt' else None
    suffix = '.dylib' if sys.platform=='darwin' else '.so'
    ctypes.CDLL(str(native/('ocgcore.dll' if os.name=='nt' else 'libocgcore'+suffix)), mode=ctypes.RTLD_GLOBAL)
    library = ctypes.CDLL(str(native/('ygoserver.dll' if os.name=='nt' else 'libygoserver'+suffix)))
    print('Native libraries loaded', flush=True)
    library.start_server.argtypes = [ctypes.c_char_p]
    library.start_server(f'{port} 0 1 2 7 T T 8000 5 1 0 0'.encode())

def play(native, count, solo_first):
    with tempfile.TemporaryDirectory(prefix='ultra-tag-') as folder:
        runtime = Path(folder)
        (runtime/'data').mkdir()
        (runtime/'Data').mkdir(exist_ok=True)
        shutil.copy2(ROOT/'Data/locales/en-US/cards.cdb', runtime/'cards.cdb')
        (runtime/'Data/locales/zh-CN').mkdir(parents=True)
        shutil.copy2(ROOT/'Data/locales/en-US/cards.cdb', runtime/'Data/locales/zh-CN/cards.cdb')
        shutil.copy2(ROOT/'Data/script.zip', runtime/'data/script.zip')
        for name in ('lflist.conf', 'lflist_genesys.conf'):
            shutil.copy2(ROOT/'Data'/name, runtime/'Data'/name)
        with socket.socket() as probe:
            probe.bind(('127.0.0.1', 0)); port = probe.getsockname()[1]
        proc = subprocess.Popen([sys.executable, __file__, '--server', str(native), folder, str(port)],
                                stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
        clients = []
        buffers = []
        def send(index, op, data=b''):
            clients[index].sendall(struct.pack('<HB', len(data)+1, op)+data)
        try:
            for index in range(count):
                for attempt in range(600):
                    # A socket that failed to connect cannot be reused reliably
                    # on Darwin. Retry startup with a fresh socket each time.
                    client = socket.socket()
                    try: client.connect(('127.0.0.1', port)); break
                    except OSError:
                        client.close()
                        if proc.poll() is not None: raise AssertionError(proc.stdout.read().decode())
                        time.sleep(.05)
                else: raise AssertionError('Server did not listen')
                clients.append(client); buffers.append(bytearray())
                send(index, 0x10, ('Player '+str(index)).encode('utf-16-le').ljust(40, b'\0'))
                send(index, 0x12, struct.pack('<HBBI', 0x1362, 204, 204, 0)+bytes(40))
            ready = set(); confirmed = set(); started = False; turns = 0
            owners = collections.Counter(); swaps = collections.Counter(); won = False; trace=[]
            deadline = time.monotonic()+35
            while time.monotonic()<deadline:
                readable, _, _ = select.select(clients, [], [], .2)
                for client in readable:
                    index = clients.index(client)
                    body = client.recv(65536)
                    if not body:
                        assert won, 'Server disconnected before the match completed'
                        continue
                    buffers[index].extend(body)
                    buffer = buffers[index]
                    while len(buffer)>=2:
                        length = struct.unpack_from('<H', buffer)[0]
                        if len(buffer)<length+2: break
                        frame = bytes(buffer[2:length+2]); del buffer[:length+2]
                        op = frame[0]
                        if op==0x9:
                            assert struct.unpack_from('<H',frame,1)[0]==40, ('Empty starting deck',index,frame.hex())
                        if op==2: raise AssertionError(('Native room error', index, frame.hex()))
                        if op==0x13 and index not in ready:
                            # Forty vanillas make turn advancement deterministic and avoid chains.
                            send(index, 2, struct.pack('<II',40,0)+struct.pack('<40I', *([89631139]*40)))
                            send(index, 0x22); ready.add(index)
                        if op==0x21 and (frame[1]&15)==9:
                            if index==0: confirmed.add(frame[1]>>4)
                            if len(confirmed)==count and not started:
                                send(0,0x25);started=True
                        if op==3: send(index, 3, bytes([1 if index==0 else 3]))
                        if op==4: send(index, 4, bytes([0 if solo_first else 1]))
                        if op!=1 or len(frame)<2: continue
                        message = frame[1]
                        if index==0 and len(trace)<35:trace.append(frame[:35].hex())
                        if message==1: raise AssertionError(('Engine rejected response',index))
                        if message==40 and index==0: turns+=1
                        if message==161 and index==0: swaps[frame[2]]+=1
                        if message==16: send(index,1,struct.pack('<i',-1))
                        if message==15:
                            # The solo player draws on every opposing turn cycle;
                            # discard down to the hand limit during the End Phase.
                            minimum=frame[4]
                            send(index,1,bytes([minimum])+bytes(range(minimum)))
                        if message==11:
                            owners[index]+=1
                            if turns>=7:
                                if count==3:
                                    assert owners[2]>=3, owners
                                    assert swaps[0 if solo_first else 1]==0, swaps
                                    send(2,0x14)
                                else:
                                    assert all(owners[i]>0 for i in range(4)), owners
                                if count==4:
                                    print('PASS 2v2 retains all four controllers',dict(owners),flush=True)
                                    return
                            else: send(index,1,struct.pack('<i',7))
                        if message==5 and count==3:
                            won=True
                            assert turns>=7 and frame[2:]==bytes([1 if solo_first else 0,0]), ('Unexpected duel end',turns,frame.hex())
                            assert all(owners[i]>0 for i in range(3)), (owners,frame.hex(),trace)
                            print('PASS 2v1', 'solo first' if solo_first else 'team first',
                                  'turns',turns,'controllers',dict(owners),'swaps',dict(swaps),flush=True)
                            return
            raise AssertionError(('Tag duel stalled',count,solo_first,turns,dict(owners),dict(swaps)))
        finally:
            for client in clients: client.close()
            if proc.poll() is None: proc.terminate()
            output = proc.communicate(timeout=5)[0].decode(errors='replace')
            if output.strip(): print(output[-3000:])

if __name__=='__main__':
    if len(sys.argv)>1 and sys.argv[1]=='--server':
        server(Path(sys.argv[2]), sys.argv[3], int(sys.argv[4]))
    else:
        parser=argparse.ArgumentParser();parser.add_argument('--native',type=Path,required=True)
        native=parser.parse_args().native.resolve()
        play(native,3,False);play(native,3,True);play(native,4,False)

