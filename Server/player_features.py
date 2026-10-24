"""Account cosmetics, invitations and owner tools. No client can grant currency."""
import json
import os
from pathlib import Path
import re
import time


def initialize(db):
    db.executescript('''
    create table if not exists mission_claims(user integer not null, mission text not null, primary key(user,mission));
    create table if not exists profiles(user integer primary key references users(id), gems integer not null default 0,
        xp integer not null default 0, claimed integer not null default 0, gold integer not null default 0,
        activity text not null default 'Home', mate integer not null default 1000001);
    create table if not exists owned(user integer not null, item text not null, primary key(user,item));
    create table if not exists games(user integer not null, nonce text not null, deck text not null, opponent text not null,
        mode text not null, started integer not null, result text, turns integer, primary key(user,nonce));
    create table if not exists grants(id integer primary key, actor integer not null, recipient integer not null,
        amount integer not null, reason text not null, created integer not null);
    create table if not exists invites(sender integer not null, recipient integer not null, room text not null,
        expires integer not null, primary key(sender,recipient));
    ''')
    if 'avatar' not in {r[1] for r in db.execute('pragma table_info(profiles)')}:
        db.execute('alter table profiles add column avatar integer not null default 0')


def is_admin(state, name):
    # Local VPS file, never a shipped client code or a registration-controlled field.
    try:
        names = json.loads((state/'admins.json').read_text())
        return name.casefold() in [str(n).casefold() for n in names]
    except (OSError, ValueError):
        return False


