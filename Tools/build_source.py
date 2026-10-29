"""Record the exact public source inputs without including local accounts or saves."""
import hashlib
import json
import re
import subprocess

SCOPES = ['Assets/Scripts/MDPro3', 'Assets/Scripts/Editor', 'Tools', 'Tests',
          'Assets/Resources/Ultra/MenuIcons', 'Data/PremadeDecks',
          'Data/premade-decks.json', 'Data/card-image-source.json',
          'Data/card-image-missing.json', 'Data/MenuIcons-LICENSES.txt',
          'README.md', 'FORMAT-BUILD.md', 'BRANDING-MIGRATION.md']

def release_version(root):
    text = (root/'Assets/Scripts/MDPro3/Net/UltraServices.cs').read_text(encoding='utf-8-sig')
    return re.search(r'public const string Version = "([0-9.]+)";', text).group(1)

def provenance(root):
    def git(*args):
        return subprocess.check_output(['git', *args], cwd=root)
    names = git('ls-files', '-c', '-o', '--exclude-standard', '--', *SCOPES).decode().splitlines()
    files = {n: hashlib.sha256((root/n).read_bytes()).hexdigest()
             for n in sorted(set(names)) if (root/n).is_file()}
    settings = (root/'ProjectSettings/ProjectSettings.asset').read_text(encoding='utf-8-sig')
    previous = git('show', 'HEAD:ProjectSettings/ProjectSettings.asset').decode('utf-8-sig')
    def identity(text):
        return re.findall(r'(?m)^  (productName|bundleVersion): (.*)$', text)
    files['applicationIdentity'] = identity(settings)
    dirty = bool(git('status', '--porcelain', '--', *SCOPES).strip()) or identity(settings) != identity(previous)
    return dict(sourceCommit=git('rev-parse', 'HEAD').decode().strip(), sourceDirty=dirty,
                sourceTreeSha256=hashlib.sha256(json.dumps(files, sort_keys=True).encode()).hexdigest())
