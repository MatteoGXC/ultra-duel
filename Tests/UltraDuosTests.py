"""Four authenticated friends start a real native 2v2 duel on the public service."""
import argparse,json,secrets,socket,ssl,struct,time,urllib.request
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def main():
    p=argparse.ArgumentParser();p.add_argument('--certificate',type=Path,required=True);args=p.parse_args()
    context=ssl.create_default_context(cafile=str(args.certificate))
    def api(path,body=None,token='',method=None):
        req=urllib.request.Request('https://69.48.201.156:9444'+path,data=json.dumps(body).encode() if body is not None else None,method=method or ('POST' if body is not None else 'GET'),headers={'Content-Type':'application/json','Authorization':'Bearer '+token})
        return json.load(urllib.request.urlopen(req,context=context,timeout=20))
    prefix='UT_DUOS_'+secrets.token_hex(3);users=[api('/register',dict(name=prefix+'_'+str(i),password=secrets.token_urlsafe(20))) for i in range(4)]
    for friend in users[1:]:
        api('/friends/request',dict(name=friend['name']),users[0]['token']);api('/friends/accept',dict(id=users[0]['id']),friend['token'])
    room=api('/rooms',dict(format='TCG',cap=100,mode='2v2'),users[0]['token']);assert room['capacity']==4 and room['mode']=='2v2'
    clients=[];buffers=[bytearray() for _ in users];seats={};starts=set();ready=set()
    def send(i,op,payload=b''):clients[i].sendall(struct.pack('<HB',len(payload)+1,op)+payload)
    main=[];side=[];dest=main
    for line in (ROOT/'Deck/Starter - TCG.ydk').read_text(encoding='utf-8-sig').splitlines():
        if line=='!side':dest=side
        elif line.isdigit():dest.append(int(line))
    deck=struct.pack('<II',len(main),len(side))+struct.pack('<'+'I'*(len(main)+len(side)),*(main+side))
    try:
        for i,user in enumerate(users):
            joined=api('/rooms/'+room['id']+'/join',{},user['token']);c=socket.create_connection((joined['address'],joined['port']),timeout=10);c.settimeout(.05);clients.append(c)
            send(i,0x10,user['name'].encode('utf-16-le')[:38].ljust(40,b'\0'));send(i,0x12,struct.pack('<HBBI',0x1362,0,0,0)+joined['secret'].encode('utf-16-le').ljust(40,b'\0'))
        phase=0;until=time.monotonic()+30
        while time.monotonic()<until and len(starts)<4:
            for i,c in enumerate(clients):
                try:buffers[i].extend(c.recv(65535))
                except socket.timeout:pass
                b=buffers[i]
                while len(b)>=2 and len(b)>=struct.unpack_from('<H',b)[0]+2:
                    n=struct.unpack_from('<H',b)[0];packet=bytes(b[2:n+2]);del b[:n+2];op=packet[0]
                    if op==0x13:seats[i]=packet[1]&15
                    if op==0x21 and packet[1]&15==9:ready.add(packet[1]>>4)
                    if op==3:send(i,3,bytes([1 if seats.get(i)==0 else 3]))
                    if op==4:send(i,4,b'\x01')
                    if op==1 and len(packet)>1 and packet[1]==4:starts.add(i)
            if phase==0 and len(seats)==4:
                assert sorted(seats.values())==[0,1,2,3],seats
                for i in range(4):send(i,2,deck);send(i,0x22)
                phase=1
            if phase==1 and len(ready)==4:send(0,0x25);phase=2
        assert len(starts)==4,(seats,ready,starts)
        print('PASS public native Duos: four authenticated friends, seats 0-3, four legal decks/readies, RPS, turn choice and engine START delivered to all four.')
    finally:
        for c in clients:c.close()
        api('/rooms/'+room['id'],token=users[0]['token'],method='DELETE')
        for user in users:api('/logout',{},user['token'])
if __name__=='__main__':main()
