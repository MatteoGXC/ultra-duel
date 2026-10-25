"""Exercise queue isolation, pairing, confirmation, expiry and cancellation."""
import concurrent.futures
from pathlib import Path
import sys
import unittest
from unittest.mock import patch
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'Server'))
import service

class Process:
    def poll(self):return None

class QueueTests(unittest.TestCase):
    def setUp(self):
        service.QUEUE.clear();service.MATCHES.clear();service.ROOMS.clear()
        self.native=patch.object(service,'start_table',side_effect=lambda room,n:dict(port=7911,secret='private-grant',process=Process(),members=set()))
        self.native.start()
    def tearDown(self):self.native.stop();service.ROOMS.clear()
    def request(self,uid,now=100,format='TCG',cap=100,accept=True,method='POST'):
        return service.matchmaking(dict(id=uid,name='Player'+str(uid)),dict(format=format,cap=cap,accept=accept),method,now)
    def test_confirmed_pair_and_private_connection_grant(self):
        self.assertEqual(self.request(1)['status'],'waiting')
        self.assertEqual(self.request(1)['status'],'waiting')
        self.assertEqual(self.request(2)['status'],'confirming')
        self.assertEqual(self.request(1)['status'],'confirming')
        second=self.request(2);first=self.request(1)
        self.assertEqual(first,second);self.assertEqual(first['status'],'ready')
        room=next(iter(service.ROOMS.values()))
        self.assertEqual(room['members'],{1,2});self.assertTrue(room['randomMatch'])
        self.assertNotIn('secret',service.public_room(room))
        self.assertEqual(len(service.ROOMS),1)
        self.request(1,method='DELETE');self.assertNotIn(1,service.MATCHES)
        self.assertEqual(self.request(2),second)
    def test_format_and_point_cap_isolation(self):
        for uid,format,cap in [(1,'TCG',100),(2,'Genesys',100),(3,'Genesys',200)]:
            self.assertEqual(self.request(uid,format=format,cap=cap)['status'],'waiting')
        self.assertEqual(self.request(4,format='Genesys',cap=200)['status'],'confirming')
        self.assertEqual(service.MATCHES[4]['players'],{3,4})
    def test_cancel_waiting_and_confirming(self):
        self.request(1);self.request(1,method='DELETE')
        self.assertEqual(self.request(2)['status'],'waiting')
        self.request(3);self.request(2,method='DELETE')
        with self.assertRaisesRegex(service.Problem,'left the search'):self.request(3)
        self.assertFalse(service.ROOMS)
    def test_stale_clients_and_confirmations_expire(self):
        self.request(1);self.assertEqual(self.request(2,now=121)['status'],'waiting')
        self.assertNotIn(1,service.QUEUE)
        self.request(3,now=121)
        self.assertEqual(self.request(4,now=167)['status'],'waiting')
        self.assertNotIn(2,service.MATCHES)
    def test_concurrent_requests_never_pair_a_player_twice(self):
        with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:list(pool.map(self.request,range(1,9)))
        pairs={tuple(sorted(m['players'])) for m in service.MATCHES.values()}
        self.assertEqual(len(pairs),4);self.assertEqual(sorted(x for pair in pairs for x in pair),list(range(1,9)))
    def test_invalid_format(self):
        with self.assertRaises(service.Problem):self.request(1,format='bad')

if __name__=='__main__':unittest.main()
