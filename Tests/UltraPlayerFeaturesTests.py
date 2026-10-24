"""Exercise the real API routes in an isolated database, including authorization."""
import json
from pathlib import Path
import sqlite3
import sys
import tempfile
import time
import unittest

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
