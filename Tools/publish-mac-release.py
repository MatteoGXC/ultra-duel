"""Publish only a DMG that passed the installed Apple Silicon game checks."""
import argparse
import hashlib
import http.client
import json
from pathlib import Path
import subprocess
import urllib.parse
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT.parent/'release-work/mac-verified-017'
API = 'https://api.github.com/repos/MatteoGXC/ultra-duel'


def main():
    parser=argparse.ArgumentParser();parser.add_argument('--publish',action='store_true');args=parser.parse_args()
    verification=json.loads((OUTPUT/'verification.json').read_text())
    assert verification['conclusion']=='success', 'Installed game verification did not pass'
    evidence=OUTPUT/'mac-installation-evidence'
    assert list(evidence.rglob('platform-passed.txt')), 'Native player evidence missing'
    for folder in ('mac-ui','mac-duel-ui'):
        assert any(p.parent.name==folder for p in evidence.rglob('passed.txt')), folder+' did not pass'
    audit=json.loads(next(evidence.rglob('mac-card-effects.json')).read_text())
    assert audit['checked']>=14000 and not audit['errors']
    dmg=next((OUTPUT/'verified-ultra-duel-apple-silicon-dmg').rglob('Ultra-Duel-Apple-Silicon.dmg'))
    sha=hashlib.file_digest(dmg.open('rb'),'sha256').hexdigest()
    state=json.loads((ROOT.parent/'release-work/mac-verification/private-input.json').read_text())
    runtime=json.loads((ROOT.parent/'release-work/mac-verification/versioned-runtime-017.json').read_text())
    result=subprocess.run(['git','credential','fill'],input='protocol=https\nhost=github.com\n\n',text=True,capture_output=True,check=True)
    token=dict(line.split('=',1) for line in result.stdout.splitlines() if '=' in line)['password']
    headers={'Authorization':'Bearer '+token,'User-Agent':'Ultra-Duel Mac publisher','Accept':'application/vnd.github+json','Content-Type':'application/json'}
    def api(path,method='GET',body=None):
        with urllib.request.urlopen(urllib.request.Request(API+path,headers=headers,method=method,data=None if body is None else json.dumps(body).encode()),timeout=120) as response:
            data=response.read();return json.loads(data) if data else None
    endpoint='/releases/'+str(state['release_id'])
    release=api(endpoint)
    assert release['draft'], 'Refusing to replace a public release'
    assets={a['name']:a for a in api(endpoint+'/assets?per_page=100')}
    for part in runtime['parts']:
        actual=assets[part['name']]
        assert actual['size']==part['size'] and actual.get('digest')=='sha256:'+part['sha256'], part['name']
    readme=OUTPUT/'READ-ME-MAC.txt'
    readme.write_text('Ultra Duel 0.1.7 for Apple Silicon Macs\n\n1. Open the DMG and drag Ultra Duel into Applications.\n2. Open Ultra Duel from Applications. This independent build is ad-hoc signed, not Apple notarized. If macOS blocks it, open System Settings > Privacy & Security and choose Open Anyway for Ultra Duel.\n3. Let the first-time setup download the artwork and fields (3.7 GiB). Downloads can be paused and resumed. Allow 10 GB of free space during setup.\n\nYour decks, settings and account data are saved in ~/Library/Application Support/Ultra Duel.\n\nVerified on an Apple Silicon macOS runner: DMG mount and application copy, signature integrity, first installation, installed native player, 14,625 card initializers, menu/deck rendering, and a rendered Solo duel. Three-player tag and four-player tag were tested separately through the native Mac engine.\n\nThree- and four-player free-for-all modes are not implemented. Live friend-duel server updates are separate from this installer.\n',encoding='utf-8')
    checksums=OUTPUT/'SHA256SUMS.txt';checksums.write_text(sha+'  '+dmg.name+'\n',encoding='ascii')
    print('READY',dmg,dmg.stat().st_size,sha,flush=True)
    if not args.publish:return
    def upload(path):
        digest='sha256:'+hashlib.file_digest(path.open('rb'),'sha256').hexdigest()
        prior=assets.get(path.name)
        if prior:
            assert prior.get('digest')==digest, 'Draft asset differs: '+path.name
            return prior
        url=urllib.parse.urlparse(release['upload_url'].split('{')[0]+'?'+urllib.parse.urlencode({'name':path.name}))
        connection=http.client.HTTPSConnection(url.hostname,timeout=1800)
        connection.putrequest('POST',url.path+'?'+url.query)
        for key,value in headers.items():
            if key!='Content-Type':connection.putheader(key,value)
        connection.putheader('Content-Type','application/octet-stream');connection.putheader('Content-Length',str(path.stat().st_size));connection.endheaders()
        with path.open('rb') as stream:
            for chunk in iter(lambda:stream.read(4*1024*1024),b''):connection.send(chunk)
        response=connection.getresponse();body=response.read();connection.close()
        assert response.status==201,(response.status,body[:250])
        asset=json.loads(body);assert asset['digest']==digest
        print('UPLOADED_AND_VERIFIED',path.name,flush=True);return asset
    for path in (dmg,readme,checksums):upload(path)
    # Build inputs are retained locally; never expose them as player downloads.
    for name,asset in assets.items():
        if name.startswith('mac-input') and name.endswith('.zip'):api('/releases/assets/'+str(asset['id']),'DELETE')
    tag='macos-v'+runtime['version']
    body=('Apple Silicon download: **Ultra-Duel-Apple-Silicon.dmg**. Open it and drag Ultra Duel into Applications.\n\n'
          'The first launch installs 3.7 GiB of artwork and fields; allow 10 GB of free space. Downloads resume after interruptions. The content `.bin` files are downloaded automatically; do not open them manually.\n\n'
          'This independent build is ad-hoc signed, not Apple notarized. If macOS blocks it, use **System Settings → Privacy & Security → Open Anyway**.\n\n'
          'Passed an actual DMG install, installed ARM player checks, every card initializer, menu/deck rendering, and a graphical Solo duel. Native three-player tag and four-player tag tests also passed. Free-for-all modes remain unimplemented. Live friend-server deployment is separate.\n\n'
          '[Installation verification]('+verification['html_url']+').\n')
    release=api(endpoint,'PATCH',dict(tag_name=tag,target_commitish=verification['head_sha'],name='Ultra Duel '+runtime['version']+' — Apple Silicon Mac',body=body,draft=False,prerelease=False,make_latest='false'))
    # GitHub can preserve its temporary draft tag during publication; enforce the final tag.
    if release['tag_name']!=tag:release=api(endpoint,'PATCH',dict(tag_name=tag,target_commitish=verification['head_sha']))
    assert release['tag_name']==tag and not release['draft']
    (OUTPUT/'published-release.json').write_text(json.dumps(release,indent=2))
    print('MAC_RELEASE_PUBLISHED',release['html_url'],flush=True)


if __name__=='__main__':main()
