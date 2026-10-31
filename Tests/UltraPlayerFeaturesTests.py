"""Exercise the real API routes in an isolated database, including authorization."""
import json
from pathlib import Path
import sqlite3
import sys
import tempfile
import time
import unittest
import hashlib
from types import SimpleNamespace
from unittest.mock import patch

sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'Server'))
import service
import player_features


class PlayerFeatures(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory(); self.state=Path(self.tmp.name)
        self.db=sqlite3.connect(':memory:');self.db.row_factory=sqlite3.Row
        self.db.executescript('create table users(id integer primary key,name text,seen integer);insert into users values(1,"owner",1),(2,"friend",1),(3,"stranger",1);')
        player_features.initialize(self.db)
        self.state.joinpath('admins.json').write_text('["owner"]')
        self.rooms={'room1':{'hostId':1}}
    def tearDown(self):self.db.close();self.tmp.cleanup()
    def call(self,path,body=None,uid=1,method='POST'):
        u=self.db.execute('select * from users where id=?',(uid,)).fetchone()
        return player_features.route(None,self.db,method,path,body or {},int(time.time()),u,
            (self.state,service.Problem,lambda db,a,b:{a,b}=={1,2},self.rooms,service.LOCK))
    def test_admin_permission_and_ledger(self):
        for path,body,method in [('/admin/players',{},'GET'),('/admin/grant',{'id':2,'amount':500},'POST')]:
            with self.assertRaises(service.Problem) as e:self.call(path,body,2,method)
            self.assertEqual(e.exception.status,403)
        self.call('/admin/grant',{'id':2,'amount':500,'reason':'test'})
        self.assertEqual(player_features.profile(self.db,2)['gems'],500)
        self.assertEqual(self.db.execute('select count(*) from grants').fetchone()[0],1)
        for amount in (0,-1,100001):
            with self.assertRaises(service.Problem):self.call('/admin/grant',{'id':2,'amount':amount})
    def test_unknown_recipient(self):
        with self.assertRaises(service.Problem):self.call('/admin/grant',{'id':999,'amount':1})
    def test_unlock_all_is_owner_only_account_scoped_and_idempotent(self):
        for uid in (2,3):
            with self.assertRaises(service.Problem):self.call('/admin/unlock-all',{'id':2},uid)
        for _ in range(2):self.call('/admin/unlock-all',{'id':2})
        unlocked=player_features.profile(self.db,2)
        catalog=json.loads(Path(player_features.__file__).with_name('shop-catalog.json').read_text())
        self.assertTrue({i['key'] for i in catalog}<set(unlocked['owned']))
        self.assertEqual((unlocked['unlock_all'],unlocked['xp'],unlocked['gems']),(1,4000,0))
        self.assertEqual(player_features.profile(self.db,1)['unlock_all'],0)
        self.assertEqual(player_features.profile(self.db,3)['unlock_all'],0)
        with self.assertRaises(service.Problem):self.call('/admin/unlock-all',{'id':999})
    def test_only_primary_admin_gets_owner_tools(self):
        self.state.joinpath('admins.json').write_text('["owner","friend"]')
        self.assertTrue(player_features.is_admin(self.state,'friend'))
        self.assertFalse(player_features.is_owner(self.state,'friend'))
        with self.assertRaises(service.Problem):self.call('/admin/unlock-all',{'id':2},2)
    def test_duel_actions_require_owner_and_membership(self):
        self.db.execute('create table sessions(token text,user integer,expires integer)')
        for uid in (1,2):self.db.execute('insert into sessions values(?,?,?)',(hashlib.sha256(str(uid).encode()).hexdigest(),uid,int(time.time())+1000))
        room={'members':{2}}
        def call(uid,body):
            request=SimpleNamespace(headers={'Authorization':'Bearer '+str(uid)})
            return service.Handler.route(request,self.db,'POST','/admin/duel',body,int(time.time()))
        body=dict(room='duel',action='set-lp',target=0,value=12345)
        with patch.object(service,'STATE',self.state),patch.object(service,'ROOMS',{'duel':room}),patch.object(service,'send_duel_command',return_value=0) as command:
            with self.assertRaises(service.Problem) as denied:call(2,body)
            self.assertEqual(denied.exception.status,403)
            with self.assertRaises(service.Problem):call(1,body)
            command.assert_not_called()
            room['members'].add(1)
            self.assertTrue(call(1,body)['ok'])
            command.assert_called_once_with(room,'owner',2,0,12345)
            self.assertEqual(self.db.execute('select actor,action from admin_actions').fetchone()[:],(1,'set-lp'))
            other={'members':{2}}
            room['tables']={1:room,2:other}
            body['table']=2
            with self.assertRaises(service.Problem):call(1,body)
            other['members'].add(1)
            self.assertTrue(call(1,body)['ok'])
            command.assert_called_with(other,'owner',2,0,12345)
    def test_opponent_duel_actions_validate_target_values_and_owner(self):
        self.db.execute('create table sessions(token text,user integer,expires integer)')
        for uid in (1,2):self.db.execute('insert into sessions values(?,?,?)',(hashlib.sha256(str(uid).encode()).hexdigest(),uid,int(time.time())+1000))
        room={'members':{1,2}}
        def call(uid,body):
            request=SimpleNamespace(headers={'Authorization':'Bearer '+str(uid)})
            return service.Handler.route(request,self.db,'POST','/admin/duel',body,int(time.time()))
        with patch.object(service,'STATE',self.state),patch.object(service,'ROOMS',{'duel':room}),patch.object(service,'send_duel_command',return_value=0) as command:
            for action,op,value in [('clear-field',5,0),('clear-hand-field',6,0),('defense',7,-1),('defense',7,6),('zero-atk',8,4),('skip-turn',9,0)]:
                body=dict(room='duel',action=action,target=0,value=value)
                with self.assertRaises(service.Problem) as denied:call(2,body)
                self.assertEqual(denied.exception.status,403)
                self.assertTrue(call(1,body)['ok'])
                command.assert_called_with(room,'owner',op,1,value)
            for target in (0,1):
                body=dict(room='duel',action='set-atk',target=target,value=4500,zone=4)
                with self.assertRaises(service.Problem):call(2,body)
                self.assertTrue(call(1,body)['ok'])
                command.assert_called_with(room,'owner',10,target,4500*8+4)
            for value,zone in [(-1,0),(100000000,0),(1000,-1),(1000,7)]:
                command.reset_mock()
                with self.assertRaises(service.Problem):call(1,dict(room='duel',action='set-atk',value=value,zone=zone))
                command.assert_not_called()
            for action,value in [('defense',-2),('defense',7),('zero-atk',-1),('zero-atk',7),('unknown',0)]:
                command.reset_mock()
                with self.assertRaises(service.Problem):call(1,dict(room='duel',action=action,value=value))
                command.assert_not_called()

    def test_spectating_uses_friend_authorization_and_marks_observer_role(self):
        self.db.executescript('create table sessions(token text,user integer,expires integer);create table friends(sender integer,recipient integer,accepted integer);insert into friends values(1,2,1);')
        for uid in (1,2,3):self.db.execute('insert into sessions values(?,?,?)',(hashlib.sha256(str(uid).encode()).hexdigest(),uid,int(time.time())+1000))
        room=dict(id='r',host='owner',hostId=1,format='TCG',cap=100,port=7911,expires=int(time.time())+1000,mode='1v1',capacity=2,secret='private',members={1},process=SimpleNamespace(poll=lambda:None))
        def call(uid,path,body=None):
            request=SimpleNamespace(headers={'Authorization':'Bearer '+str(uid)})
            return service.Handler.route(request,self.db,'POST',path,body or {},int(time.time()))
        with patch.object(service,'ROOMS',{'r':room}),patch.object(service,'clean_rooms'):
            with self.assertRaises(service.Problem) as denied:call(3,'/rooms/r/watch')
            self.assertEqual(denied.exception.status,403)
            watch=call(2,'/rooms/r/watch')
            self.assertTrue(watch['spectate'])
            self.assertEqual(watch['secret'],'private')
            self.assertFalse(call(2,'/rooms/r/join')['spectate'])
            table2=dict(port=7912,secret='second',members=set(),process=SimpleNamespace(poll=lambda:None))
            with patch.object(service,'start_table',return_value=table2) as start:
                joined=call(2,'/rooms/r/join',{'table':2})
                self.assertEqual((joined['port'],joined['table'],joined['secret']),(7912,2,'second'))
                self.assertNotEqual(room['secret'],table2['secret'])
                start.assert_called_once_with(room,2)
                self.assertTrue(call(2,'/rooms/r/watch',{'table':2})['spectate'])
                start.assert_called_once()
            with self.assertRaises(service.Problem):call(2,'/rooms/r/join',{'table':4})

    def test_profile_avatar_is_account_scoped(self):
        p=self.call('/profile/avatar',{'card':16509007})
        self.assertEqual(p['avatar'],16509007)
        self.assertEqual(player_features.profile(self.db,2)['avatar'],0)
        self.assertEqual(p['gems'],0)
        for value in (-1,1000000000):
            with self.assertRaises(service.Problem):self.call('/profile/avatar',{'card':value})
    def test_shop_ownership_and_duplicate_purchase(self):
        item=json.loads(Path(player_features.__file__).with_name('shop-catalog.json').read_text())[0]
        with self.assertRaises(service.Problem):self.call('/shop/buy',{'item':item['key']},2)
        self.call('/admin/grant',{'id':2,'amount':1000})
        for _ in range(2):self.call('/shop/buy',{'item':item['key']},2)
        p=player_features.profile(self.db,2)
        self.assertEqual(p['gems'],1000-item['price']);self.assertEqual(p['owned'],[item['key']])
        with self.assertRaises(service.Problem):self.call('/shop/buy',{'item':'made-up'},2)
    def test_game_and_reward_idempotency(self):
        for n in range(2):
            nonce=f'{n:032x}';self.call('/games/start',{'nonce':nonce,'deck':'Blue-Eyes','opponent':'friend'})
            for _ in range(2):self.call('/games/finish',{'nonce':nonce,'result':'WIN','turns':5})
        p=player_features.profile(self.db,1)
        self.assertEqual((p['wins'],p['xp'],p['favoriteDeck']),(2,200,'Blue-Eyes'))
        for _ in range(2):self.call('/pass/claim')
        self.assertEqual(player_features.profile(self.db,1)['gems'],100)
        self.call('/games/finish',{'nonce':'unstarted','result':'WIN'})
        self.assertEqual(player_features.profile(self.db,1)['xp'],200)
    def test_missions_locked_idempotent_and_daily_reset(self):
        self.call('/missions/claim')
        self.assertEqual(player_features.profile(self.db,1)['gems'],0)
        nonce='f'*32
        self.call('/games/start',{'nonce':nonce,'deck':'Blue-Eyes'})
        self.call('/games/finish',{'nonce':nonce,'result':'WIN','turns':4})
        for _ in range(2):self.call('/missions/claim')
        self.assertEqual(player_features.profile(self.db,1)['gems'],75)
        rows=player_features.mission_rows(self.db,1)
        self.assertTrue(all(r['claimed'] for r in rows if r['category']=='Daily'))
        tomorrow=int(time.time())+86400
        rows=player_features.mission_rows(self.db,1,tomorrow)
        self.assertTrue(all(not r['claimed'] and r['progress']==0 for r in rows if r['category']=='Daily'))
        self.assertEqual(self.db.execute('select count(*) from mission_claims').fetchone()[0],2)
    def test_mission_claims_stay_with_their_account(self):
        self.call('/games/start',{'nonce':'e'*32})
        self.call('/games/finish',{'nonce':'e'*32,'result':'WIN'})
        self.call('/missions/claim',uid=2)
        self.assertEqual(player_features.profile(self.db,2)['gems'],0)
    def test_friend_invitation_authorization(self):
        self.call('/invites',{'id':2,'room':'room1'})
        for uid,target in ((1,3),(2,1)):
            with self.assertRaises(service.Problem):self.call('/invites',{'id':target,'room':'room1'},uid)
        self.assertEqual(self.db.execute('select count(*) from invites').fetchone()[0],1)
    def test_presence_and_no_admin_by_default(self):
        self.call('/presence',{'activity':'In duel'},2)
        self.assertEqual(player_features.profile(self.db,2)['activity'],'In duel')
        self.state.joinpath('admins.json').unlink()
        self.assertFalse(player_features.is_admin(self.state,'owner'))
        with self.assertRaises(service.Problem):self.call('/presence',{'activity':'administrator'},2)


if __name__=='__main__':unittest.main(verbosity=2)
