"""Ultra Duel's private friends service and isolated native duel rooms.

Only this service owns the SQLite database. Native rooms receive a unique secret
and run in separate processes; the default embedded Solo server stays loopback.
"""
import contextlib
import ctypes
import hashlib
import hmac
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import json
import os
from pathlib import Path
import re
import secrets
import signal
import socket
import sqlite3
import ssl
import subprocess
import sys
import threading
import time
import player_features

STATE = Path(os.environ.get('ULTRA_STATE', '/state'))
HOST = os.environ.get('ULTRA_HOST', '69.48.201.156')
PORT = int(os.environ.get('ULTRA_PORT', '9444'))
ROOM_FIRST = int(os.environ.get('ULTRA_ROOM_FIRST', '7911'))
ROOM_COUNT = 12
ROOMS = {}
LOCK = threading.RLock()
RATES = {}

class Problem(Exception):
    def __init__(self, message, status=400): self.message=message; self.status=status

def database():
    db = sqlite3.connect(STATE/'friends.sqlite3', timeout=10)
    db.row_factory = sqlite3.Row
    db.execute('pragma foreign_keys=on')
    return db

def initialize():
    STATE.mkdir(parents=True, exist_ok=True)
    with database() as db:
        db.executescript('''
        pragma journal_mode=WAL;
        create table if not exists users(id integer primary key, name text not null unique collate nocase,
            salt text not null, password text not null, seen integer not null default 0);
        create table if not exists sessions(token text primary key, user integer not null references users(id), expires integer not null);
        create table if not exists friends(sender integer not null references users(id), recipient integer not null references users(id),
            accepted integer not null default 0, primary key(sender,recipient), check(sender<>recipient));
        ''')
        player_features.initialize(db)
    cert, key = STATE/'certificate.pem', STATE/'private-key.pem'
    if not cert.exists() or not key.exists():
        subprocess.run(['openssl','req','-x509','-newkey','rsa:3072','-sha256','-days','3650','-nodes',
            '-keyout',str(key),'-out',str(cert),'-subj','/CN=Ultra Duel Friends',
            '-addext',f'subjectAltName=IP:{HOST}'],check=True,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
        key.chmod(0o600)
    return cert, key

def password_hash(password, salt):
    return hashlib.scrypt(password.encode(), salt=bytes.fromhex(salt), n=16384, r=8, p=1).hex()

def session(db, uid):
    token = secrets.token_urlsafe(32)
    db.execute('insert into sessions values(?,?,?)', (hashlib.sha256(token.encode()).hexdigest(),uid,int(time.time())+2592000))
    db.execute('delete from sessions where expires < ?', (int(time.time()),))
    return token

def are_friends(db, a, b):
    return db.execute('select 1 from friends where accepted=1 and ((sender=? and recipient=?) or (sender=? and recipient=?))', (a,b,b,a)).fetchone() is not None

def clean_rooms():
    now = time.time()
    for rid,room in list(ROOMS.items()):
        if room['process'].poll() is not None or room['expires']<now:
            if room['process'].poll() is None: room['process'].terminate()
            room['log'].close()
            del ROOMS[rid]

def public_room(room):
    return {key:room[key] for key in ('id','host','hostId','format','cap','port','expires')}

class Handler(BaseHTTPRequestHandler):
    server_version = 'UltraDuel/0.1.3'
    def log_message(self, *_): pass # Passwords, tokens and private room secrets never enter access logs.
    def reply(self, data, status=200):
        body=json.dumps(data,separators=(',',':')).encode()
        self.send_response(status)
        self.send_header('Content-Type','application/json; charset=utf-8')
        self.send_header('Cache-Control','no-store')
        self.send_header('Content-Length',str(len(body)))
        self.end_headers(); self.wfile.write(body)
    def do_GET(self): self.handle_api('GET')
    def do_POST(self): self.handle_api('POST')
    def do_DELETE(self): self.handle_api('DELETE')
    def handle_api(self, method):
        try:
            ip=self.client_address[0]; now=int(time.time()); path=self.path.split('?')[0]
            bucket=(ip,now//60, 'auth' if path in ('/register','/login') else 'api')
            with LOCK:
                if len(RATES)>2000:
                    for k in list(RATES):
                        if k[1]<now//60-1: del RATES[k]
                RATES[bucket]=RATES.get(bucket,0)+1
                if RATES[bucket]>(12 if bucket[2]=='auth' else 180): raise Problem('Please wait before trying again.',429)
            length=int(self.headers.get('Content-Length','0'))
            if length<0 or length>8192: raise Problem('Request too large.',413)
            body=json.loads(self.rfile.read(length)) if length else {}
            if not isinstance(body,dict): raise Problem('Expected a JSON object.')
            with contextlib.closing(database()) as db:
                with db: result=self.route(db,method,path,body,now)
            self.reply(result)
        except Problem as e: self.reply({'error':e.message},e.status)
        except (ValueError,TypeError,KeyError,UnicodeError): self.reply({'error':'Invalid request.'},400)
        except sqlite3.IntegrityError: self.reply({'error':'That username or request already exists.'},409)
        except Exception as e:
            print('API_ERROR',type(e).__name__,flush=True)
            self.reply({'error':'Service temporarily unavailable. Please try again.'},503)
    def route(self, db, method, path, body, now):
        if path=='/health' and method=='GET': return {'ok':True,'version':'0.1.3'}
        if path in ('/register','/login') and method=='POST':
            name=str(body.get('name','')).strip(); password=str(body.get('password',''))
            if not re.fullmatch(r'[A-Za-z0-9_-]{3,24}',name): raise Problem('Use 3–24 letters, numbers, underscores or hyphens for your username.')
            if len(password)<8 or len(password)>128: raise Problem('Use a password between 8 and 128 characters.')
            user=db.execute('select * from users where name=?',(name,)).fetchone()
            if path=='/register':
                if user: raise Problem('That username is taken.',409)
                salt=secrets.token_hex(16)
                uid=db.execute('insert into users(name,salt,password,seen) values(?,?,?,?)', (name,salt,password_hash(password,salt),now)).lastrowid
            else:
                # Do the same expensive hash for nonexistent usernames.
                salt=user['salt'] if user else '0'*32
                computed=password_hash(password,salt)
                if not user or not hmac.compare_digest(computed,user['password']): raise Problem('Incorrect username or password.',401)
                uid=user['id']; name=user['name']
            return {'token':session(db,uid),'name':name,'id':uid}
        authorization=self.headers.get('Authorization','')
        token=authorization[7:] if authorization.startswith('Bearer ') else ''
        user=db.execute('select users.* from users join sessions on users.id=sessions.user where sessions.token=? and sessions.expires>?',
            (hashlib.sha256(token.encode()).hexdigest(),now)).fetchone()
        if not user: raise Problem('Please sign in again.',401)
        uid=user['id']; db.execute('update users set seen=? where id=?',(now,uid))
        feature = player_features.route(self,db,method,path,body,now,user,(STATE,Problem,are_friends,ROOMS,LOCK))
        if feature is not None: return feature
        if path=='/logout' and method=='POST':
            db.execute('delete from sessions where token=?',(hashlib.sha256(token.encode()).hexdigest(),))
            return {'ok':True}
        if path=='/state' and method=='GET':
            friends=[]; requests=[]; sent=[]
            for row in db.execute('''select f.sender,f.recipient,f.accepted,u.id,u.name,u.seen from friends f
                join users u on u.id=case when f.sender=? then f.recipient else f.sender end
                where f.sender=? or f.recipient=? order by u.name''',(uid,uid,uid)):
                activity=db.execute('select activity,mate from profiles where user=?',(row['id'],)).fetchone()
                record={'id':row['id'],'name':row['name'],'online':row['seen']>now-120,'activity':activity[0] if activity and row['seen']>now-120 else ('Online' if row['seen']>now-120 else 'Offline'),'mate':activity[1] if activity else 1000001}
                (friends if row['accepted'] else sent if row['sender']==uid else requests).append(record)
            with LOCK:
                clean_rooms()
                rooms=[public_room(r) for r in ROOMS.values() if r['hostId']==uid or are_friends(db,uid,r['hostId'])]
            return {'id':uid,'name':user['name'],'friends':friends,'requests':requests,'sent':sent,'rooms':rooms,'admin':player_features.is_admin(STATE,user['name']),'profile':player_features.profile(db,uid),'invites':[dict(r) for r in db.execute('select i.room,u.name,i.sender from invites i join users u on u.id=i.sender where i.recipient=? and i.expires>?',(uid,now))]}
        if path=='/friends/request' and method=='POST':
            target=db.execute('select id from users where name=?',(str(body.get('name','')).strip(),)).fetchone()
            if not target: raise Problem('No player has that username.',404)
            other=target['id']
            if other==uid: raise Problem('Enter another player’s username.')
            if db.execute('select 1 from friends where (sender=? and recipient=?) or (sender=? and recipient=?)',(uid,other,other,uid)).fetchone(): raise Problem('A friendship or pending request already exists.',409)
            db.execute('insert into friends values(?,?,0)',(uid,other)); return {'ok':True}
        if path=='/friends/accept' and method=='POST':
            count=db.execute('update friends set accepted=1 where sender=? and recipient=? and accepted=0',(int(body['id']),uid)).rowcount
            if not count: raise Problem('Friend request not found.',404)
            return {'ok':True}
        if path.startswith('/friends/') and method=='DELETE':
            other=int(path.rsplit('/',1)[1]); db.execute('delete from friends where (sender=? and recipient=?) or (sender=? and recipient=?)',(uid,other,other,uid)); return {'ok':True}
        if path=='/rooms' and method=='POST':
            mode=str(body.get('format','TCG')); cap=int(body.get('cap',100))
            if mode not in ('TCG','Genesys') or not 0<=cap<=10000: raise Problem('Invalid duel format or point cap.')
            with LOCK:
                clean_rooms()
                if any(r['hostId']==uid for r in ROOMS.values()): raise Problem('Close your existing room before creating another.')
                used={r['port'] for r in ROOMS.values()}
                port=next((p for p in range(ROOM_FIRST,ROOM_FIRST+ROOM_COUNT) if p not in used),None)
                if port is None: raise Problem('All duel rooms are busy. Please try again later.',503)
                rid=secrets.token_hex(8); secret=secrets.token_hex(8)
                env=dict(os.environ,ULTRA_ROOM_SECRET=secret)
                log=(STATE/f'room-{rid}.log').open('ab')
                proc=subprocess.Popen([sys.executable,str(Path(__file__).resolve()),'--room',str(port),mode,str(cap)],env=env,stdout=log,stderr=log)
                ready=False
                for _ in range(40):
                    if proc.poll() is not None: break
                    try:
                        with socket.create_connection(('127.0.0.1',port),timeout=.1): ready=True; break
                    except OSError: time.sleep(.1)
                if not ready:
                    proc.terminate(); log.close(); raise Problem('The duel room could not start.',503)
                ROOMS[rid]=dict(id=rid,host=user['name'],hostId=uid,format=mode,cap=cap,port=port,secret=secret,process=proc,log=log,expires=now+3600)
                return dict(public_room(ROOMS[rid]),address=HOST,secret=secret)
        if path.startswith('/rooms/'):
            rid=path.split('/')[2]
            with LOCK:
                clean_rooms(); room=ROOMS.get(rid)
                if not room:
                    if method=='DELETE': return {'ok':True}
                    raise Problem('This room has closed.',404)
                if method=='DELETE' and room['hostId']==uid:
                    room['process'].terminate(); room['log'].close(); del ROOMS[rid]; return {'ok':True}
                if method=='POST' and path.endswith('/join'):
                    if room['hostId']!=uid and not are_friends(db,uid,room['hostId']): raise Problem('Only the host’s friends can join this room.',403)
                    return dict(public_room(room),address=HOST,secret=room['secret'])
        raise Problem('Not found.',404)

def room_worker():
    port=int(sys.argv[2]); genesys=sys.argv[3]=='Genesys'; cap=int(sys.argv[4])
    core=ctypes.CDLL('/app/native/libocgcore.so',mode=ctypes.RTLD_GLOBAL)
    server=ctypes.CDLL('/app/native/libygoserver.so')
    server.configure_remote_room.argtypes=[ctypes.c_char_p]
    server.configure_remote_room(os.environ['ULTRA_ROOM_SECRET'].encode())
    server.set_local_genesys_point_cap.argtypes=[ctypes.c_uint]; server.set_local_genesys_point_cap(cap)
    server.start_server.argtypes=[ctypes.c_char_p]; server.start_server.restype=ctypes.c_int
    args=f"{port} {'-1' if genesys else '0'} 1 0 {'8' if genesys else '7'} F F 8000 5 1 0 0"
    # The native entry point blocks until the last player leaves. Its return
    # value is main()'s exit status, not the bound port.
    server.start_server(args.encode())

def main():
    if len(sys.argv)>1 and sys.argv[1]=='--room': return room_worker()
    cert,key=initialize()
    context=ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER); context.minimum_version=ssl.TLSVersion.TLSv1_2
    context.load_cert_chain(cert,key)
    server=ThreadingHTTPServer(('0.0.0.0',PORT),Handler)
    server.socket=context.wrap_socket(server.socket,server_side=True)
    print('ULTRA_FRIENDS_READY',PORT,flush=True)
    try: server.serve_forever()
    finally:
        for room in ROOMS.values(): room['process'].terminate()

if __name__=='__main__': main()
