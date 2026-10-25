"""Account inbox integration through the real authenticated service routes."""
import hashlib
import sqlite3
import sys
import time
import unittest
from pathlib import Path
from types import SimpleNamespace
from unittest.mock import patch

sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'Server'))
import service
import player_features


class Notifications(unittest.TestCase):
    def setUp(self):
        self.db=sqlite3.connect(':memory:');self.db.row_factory=sqlite3.Row
        self.db.executescript('''create table users(id integer primary key,name text,seen integer);
        create table sessions(token text,user integer,expires integer);
        create table friends(sender integer,recipient integer,accepted integer);
        insert into users values(1,'Alice',0),(2,'Bob',0),(3,'Other',0);''')
        player_features.initialize(self.db)
        self.now=int(time.time())
        for uid in (1,2,3):self.db.execute('insert into sessions values(?,?,?)',(hashlib.sha256(str(uid).encode()).hexdigest(),uid,self.now+1000))
    def tearDown(self):self.db.close()
    def call(self,uid,path,body=None):
        return service.Handler.route(SimpleNamespace(headers={'Authorization':'Bearer '+str(uid)}),self.db,'GET' if body is None else 'POST',path,body or {},self.now)
    def inbox(self,uid):return self.call(uid,'/notifications')['notifications']
    def test_friend_request_and_acceptance_reach_both_inboxes(self):
        self.call(1,'/friends/request',{'name':'Bob'})
        self.assertEqual(self.inbox(1)[0]['kind'],'request-sent')
        received=self.inbox(2)[0]
        self.assertEqual((received['kind'],received['name'],received['peer'],received['seen']),('friend-request','Alice',1,0))
        self.call(2,'/friends/accept',{'id':1})
        self.assertEqual(self.inbox(1)[0]['kind'],'friend-accepted')
        with self.assertRaises(service.Problem):self.call(2,'/friends/accept',{'id':1})
        self.assertEqual(len(self.inbox(1)),2)
        self.assertEqual(self.inbox(3),[])
    def test_invite_confirmation_duplicate_suppression_and_expiry(self):
        self.db.execute('insert into friends values(1,2,1)')
        with patch.object(service,'ROOMS',{'r':{'hostId':1}}):
            for _ in range(2):self.call(1,'/invites',{'id':2,'room':'r'})
            self.assertEqual(len(self.inbox(1)),1)
            invite=self.inbox(2)[0]
            self.assertEqual((invite['kind'],invite['room'],invite['expires']),('duel-invite','r',self.now+300))
            self.assertEqual(self.inbox(1)[0]['kind'],'invite-sent')
            with self.assertRaises(service.Problem):self.call(1,'/invites',{'id':3,'room':'r'})
            self.now+=25
            self.call(1,'/invites',{'id':2,'room':'r'})
            self.assertEqual(len(self.inbox(2)),1)
            self.assertEqual(self.inbox(2)[0]['expires'],self.now+300)
            self.now+=301
            self.call(1,'/invites',{'id':2,'room':'r'})
            self.assertEqual(len(self.inbox(2)),2)
    def test_mark_read_is_account_scoped_and_does_not_clear_newer_events(self):
        self.call(1,'/friends/request',{'name':'Bob'})
        first=self.inbox(2)[0]['id']
        self.call(3,'/notifications/read',{'through':first,'user':2})
        self.assertEqual(self.inbox(2)[0]['seen'],0)
        player_features.notifications.add(self.db,2,'friend-accepted',3,'Other',self.now)
        result=self.call(2,'/notifications/read',{'through':first})['notifications']
        self.assertEqual([r['seen'] for r in result],[0,1])
        for value in (-1,0,True,'all',2**64):
            with self.assertRaises(service.Problem):self.call(2,'/notifications/read',{'through':value})
        with self.assertRaises(service.Problem) as error:self.call(999,'/notifications')
        self.assertEqual(error.exception.status,401)
    def test_home_state_contains_inbox_and_history_is_retained_after_read(self):
        self.call(1,'/friends/request',{'name':'Bob'})
        with patch.object(service,'ROOMS',{}),patch.object(service,'clean_rooms'):
            state=self.call(2,'/state')
        self.assertEqual(state['notifications'],self.inbox(2))
        self.call(2,'/notifications/read',{'through':state['notifications'][0]['id']})
        self.assertEqual(len(self.inbox(2)),1)
        self.assertEqual(self.inbox(2)[0]['seen'],1)
    def test_retention_is_bounded_per_account(self):
        for _ in range(110):player_features.notifications.add(self.db,1,'invite-sent',2,'Bob',self.now)
        self.assertEqual(len(self.inbox(1)),100)
        self.assertEqual(len(self.inbox(2)),0)


if __name__=='__main__':unittest.main()
