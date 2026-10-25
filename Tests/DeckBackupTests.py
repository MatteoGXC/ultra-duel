"""Exercise real authenticated routes, revision races and reinstall recovery."""
import hashlib
import sqlite3
import sys
import time
import unittest
from pathlib import Path
from types import SimpleNamespace

sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'Server'))
import service
import player_features


class DeckBackups(unittest.TestCase):
    def setUp(self):
        self.db=sqlite3.connect(':memory:');self.db.row_factory=sqlite3.Row
        self.db.executescript('create table users(id integer primary key,name text,seen integer);create table sessions(token text,user integer,expires integer);insert into users values(1,"a",0),(2,"b",0);')
        player_features.initialize(self.db)
        for uid in (1,2):self.db.execute('insert into sessions values(?,?,?)',(hashlib.sha256(str(uid).encode()).hexdigest(),uid,int(time.time())+100))
    def tearDown(self):self.db.close()
    def call(self,uid=1,method='GET',body=None):
        return service.Handler.route(SimpleNamespace(headers={'Authorization':'Bearer '+str(uid)}),self.db,method,'/decks',body or {},int(time.time()))
    def save(self,name='my deck.ydk',content='#main\n123\n#extra\n!side',base='',uid=1):
        return self.call(uid,'POST',dict(name=name,content=content,baseSha=base,user=2))
    def test_account_isolation_and_login_restore(self):
        with self.assertRaises(service.Problem) as e:self.call(999)
        self.assertEqual(e.exception.status,401)
        saved=self.save();rows=self.call()['decks']
        self.assertEqual(rows[0]['sha256'],saved['sha256'])
        self.assertEqual(self.call(2)['decks'],[])
        self.assertEqual(rows[0]['content'],'#main\n123\n#extra\n!side')
        self.save(uid=2,content='#main\n456')
        self.assertNotEqual(self.call()['decks'][0]['content'],self.call(2)['decks'][0]['content'])
    def test_optimistic_conflicts_keep_existing_and_revision_history(self):
        old=self.save()
        with self.assertRaises(service.Problem) as e:self.save(content='#main\n456')
        self.assertEqual(e.exception.status,409)
        new=self.save(content='#main\n456',base=old['sha256'])
        self.assertNotEqual(new['sha256'],old['sha256'])
        self.assertEqual(self.db.execute('select sha256 from deck_versions').fetchone()[0],old['sha256'])
        self.save(content='#main\n456') # Retry is idempotent even after a lost response.
        self.assertEqual(self.db.execute('select count(*) from deck_versions').fetchone()[0],1)
    def test_unsafe_names_and_large_or_invalid_content(self):
        for name in ('../bad.ydk','/root.ydk','a/../bad.ydk','C:/bad.ydk','CON.ydk','a\\bad.ydk','a/.ydk/','bad.txt','a\x00.ydk','AUX/deck.ydk'):
            with self.subTest(name=name),self.assertRaises(service.Problem):self.save(name=name)
        for content in ('no deck','#main\n'+'1'*65536,'#main\x00'):
            with self.assertRaises(service.Problem):self.save(content=content)
        self.save(name='folder/日本語.ydk')
    def test_case_insensitive_names_and_revision_limit(self):
        sha=self.save(name='Deck.ydk')['sha256']
        for i in range(15):sha=self.save(name='deck.ydk',content='#main\n'+str(i),base=sha)['sha256']
        self.assertEqual(len(self.call()['decks']),1)
        self.assertEqual(self.db.execute('select count(*) from deck_versions').fetchone()[0],10)
    def test_count_quota(self):
        self.db.executemany('insert into deck_saves values(?,?,?,?,?,?)',((1,str(i),str(i)+'.ydk','#main','x',0) for i in range(1000)))
        with self.assertRaises(service.Problem) as e:self.save()
        self.assertEqual(e.exception.status,413)


if __name__=='__main__':unittest.main()
