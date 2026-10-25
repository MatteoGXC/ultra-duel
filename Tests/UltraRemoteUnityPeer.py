"""Pair the real Windows player with a native TCP peer on the public VPS."""
import argparse
import json
import os
from pathlib import Path
import secrets
import socket
import ssl
import struct
import subprocess
import urllib.request

ROOT=Path(__file__).resolve().parents[1]

def main():
    p=argparse.ArgumentParser()
    p.add_argument('--certificate',type=Path,required=True)
    p.add_argument('--game',type=Path,default=ROOT/'release/Ultra-Duel')
    p.add_argument('--mode',choices=['TCG','Genesys'],required=True)
    p.add_argument('--cap',type=int,default=100)
    args=p.parse_args()
    context=ssl.create_default_context(cafile=str(args.certificate))
    def api(path,method='GET',body=None,token=''):
        h={'Content-Type':'application/json'}
        if token:h['Authorization']='Bearer '+token
        req=urllib.request.Request('https://69.48.201.156:9444'+path,headers=h,method=method,
            data=json.dumps(body).encode() if body is not None else None)
        with urllib.request.urlopen(req,context=context,timeout=20) as response:return json.load(response)
    prefix='UT_REMOTE_'+secrets.token_hex(3);password=secrets.token_urlsafe(20)
    host=api('/register','POST',dict(name=prefix+'_h',password=password))
    player=api('/register','POST',dict(name=prefix+'_p',password=password))
    api('/friends/request','POST',dict(name=player['name']),host['token'])
    api('/friends/accept','POST',dict(id=host['id']),player['token'])
    room=api('/rooms','POST',dict(format=args.mode,cap=args.cap),host['token'])
    room=api('/rooms/'+room['id']+'/join','POST',{},player['token'])
    game=args.game.resolve()
    session=game/'Data/friends-session.json';before=session.read_bytes() if session.exists() else None
    output=ROOT.parent/'checks'/f'ultra-rebrand-remote-{args.mode}-{args.cap}'
    output.mkdir(parents=True,exist_ok=True)
    fixture=output/'room.json'
    fixture.write_bytes(json.dumps(room).encode())
    peer=None
    try:
        peer=socket.create_connection((room['address'],room['port']),timeout=10)
        def send(op,data=b''):peer.sendall(struct.pack('<HB',len(data)+1,op)+data)
        send(0x10,'VPS Peer'.encode('utf-16-le').ljust(40,b'\0'))
        send(0x12,struct.pack('<HBBI',0x1362,0,0,0)+room['secret'].encode('utf-16-le').ljust(40,b'\0'))
        main=[];side=[];dest=main
        deck='TCG' if args.mode=='TCG' else 'Genesys Zero' if args.cap==0 else 'Genesys'
        for line in (ROOT/'Deck'/f'Starter - {deck}.ydk').read_text(encoding='utf-8-sig').splitlines():
            if line=='!side':dest=side
            elif line.isdigit():dest.append(int(line))
        send(2,struct.pack('<II',len(main),len(side))+struct.pack('<'+'I'*(len(main)+len(side)),*(main+side)))
        send(0x22)
        session.write_bytes(json.dumps(dict(token=player['token'],name=player['name'])).encode())
        result=subprocess.run([str(game/'Ultra-Duel.exe'),'--format-smoke',str(output),args.mode,
            '--ultra-remote-room',str(fixture),'-screen-fullscreen','0','-screen-width','1280','-screen-height','720',
            '-logFile',str(output/'runtime.log')],cwd=game,timeout=110,creationflags=subprocess.CREATE_NO_WINDOW)
        assert result.returncode==0 and (output/'passed.txt').exists(), 'Remote Unity flow did not pass; see runtime.log'
        print('PASS actual Windows player joined public VPS with two ready players:',args.mode,args.cap)
    finally:
        if peer:peer.close()
        api('/rooms/'+room['id'],'DELETE',token=host['token'])
        api('/logout','POST',{},host['token']);api('/logout','POST',{},player['token'])
        if before is not None:session.write_bytes(before)
        elif session.exists():session.unlink()
        if fixture.exists():fixture.unlink()

if __name__=='__main__':main()
