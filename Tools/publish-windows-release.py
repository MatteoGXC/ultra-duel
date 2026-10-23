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
OUTPUT = ROOT.parent / 'release-work/windows-v0.1.0'
REPO = '/repos/MatteoGXC/matteo-duel'

def main():
    credentials = subprocess.run(['git', 'credential', 'fill'], input='protocol=https\nhost=github.com\n\n',
                                 text=True, capture_output=True, check=True)
    token = dict(line.split('=', 1) for line in credentials.stdout.splitlines() if '=' in line)['password']
    headers = {'Authorization': 'Bearer '+token, 'User-Agent': 'Matteo-Duel release publisher',
               'Accept': 'application/vnd.github+json', 'Content-Type': 'application/json'}
    def api(path, method='GET', body=None):
        request = urllib.request.Request('https://api.github.com'+path, method=method, headers=headers,
            data=None if body is None else json.dumps(body).encode())
        with urllib.request.urlopen(request, timeout=120) as response:
            raw = response.read()
            return json.loads(raw) if raw else None
    manifest = json.loads((OUTPUT/'windows-manifest.json').read_text())
    body = ('## Download and play\n\nDownload **Matteo-Duel-Setup.exe** below, run it, and choose an installation folder. '
            'Setup downloads and verifies the complete game and creates a desktop shortcut. No Unity installation is needed.\n\n'
            'For a portable install, download **all** `Matteo-Duel-Windows-*.zip` files below and extract them into the same empty folder. '
            'Run `Matteo-Duel.exe` and keep its runtime/asset folders together. The automatic Source code ZIP is for developers.\n\n'
            '## Included\n\nWindows x64 player, native rules engine, Microsoft runtime DLLs, card data/scripts, artwork, audio/assets, '
            'three starter decks, 14 TCG and 15 Genesys published Solo lists, mixed difficulty, per-deck format badges, and source links. '
            'Personal decks, accounts, settings, and replays are excluded. Setup preserves existing decks and settings when updating.\n\n'
            '## Multiplayer status\n\nOffline Solo is supported. Remote hosting is **not ready**: the embedded room server currently listens only on localhost. '
            'LAN/internet hosting needs further networking work; no public lobby service is included.\n\n'
            'Built game source: `'+manifest['buildCommit']+'`. License and upstream credits are included in the repository and package.\n')
    try: release = api(REPO+'/releases/tags/v0.1.0')
    except urllib.error.HTTPError as error:
        if error.code != 404: raise
        release = api(REPO+'/releases', 'POST', dict(tag_name='v0.1.0', target_commitish='main',
            name='Matteo Duel 0.1.0 — Windows', body=body, draft=True, prerelease=False))
    if not release['draft']: raise RuntimeError('Release is already public; refusing to replace its assets.')
    endpoint = REPO+'/releases/'+str(release['id'])
    api(endpoint, 'PATCH', {'body': body})
    existing = {asset['name']:asset for asset in api(endpoint+'/assets?per_page=100')}
    upload_base = release['upload_url'].split('{')[0]
    files = [OUTPUT/p['name'] for p in manifest['parts']] + [OUTPUT/'windows-manifest.json', OUTPUT/'Matteo-Duel-Setup.exe']
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
