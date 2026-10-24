"""Exercise actual HTTPS owner permissions and the production room command pipe."""
import json,os,secrets,socket,ssl,subprocess,sys,tempfile,time,urllib.error,urllib.request
from pathlib import Path

def run():
    with tempfile.TemporaryDirectory(prefix='ultra-owner-service-') as directory:
        state=Path(directory)
        with socket.socket() as probe:probe.bind(('127.0.0.1',0));port=probe.getsockname()[1]
        env=dict(os.environ,ULTRA_STATE=directory,ULTRA_HOST='127.0.0.1',ULTRA_PORT=str(port),ULTRA_ROOM_FIRST='17911')
        process=subprocess.Popen([sys.executable,'-u','/app/service.py'],env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True)
        try:
            for _ in range(200):
                if (state/'certificate.pem').exists():
                    try:
                        with socket.create_connection(('127.0.0.1',port),timeout=.1):break
                    except OSError:pass
                if process.poll() is not None:raise AssertionError(process.stdout.read())
                time.sleep(.05)
            context=ssl.create_default_context(cafile=str(state/'certificate.pem'))
            def api(path,method='GET',body=None,token='',expected=200):
                headers={'Content-Type':'application/json'}
                if token:headers['Authorization']='Bearer '+token
                request=urllib.request.Request('https://127.0.0.1:'+str(port)+path,headers=headers,method=method,data=None if body is None else json.dumps(body).encode())
                try:
                    with urllib.request.urlopen(request,context=context,timeout=12) as response:status=response.status;result=json.load(response)
                except urllib.error.HTTPError as error:status=error.code;result=json.load(error)
                assert status==expected,(path,status,result)
                return result
            password=secrets.token_urlsafe(18)
            owner=api('/register','POST',dict(name='owner',password=password))
            friend=api('/register','POST',dict(name='friend',password=password))
            state.joinpath('admins.json').write_text('["owner"]')
            assert api('/state',token=owner['token'])['owner'] is True
            assert api('/state',token=friend['token'])['owner'] is False
            api('/admin/unlock-all','POST',{'id':owner['id']},friend['token'],403)
            api('/admin/unlock-all','POST',{'id':friend['id']},owner['token'])
            assert api('/profile',token=friend['token'])['unlock_all']==1
            assert api('/profile',token=owner['token'])['unlock_all']==0
            room=api('/rooms','POST',{'format':'TCG','mode':'1v1'},owner['token'])
            action=dict(room=room['id'],action='set-lp',value=12345)
            api('/admin/duel','POST',action,friend['token'],403)
            response=api('/admin/duel','POST',action,owner['token'],409)
            assert response['error']=='No active duel.',response
            api('/rooms/'+room['id'],'DELETE',token=owner['token'])
            print('PASS HTTPS owner-only controls, account-scoped unlock, room membership and actual native command pipe; no automatic gameplay changes')
        finally:
            process.terminate()
            output=process.communicate(timeout=5)[0]
            if output.strip():print(output[-1000:])

if __name__=='__main__':run()