def mission_rows(db, uid, now=None):
    now = int(time.time()) if now is None else now
    day = now//86400
    games = db.execute("select result,started from games where user=? and result is not null",(uid,)).fetchall()
    played=len(games); wins=sum(r['result']=='WIN' for r in games)
    daily_played=sum(r['started']//86400==day for r in games)
    daily_wins=sum(r['started']//86400==day and r['result']=='WIN' for r in games)
    xp=db.execute('select xp from profiles where user=?',(uid,)).fetchone()
    tier=min(20,(xp[0] if xp else 0)//200)
    claims={r[0] for r in db.execute('select mission from mission_claims where user=?',(uid,))}
    specs=[('daily-play','Daily','Complete a duel',daily_played,1,20),('daily-win','Daily','Win a duel',daily_wins,1,30)]
    specs += [(f'played-{n}','Lifetime','Complete duels',played,n,g) for n,g in ((5,50),(20,100))]
    specs += [(f'wins-{n}','Lifetime','Win duels',wins,n,g) for n,g in ((5,50),(20,100))]
    specs += [(f'grade-{n}','Duel Pass','Raise your Duel Pass grade',tier,n,g) for n,g in ((1,20),(5,50),(10,100),(20,200))]
    return [dict(id=key,category=cat,title=title,progress=min(progress,target),target=target,gems=gems,
        claimed=(key+(':'+str(day) if cat=='Daily' else '')) in claims) for key,cat,title,progress,target,gems in specs]


def profile(db, uid):
    db.execute('insert or ignore into profiles(user) values(?)', (uid,))
    p = dict(db.execute('select * from profiles where user=?', (uid,)).fetchone())
    p['missions'] = mission_rows(db,uid)
    p['owned'] = [r[0] for r in db.execute('select item from owned where user=?', (uid,))]
    p['wins'] = db.execute("select count(*) from games where user=? and result='WIN'", (uid,)).fetchone()[0]
    p['played'] = db.execute('select count(*) from games where user=? and result is not null', (uid,)).fetchone()[0]
    best = db.execute('select deck,count(*) n from games where user=? and result is not null group by deck order by n desc,deck limit 1', (uid,)).fetchone()
    p['favoriteDeck'] = best[0] if best else 'No games yet'
    return p


def route(h, db, method, path, body, now, user, api):
    uid = user['id']; state, Problem, are_friends, rooms, lock = api
    if path == '/profile' and method == 'GET':
        return profile(db, uid)
    if path == '/profile/avatar' and method == 'POST':
        avatar=int(body.get('card',0))
        if not 0<=avatar<=999999999:raise Problem('Invalid profile icon.')
        profile(db,uid)
        db.execute('update profiles set avatar=? where user=?',(avatar,uid))
        return profile(db,uid)
    if path == '/missions/claim' and method == 'POST':
        profile(db,uid)
        for row in mission_rows(db,uid,now):
            if body.get('id','all') not in ('all',row['id']) or row['claimed'] or row['progress']<row['target']: continue
            key=row['id']+(':'+str(now//86400) if row['category']=='Daily' else '')
            inserted=db.execute('insert or ignore into mission_claims values(?,?)',(uid,key)).rowcount
            if inserted: db.execute('update profiles set gems=gems+? where user=?',(row['gems'],uid))
        return profile(db,uid)
    if path == '/presence' and method == 'POST':
        activity = body.get('activity', 'Home')
        if activity not in ('Home', 'In duel', 'In room', 'Deck editing', 'Solo'): raise Problem('Invalid status.')
        p=profile(db, uid)
        mate=int(body.get('mate',p['mate']))
        if mate!=1000001 and 'Mate:'+str(mate) not in p['owned']: mate=1000001
        db.execute('update profiles set activity=?,mate=? where user=?', (activity,mate,uid))
        return {'ok':True}
    if path == '/games/start' and method == 'POST':
        nonce = str(body.get('nonce',''))
        if not re.fullmatch('[a-f0-9]{32}', nonce): raise Problem('Invalid game identifier.')
        profile(db, uid)
        db.execute('insert or ignore into games(user,nonce,deck,opponent,mode,started) values(?,?,?,?,?,?)',
            (uid,nonce,str(body.get('deck',''))[:128],str(body.get('opponent',''))[:64],str(body.get('mode','Solo'))[:32],now))
        db.execute("update profiles set activity='In duel' where user=?", (uid,))
        return {'ok':True}
    if path == '/games/finish' and method == 'POST':
        outcome = body.get('result')
        if outcome not in ('WIN','LOSS','DRAW'): raise Problem('Invalid result.')
        count = db.execute('update games set result=?,turns=? where user=? and nonce=? and result is null',
            (outcome,max(0,min(999,int(body.get('turns',0)))),uid,str(body.get('nonce','')))).rowcount
        if count and outcome == 'WIN': db.execute('update profiles set xp=xp+100,gems=gems+25 where user=?', (uid,))
        db.execute("update profiles set activity='Home' where user=?", (uid,))
        return profile(db, uid)
    if path == '/pass/claim' and method == 'POST':
        p = profile(db, uid); tier = min(20,p['xp']//200)
        count = max(0,tier-p['claimed'])
        db.execute('update profiles set claimed=?,gems=gems+? where user=?', (tier,count*50,uid))
        for level,key in ((10,'Frame:Neon'),(20,'Frame:Champion')):
            if tier>=level: db.execute('insert or ignore into owned values(?,?)',(uid,key))
        return profile(db, uid)
    if path == '/shop/buy' and method == 'POST':
        catalog = json.loads((Path(__file__).parent/'shop-catalog.json').read_text())
        item = next((i for i in catalog if i['key']==body.get('item')),None)
        if not item: raise Problem('This item is unavailable.',404)
        p = profile(db, uid)
        if item['key'] not in p['owned']:
            if p['gems'] < item['price']: raise Problem('Not enough gems.')
            db.execute('update profiles set gems=gems-? where user=?', (item['price'],uid))
            db.execute('insert into owned values(?,?)', (uid,item['key']))
        return profile(db, uid)
    if path == '/invites' and method == 'POST':
        other = int(body['id']); rid = str(body['room'])
        if not are_friends(db,uid,other): raise Problem('You can only invite friends.',403)
        with lock:
            room = rooms.get(rid)
            if not room or room['hostId'] != uid: raise Problem('Create a room first.')
        db.execute('insert or replace into invites values(?,?,?,?)', (uid,other,rid,now+300))
        return {'ok':True}
    if path == '/admin/players' and method == 'GET':
        if not is_admin(state,user['name']): raise Problem('Administrator permission required.',403)
        return {'players':[dict(id=r['id'],name=r['name'],online=r['seen']>now-120,
            activity=r['activity'] if r['seen']>now-120 else 'Offline',gems=r['gems'] or 0)
            for r in db.execute("select u.id,u.name,u.seen,coalesce(p.activity,'Home') activity,p.gems from users u left join profiles p on p.user=u.id order by u.name")]}
    if path == '/admin/grant' and method == 'POST':
        if not is_admin(state,user['name']): raise Problem('Administrator permission required.',403)
        other=int(body['id']); amount=int(body['amount']); reason=str(body.get('reason','Owner reward'))[:200]
        if not 1<=amount<=100000: raise Problem('Grant must be between 1 and 100,000 gems.')
        if not db.execute('select 1 from users where id=?',(other,)).fetchone(): raise Problem('Player not found.',404)
        profile(db,other)
        db.execute('update profiles set gems=gems+? where user=?',(amount,other))
        db.execute('insert into grants(actor,recipient,amount,reason,created) values(?,?,?,?,?)',(uid,other,amount,reason,now))
        return {'ok':True}
    return None
