"""Upload versioned Windows assets as a draft; publish only after verification."""
import concurrent.futures
import hashlib
import http.client
import json
from pathlib import Path
import subprocess
import time
import urllib.error
import urllib.parse
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT.parent / 'release-work/windows-v0.1.14'
REPO = '/repos/MatteoGXC/ultra-duel'

def main():
    credentials = subprocess.run(['git', 'credential', 'fill'], input='protocol=https\nhost=github.com\n\n',
                                 text=True, capture_output=True, check=True)
    token = dict(line.split('=', 1) for line in credentials.stdout.splitlines() if '=' in line)['password']
    headers = {'Authorization': 'Bearer '+token, 'User-Agent': 'Ultra-Duel release publisher',
               'Accept': 'application/vnd.github+json', 'Content-Type': 'application/json'}
    def api(path, method='GET', body=None):
        request = urllib.request.Request('https://api.github.com'+path, method=method, headers=headers,
            data=None if body is None else json.dumps(body).encode())
        with urllib.request.urlopen(request, timeout=120) as response:
            raw = response.read()
            return json.loads(raw) if raw else None
    manifest = json.loads((OUTPUT/'windows-manifest.json').read_text())
    version = manifest['version']
    tag = 'v'+version
    body = ('## Ultra Duel '+version+' - Notifications and Results Fix\n\n'
            'New players: download **Ultra-Duel-Setup.exe**. Existing players: use **Ultra-Duel-Updater.zip** beside the game executable.\n\n'
            '- Home-screen Notices button, unread badge and recent-activity card.\n'
            '- Persistent account history for incoming and sent duel invitations, friend requests and accepted requests.\n'
            '- Join an invited duel, accept a friend request and mark notifications read from the inbox.\n'
            '- Repeated invitation clicks do not flood the inbox; read state stays private to your account.\n'
            '- Corrects Heavy Borger effect labels: DARK draws one card, EARTH & FIRE deals 1500 damage. The combined-effect heading is no longer a selectable effect.\n'
            '- Fixes the blank post-duel results screen by using the loaded game font. Scores, rewards, level and Continue render even when the default font is missing.\n'
            '- Retains private VPS deck backups, local save protection, crystal gems, GUI Scale and friend-room features.\n\n'
            'Intel Mac downloads are published in a separate macOS release.\n\n'
            'Built game source: `'+manifest['buildCommit']+'`. Upstream credits and licenses remain included.\n')
    try: reference = api(REPO+'/git/ref/tags/'+tag)
    except urllib.error.HTTPError as error:
        if error.code != 404: raise
        reference = api(REPO+'/git/refs', 'POST', {'ref':'refs/tags/'+tag, 'sha':manifest['buildCommit']})
    if reference['object']['sha'] != manifest['buildCommit']:
        raise RuntimeError('Release tag points at another commit.')
    try: release = api(REPO+'/releases/tags/'+tag)
    except urllib.error.HTTPError as error:
        if error.code != 404: raise
        release = api(REPO+'/releases', 'POST', dict(tag_name=tag, target_commitish=manifest['buildCommit'],
            name=version+' [Windows]', body=body, draft=True, prerelease=False))
    if not release['draft']: raise RuntimeError('Release is already public; refusing to replace its assets.')
    endpoint = REPO+'/releases/'+str(release['id'])
    api(endpoint, 'PATCH', {'body': body})
    existing = {asset['name']:asset for asset in api(endpoint+'/assets?per_page=100')}
    upload_base = release['upload_url'].split('{')[0]
    files = [OUTPUT/name for name in ('windows-manifest.json', 'Ultra-Duel-Setup.exe',
        'update-manifest.json', 'Ultra-Duel-Update.zip', 'Ultra-Duel-Updater.zip')]
    def upload(path):
        size = path.stat().st_size
        digest = hashlib.sha256()
        with path.open('rb') as stream:
            for block in iter(lambda:stream.read(4*1024*1024), b''): digest.update(block)
        expected = 'sha256:'+digest.hexdigest()
        prior = existing.get(path.name)
        if prior and prior['state']=='uploaded' and prior['size']==size and prior.get('digest')==expected:
            print('ALREADY_VERIFIED',path.name,flush=True); return prior
        if prior:
            if prior['state']=='uploaded': raise RuntimeError('Existing uploaded asset differs: '+path.name)
            api(REPO+'/releases/assets/'+str(prior['id']), 'DELETE')
        for attempt in range(3):
            try:
                parsed = urllib.parse.urlparse(upload_base+'?'+urllib.parse.urlencode({'name':path.name}))
                connection = http.client.HTTPSConnection(parsed.hostname, timeout=1800)
                connection.putrequest('POST', parsed.path+'?'+parsed.query)
                for key,value in headers.items():
                    if key!='Content-Type': connection.putheader(key,value)
                connection.putheader('Content-Type','application/octet-stream')
                connection.putheader('Content-Length',str(size)); connection.endheaders()
                sent = 0; checkpoint = 0
                with path.open('rb') as stream:
                    for block in iter(lambda:stream.read(4*1024*1024), b''):
                        connection.send(block); sent += len(block)
                        if sent-checkpoint >= 128*1024*1024:
                            print('UPLOADING',path.name,round(sent/size*100),'%',flush=True); checkpoint=sent
                response = connection.getresponse(); raw = response.read(); connection.close()
                if response.status not in (200,201): raise RuntimeError('Upload HTTP '+str(response.status)+': '+raw[:250].decode(errors='replace'))
                result = json.loads(raw)
                if result['size']!=size or result.get('digest')!=expected: raise RuntimeError('GitHub asset digest mismatch: '+path.name)
                print('VERIFIED_UPLOAD',path.name,size,flush=True); return result
            except Exception:
                # A disconnected response can still leave a completed asset on GitHub.
                current = next((a for a in api(endpoint+'/assets?per_page=100') if a['name']==path.name),None)
                if current and current['state']=='uploaded' and current.get('digest')==expected:
                    print('VERIFIED_UPLOAD',path.name,size,flush=True); return current
                if current and current['state']!='uploaded': api(REPO+'/releases/assets/'+str(current['id']),'DELETE')
                if attempt==2: raise
                time.sleep(2)
    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
        assets = list(pool.map(upload, files))
    (OUTPUT/'uploaded-release.json').write_text(json.dumps(dict(id=release['id'],url=release['html_url'],
        assets=[dict(name=a['name'],url=a['browser_download_url'],digest=a['digest'],size=a['size']) for a in assets]),indent=2)+'\n')
    print('DRAFT_UPLOAD_COMPLETE',release['html_url'],flush=True)

if __name__ == '__main__': main()
