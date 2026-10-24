"""Exercise the deployed HTTPS friends API and remote native TCP listeners."""
import argparse
import json
from pathlib import Path
import secrets
import socket
import ssl
import struct
import time
import urllib.error
import urllib.request

ROOT = Path(__file__).resolve().parents[1]

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--certificate',type=Path,required=True)
    parser.add_argument('--url',default='https://69.48.201.156:9444')
    args=parser.parse_args()
    context=ssl.create_default_context(cafile=str(args.certificate))
    def api(path,method='GET',data=None,token='',expected=200):
        headers={'Content-Type':'application/json'}
        if token: headers['Authorization']='Bearer '+token
        req=urllib.request.Request(args.url+path,headers=headers,method=method,
            data=json.dumps(data).encode() if data is not None else None)
        try:
            with urllib.request.urlopen(req,context=context,timeout=20) as response:
                code=response.status; result=json.load(response)
        except urllib.error.HTTPError as error: code=error.code; result=json.load(error)
        assert code==expected,(path,code,result)
        return result
    prefix='UT_'+secrets.token_hex(4)
    password=secrets.token_urlsafe(18)
    a=api('/register','POST',{'name':prefix+'_a','password':password})
    b=api('/register','POST',{'name':prefix+'_b','password':password})
    c=api('/register','POST',{'name':prefix+'_c','password':password})
    assert api('/login','POST',{'name':a['name'],'password':password})['id']==a['id']
    api('/state',expected=401)
    api('/login','POST',{'name':a['name'],'password':'incorrect-password'},expected=401)
    api('/friends/request','POST',{'name':b['name']},a['token'])
    state=api('/state',token=b['token'])
    assert state['requests'][0]['id']==a['id']
    api('/friends/accept','POST',{'id':a['id']},b['token'])
    assert api('/state',token=a['token'])['friends'][0]['online']
    api('/presence','POST',{'activity':'In duel'},b['token'])
    assert api('/state',token=a['token'])['friends'][0]['activity']=='In duel'
    api('/admin/players',token=a['token'],expected=403)
    for n in range(2):
        nonce=secrets.token_hex(16)
        api('/games/start','POST',{'nonce':nonce,'deck':'API fixture','opponent':'Fixture'},a['token'])
        for _ in range(2):api('/games/finish','POST',{'nonce':nonce,'result':'WIN','turns':3},a['token'])
    for _ in range(2):api('/pass/claim','POST',{},a['token'])
    profile=api('/profile',token=a['token'])
    assert profile['xp']==200 and profile['gems']==100
    for _ in range(2):api('/missions/claim','POST',{},a['token'])
    assert api('/profile',token=a['token'])['gems']==170
    print('PASS public daily/grade mission claims and duplicate Receive All',flush=True)
    print('PASS register, login, authentication, friend requests, acceptance and presence',flush=True)
    print('PASS public account XP/gems, idempotent win/claim, in-duel presence and unauthorized admin denial',flush=True)
    def frames(client):
        data=bytearray()
        while True:
            try:
                part=client.recv(65536)
                if not part:break
                data.extend(part)
            except socket.timeout:break
        result=[]
        while len(data)>=2:
            size=struct.unpack_from('<H',data)[0]
            assert len(data)>=size+2,'Truncated frame'
            result.append(bytes(data[2:size+2])); del data[:size+2]
        return result
    def send(client,op,data=b''): client.sendall(struct.pack('<HB',len(data)+1,op)+data)
    def connect(room,secret):
        client=socket.create_connection((room['address'],room['port']),timeout=10)
        client.settimeout(.5)
        send(client,0x10,'Ultra Test'.encode('utf-16-le').ljust(40,b'\0'))
        send(client,0x12,struct.pack('<HBBI',0x1362,0,0,0)+secret.encode('utf-16-le').ljust(40,b'\0'))
        return client,frames(client)
    def deck(name):
        main=[];side=[];dest=main
        for line in (ROOT/'Deck'/('Starter - '+name+'.ydk')).read_text(encoding='utf-8-sig').splitlines():
            if line=='!side':dest=side
            elif line.isdigit():dest.append(int(line))
        return struct.pack('<II',len(main),len(side))+struct.pack('<'+'I'*(len(main)+len(side)),*(main+side))
    for mode,cap in [('TCG',100),('Genesys',100),('Genesys',0)]:
        room=api('/rooms','POST',{'format':mode,'cap':cap},a['token'])
        api('/invites','POST',{'id':b['id'],'room':room['id']},a['token'])
        assert any(i['room']==room['id'] for i in api('/state',token=b['token'])['invites'])
        api('/invites','POST',{'id':c['id'],'room':room['id']},a['token'],expected=403)

        try:
            api('/rooms/'+room['id']+'/join','POST',{},c['token'],403)
            assert not api('/state',token=c['token'])['rooms']
            join=api('/rooms/'+room['id']+'/join','POST',{},b['token'])
            assert join['format']==mode and join['cap']==cap
            wrong,rejected=connect(room,'0'*16)
            assert not any(f[0]==0x12 for f in rejected),'Wrong secret joined the room'
            assert wrong.recv(1)==b'','Wrong secret was not disconnected'
            wrong.close()
            first,joined=connect(room,room['secret']);assert any(f[0]==0x12 for f in joined)
            second,joined=connect(join,join['secret']);assert any(f[0]==0x12 for f in joined)
            send(first,2,deck(mode));send(first,0x22);ready=frames(first)
            playing_deck=mode
            if mode=='Genesys' and cap==0:
                assert any(f[0]==2 for f in ready),'Genesys zero cap was not enforced'
                playing_deck='Genesys Zero'
                send(first,2,deck(playing_deck));send(first,0x22);ready=frames(first)
            assert any(f[0]==0x21 and f[1]&15==9 for f in ready),(mode,ready)
            send(second,2,deck(playing_deck));send(second,0x22);frames(second);frames(first)
            send(first,0x25);started=frames(first)
            assert any(f[0] in (0x15,0x03) for f in started),(mode,'Duel did not start',started)
            first.close();second.close()
            print('PASS remote room',mode,cap,'friend authorization, secret, two-player join and rules',flush=True)
        finally:api('/rooms/'+room['id'],'DELETE',token=a['token'])
    api('/friends/'+str(b['id']),'DELETE',token=a['token'])
    assert not api('/state',token=b['token'])['friends']
    api('/logout','POST',{},a['token']);api('/state',token=a['token'],expected=401)
    print('PASS friend removal and session revocation; deployed service end-to-end checks complete',flush=True)
    (ROOT.parent/'checks/ultra-service-test-users.json').write_text(json.dumps([a['name'],b['name'],c['name']]))

if __name__=='__main__':main()
