$ErrorActionPreference='Stop'
$taskRoot=Join-Path ([IO.Path]::GetTempPath()) ('MatteoUpdaterTest-'+[Guid]::NewGuid().ToString('N'))
$taskGame=Join-Path $taskRoot 'game'
$taskFiles=Join-Path $taskRoot 'files'
foreach ($taskDir in @($taskGame,$taskFiles,(Join-Path $taskGame 'Deck'),(Join-Path $taskGame 'Data'),(Join-Path $taskFiles 'Matteo-Duel_Data\Managed'))) {
    New-Item -ItemType Directory -Path $taskDir -Force | Out-Null
}
Set-Content (Join-Path $taskGame 'Matteo-Duel.exe') 'old executable'
Set-Content (Join-Path $taskGame 'MDPro3-TCG-Genesys.exe') 'old executable'
Set-Content (Join-Path $taskGame 'Data\config.conf') 'personal settings'
Set-Content (Join-Path $taskGame 'Deck\Mine.ydk') 'personal deck'
Set-Content (Join-Path $taskFiles 'Matteo-Duel.exe') 'new executable'
Set-Content (Join-Path $taskFiles 'Matteo-Duel_Data\Managed\fixture.dll') 'new runtime'
$global:taskFixtureZip=Join-Path $taskRoot 'fixture.zip'
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($taskFiles,$global:taskFixtureZip)
$global:taskFixtureManifest=@{version='0.2.0';minimumVersion='0.1.0';sourceCommit='fixture';asset='Matteo-Duel-Update.zip';sha256=(Get-FileHash $global:taskFixtureZip).Hash.ToLowerInvariant();files=@()}
foreach ($taskFile in Get-ChildItem $taskFiles -File -Recurse) {
    $global:taskFixtureManifest.files+=@{path=$taskFile.FullName.Substring($taskFiles.Length+1).Replace('\','/');sha256=(Get-FileHash $taskFile.FullName).Hash.ToLowerInvariant()}
}
function Invoke-RestMethod {
    param($Uri,$Headers,$TimeoutSec)
    if ($Uri -like '*/releases/latest') { return @{tag_name='v0.2.0';html_url='https://example.invalid';assets=@(@{name='update-manifest.json';browser_download_url='https://example.invalid/manifest'},@{name='Matteo-Duel-Update.zip';browser_download_url='https://example.invalid/zip';digest=('sha256:'+$global:taskFixtureManifest.sha256);size=(Get-Item $global:taskFixtureZip).Length})} }
    return $global:taskFixtureManifest
}
function Invoke-WebRequest { param($Uri,$Headers,$OutFile,[switch]$UseBasicParsing,$TimeoutSec) Copy-Item $global:taskFixtureZip $OutFile }
$taskUpdater=Join-Path (Split-Path $PSScriptRoot -Parent) 'Tools\Update-Matteo-Duel.ps1'
& $taskUpdater -GameDirectory $taskGame
if ((Get-Content (Join-Path $taskGame 'Matteo-Duel.exe') -Raw) -ne "new executable`r`n" -and (Get-Content (Join-Path $taskGame 'Matteo-Duel.exe') -Raw) -ne "new executable`n") { throw 'Executable was not updated.' }
if ((Get-FileHash (Join-Path $taskGame 'Matteo-Duel.exe')).Hash -ne (Get-FileHash (Join-Path $taskGame 'MDPro3-TCG-Genesys.exe')).Hash) { throw 'Legacy launcher was not updated.' }
if (!(Test-Path (Join-Path $taskGame 'MDPro3-TCG-Genesys_Data\Managed\fixture.dll'))) { throw 'Legacy runtime was not updated.' }
if ((Get-Content (Join-Path $taskGame 'Data\config.conf')) -ne 'personal settings' -or (Get-Content (Join-Path $taskGame 'Deck\Mine.ydk')) -ne 'personal deck') { throw 'Personal files changed.' }
Write-Host 'PASS updater applies small packages to both launcher paths and preserves personal files.'


