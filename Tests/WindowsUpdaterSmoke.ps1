$ErrorActionPreference='Stop'
$taskRoot=Join-Path ([IO.Path]::GetTempPath()) ('UltraUpdaterTest-'+[Guid]::NewGuid().ToString('N'))
$taskTools=Join-Path $taskRoot 'tools'
& (Join-Path (Split-Path $PSScriptRoot -Parent) 'Tools\Build-Ultra-Tools.ps1') -OutputDirectory $taskTools
if($LASTEXITCODE -ne 0){throw 'Tools compilation failed'}
Add-Type -AssemblyName System.IO.Compression.FileSystem
function Invoke-RestMethod {
 param($Uri,$Headers,$TimeoutSec)
 if($Uri -like '*/releases/latest') {return @{tag_name='v0.2.0';html_url='https://example.invalid';assets=@(@{name='update-manifest.json';browser_download_url='https://example.invalid/manifest'},@{name='Ultra-Duel-Update.zip';browser_download_url='https://example.invalid/zip';digest=('sha256:'+$global:taskFixtureManifest.sha256);size=(Get-Item $global:taskFixtureZip).Length})}}
 return $global:taskFixtureManifest
}
function Invoke-WebRequest {param($Uri,$Headers,$OutFile,[switch]$UseBasicParsing,$TimeoutSec) Microsoft.PowerShell.Management\Copy-Item -LiteralPath $global:taskFixtureZip -Destination $OutFile}
function Copy-Item {
 param($LiteralPath,$Destination,[switch]$Force)
 if($global:taskFailCopy -and $Destination.StartsWith($global:taskFailCopy+'.ultra-install-')) {$global:taskFailCopy=$null;throw 'Injected write failure'}
 Microsoft.PowerShell.Management\Copy-Item @PSBoundParameters
}
function Test-Upgrade([string]$case) {
 $taskCase=Join-Path $taskRoot $case;$taskGame=Join-Path $taskCase 'game';$taskFiles=Join-Path $taskCase 'files'
 foreach($dir in @('Data','Deck','Progress','Matteo-Duel_Data\Managed','Matteo-Duel_Data\StandaloneWindows64\MasterDuel')) {New-Item -ItemType Directory -Path (Join-Path $taskGame $dir) -Force|Out-Null}
 Set-Content (Join-Path $taskGame 'Matteo-Duel.exe') 'old player'
 Set-Content (Join-Path $taskGame 'MDPro3-TCG-Genesys.exe') 'old alias'
 Set-Content (Join-Path $taskGame 'Update-Matteo-Duel.cmd') 'obsolete updater'
 Set-Content (Join-Path $taskGame 'Matteo-Duel_Data\Managed\Assembly-CSharp.dll') 'old code'
 $outside=Join-Path $taskCase 'shared-original.dll'
 New-Item -ItemType HardLink -Path $outside -Target (Join-Path $taskGame 'Matteo-Duel_Data\Managed\Assembly-CSharp.dll')|Out-Null
 Set-Content (Join-Path $taskGame 'Matteo-Duel_Data\StandaloneWindows64\MasterDuel\field.bundle') 'unchanged field'
 $personal=@{'Data\config.conf'='personal settings';'Data\Settings.json'='custom graphics';'Data\friends-session.json'='private session';'Deck\Mine.ydk'='#matteo-format:Genesys:47';'Progress\profile.json'='earned progress'}
 foreach($entry in $personal.GetEnumerator()){Set-Content (Join-Path $taskGame $entry.Key) $entry.Value}
 $hashes=@{};foreach($name in $personal.Keys){$hashes[$name]=(Get-FileHash (Join-Path $taskGame $name)).Hash}
 foreach($name in @('Ultra-Duel.exe','UnityPlayer.dll','Ultra-Duel_Data\globalgamemanagers','Ultra-Duel_Data\Managed\Assembly-CSharp.dll','Ultra-Duel_Data\Plugins\x86_64\ocgcore.dll','Ultra-Duel_Data\Plugins\x86_64\ygoserver.dll')) {
  $path=Join-Path $taskFiles $name;New-Item -ItemType Directory -Path (Split-Path $path -Parent) -Force|Out-Null;Set-Content $path ('new '+$name+' UnityPlayer.dll')
 }
 $global:taskFixtureZip=Join-Path $taskCase 'fixture.zip';[IO.Compression.ZipFile]::CreateFromDirectory($taskFiles,$global:taskFixtureZip)
 $global:taskFixtureManifest=@{version='0.2.0';runtimeLayout='ultra-v1';minimumVersion='0.1.0';sourceCommit='fixture';asset='Ultra-Duel-Update.zip';sha256=(Get-FileHash $global:taskFixtureZip).Hash.ToLowerInvariant();files=@()}
 foreach($file in Get-ChildItem $taskFiles -File -Recurse){$global:taskFixtureManifest.files+=@{path=$file.FullName.Substring($taskFiles.Length+1).Replace('\','/');sha256=(Get-FileHash $file.FullName).Hash.ToLowerInvariant()}}
 if($case -eq 'rollback'){$global:taskFailCopy=Join-Path $taskGame 'Ultra-Duel_Data\Managed\Assembly-CSharp.dll'}
 if($case -eq 'unsafe-path'){$global:taskFixtureManifest.files+=@{path='../outside.txt';sha256='invalid'}}
 $failed=$false
 try {& (Join-Path $taskTools 'Update-Ultra-Duel.ps1') -GameDirectory $taskGame -BackupDirectory (Join-Path $taskCase 'personal-backup') -SkipShortcuts}
 catch {$failed=$true;if($case -eq 'success'){throw}}
 foreach($name in $personal.Keys){if((Get-FileHash (Join-Path $taskGame $name)).Hash -ne $hashes[$name]){throw ('Personal file changed: '+$name)}}
 if((Get-Content -LiteralPath $outside) -ne 'old code'){throw 'Updating a linked runtime modified a file outside the installation'}
 if($case -eq 'success') {
  if($failed -or !(Test-Path (Join-Path $taskGame 'Ultra-Duel.exe')) -or (Test-Path (Join-Path $taskGame 'Matteo-Duel.exe')) -or (Test-Path (Join-Path $taskGame 'Matteo-Duel_Data')) -or (Test-Path (Join-Path $taskGame 'Update-Matteo-Duel.cmd'))){throw 'Canonical upgrade or legacy cleanup failed'}
  if((Get-Content (Join-Path $taskGame 'Ultra-Duel_Data\StandaloneWindows64\MasterDuel\field.bundle')) -ne 'unchanged field'){throw 'Existing assets were lost'}
 } else {
  if(!$failed -or !(Test-Path (Join-Path $taskGame 'Matteo-Duel.exe')) -or (Test-Path (Join-Path $taskGame 'Ultra-Duel.exe')) -or (Get-Content (Join-Path $taskGame 'Matteo-Duel_Data\Managed\Assembly-CSharp.dll')) -ne 'old code'){throw ('Failed update did not preserve original installation: '+$case)}
 }
 Write-Host ('PASS updater '+$case+': legacy upgrade, save preservation and transactional safety')
}
Test-Upgrade 'success'
Test-Upgrade 'rollback'
Test-Upgrade 'unsafe-path'
