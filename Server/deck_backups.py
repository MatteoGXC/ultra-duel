"""Authenticated, revision-checked deck backups. User IDs come only from sessions."""
import hashlib
import re


def initialize(db):
    db.executescript('''
    create table if not exists deck_saves(user integer not null, name_key text not null,
        name text not null, content text not null, sha256 text not null, updated integer not null,
        primary key(user,name_key));
    create table if not exists deck_versions(id integer primary key, user integer not null,
        name_key text not null, content text not null, sha256 text not null, updated integer not null);
    ''')


def route(db, method, body, now, uid, Problem):
    if method == 'GET':
        return {'decks': [dict(row) for row in db.execute(
            'select name,content,sha256,updated from deck_saves where user=? order by name_key', (uid,))]}
    if method != 'POST':
        raise Problem('Unsupported deck backup request.',405)
    name = body.get('name','')
    content = body.get('content','')
    if not isinstance(name,str) or not isinstance(content,str):
        raise Problem('Invalid deck backup.')
    parts = name.split('/')
    if (len(name)>180 or not name.lower().endswith('.ydk') or
        any(not p or p in ('.','..') or p.endswith((' ','.')) or
            re.search(r'[\\<>:"|?*\x00-\x1f]',p) or
            re.fullmatch(r'(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(\..*)?',p,re.I) for p in parts)):
        raise Problem('Invalid deck filename.')
    if len(content.encode('utf-8'))>65536 or '\x00' in content or '#main' not in content:
        raise Problem('Invalid deck file or deck exceeds 64 KB.')
    key = name.casefold()
    digest = hashlib.sha256(content.encode('utf-8')).hexdigest()
    current = db.execute('select * from deck_saves where user=? and name_key=?',(uid,key)).fetchone()
    if current and current['sha256']==digest:
        return {'ok':True,'sha256':digest}
    if current and body.get('baseSha','')!=current['sha256']:
        raise Problem('This deck changed on another device. Sync again to keep both copies.',409)
    size,count = db.execute('select coalesce(sum(length(cast(content as blob))),0),count(*) from deck_saves where user=?',(uid,)).fetchone()
    if size - (len(current['content'].encode('utf-8')) if current else 0) + len(content.encode('utf-8'))>5*1024*1024 or (not current and count>=1000):
        raise Problem('Deck backup storage is full.',413)
    if current:
        db.execute('insert into deck_versions(user,name_key,content,sha256,updated) values(?,?,?,?,?)',
            (uid,key,current['content'],current['sha256'],current['updated']))
        db.execute('delete from deck_versions where user=? and name_key=? and id not in (select id from deck_versions where user=? and name_key=? order by id desc limit 10)',(uid,key,uid,key))
    db.execute('insert or replace into deck_saves values(?,?,?,?,?,?)',(uid,key,name,content,digest,now))
    return {'ok':True,'sha256':digest}
