"""Private account notifications; callers cannot choose another recipient's inbox."""
def initialize(db):
    db.executescript('''
    create table if not exists notifications(id integer primary key autoincrement,
        user integer not null, kind text not null, peer integer not null,
        name text not null, room text not null default '', created integer not null,
        expires integer not null default 0, seen integer not null default 0);
    create index if not exists notifications_inbox on notifications(user,id);
    ''')


def add(db,uid,kind,peer,name,now,room='',expires=0):
    db.execute('insert into notifications(user,kind,peer,name,room,created,expires) values(?,?,?,?,?,?,?)',
        (uid,kind,peer,name,room,now,expires))
    db.execute('delete from notifications where user=? and id not in (select id from notifications where user=? order by id desc limit 100)',(uid,uid))


def inbox(db,uid):
    return [dict(r) for r in db.execute('select id,kind,peer,name,room,created,expires,seen from notifications where user=? order by id desc limit 100',(uid,))]


def read(db,uid,body,Problem):
    through=body.get('through',0)
    if not isinstance(through,int) or isinstance(through,bool) or not 0<through<2**63:
        raise Problem('Invalid notification.')
    db.execute('update notifications set seen=1 where user=? and id<=?',(uid,through))
    return {'ok':True,'notifications':inbox(db,uid)}
