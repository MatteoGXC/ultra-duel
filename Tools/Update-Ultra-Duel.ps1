param([string]$GameDirectory = $PSScriptRoot, [switch]$CheckOnly, [switch]$SkipShortcuts, [string]$BackupDirectory)
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$taskGame = [IO.Path]::GetFullPath($GameDirectory)
$taskTools = Join-Path $PSScriptRoot 'Ultra-Duel-Migration.dll'
if (!(Test-Path -LiteralPath $taskTools)) { throw 'Extract the complete Ultra Duel updater ZIP before running it.' }
# Load an isolated copy so Windows never locks the DLL being updated in the game folder.
$taskCache = Join-Path ([IO.Path]::GetTempPath()) ('UltraDuel-update-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $taskCache | Out-Null
$taskMigrationLibrary = Join-Path $taskCache 'Ultra-Duel-Migration.dll'
Copy-Item -LiteralPath $taskTools -Destination $taskMigrationLibrary
if (!('MDPro3.UltraInstallMigration' -as [type])) { Add-Type -Path $taskMigrationLibrary }
if (![MDPro3.UltraInstallMigration]::IsInstallation($taskGame)) {
    throw 'Put the updater in your existing Ultra Duel game folder, then run it again.'
}
$taskHeaders = @{ 'User-Agent'='Ultra-Duel-Updater'; Accept='application/vnd.github+json' }
$taskRepo = 'https://api.github.com/repos/MatteoGXC/ultra-duel'
$taskInstalled = [version]'0.1.0' # First public installer predates the version marker.
$taskMarker = Join-Path $taskGame 'Data\installed-version.json'
if (Test-Path -LiteralPath $taskMarker) {
    $taskInstalled = [version]((Get-Content -LiteralPath $taskMarker -Raw | ConvertFrom-Json).version -replace '^v','')
}
Write-Host "Ultra Duel $taskInstalled - checking for updates..."
$taskRelease = Invoke-RestMethod -Uri "$taskRepo/releases/latest" -Headers $taskHeaders -TimeoutSec 30
$taskAvailable = [version]($taskRelease.tag_name -replace '^v','')
if ($taskAvailable -le $taskInstalled) {
    if (!$CheckOnly -and [MDPro3.UltraInstallMigration]::IsCompiledUnityPlayer($taskGame) -and
        @([MDPro3.UltraInstallMigration]::LegacyDataDirectories | Where-Object { Test-Path -LiteralPath (Join-Path $taskGame $_) }).Count -gt 0) {
        [MDPro3.UltraInstallMigration]::AssertNotRunning($taskGame)
        if (!$BackupDirectory) { $BackupDirectory=Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Ultra Duel\UserData' }
        $taskRecoveryBackup=New-Object MDPro3.UltraUserDataBackup($taskGame,$BackupDirectory)
        [void]$taskRecoveryBackup.Capture()
        $taskRecovery=New-Object MDPro3.UltraInstallMigration($taskGame)
        try { $taskRecovery.Prepare();$taskRecovery.Complete() }
        catch { $taskRecovery.Rollback();throw }
        Write-Host 'Completed the pending runtime migration. Launch Ultra-Duel.exe.' -ForegroundColor Green
        return
    }
    Write-Host 'You already have the latest published Windows release.' -ForegroundColor Green
    return
}
Write-Host "Update available: $($taskRelease.tag_name)"
if ($CheckOnly) { return }
$taskManifestAsset = $taskRelease.assets | Where-Object name -eq 'update-manifest.json' | Select-Object -First 1
if (!$taskManifestAsset) {
    throw "A small update package is not published for $($taskRelease.tag_name) yet. Nothing was downloaded or changed. See $($taskRelease.html_url)"
}
$taskManifest = Invoke-RestMethod -Uri $taskManifestAsset.browser_download_url -Headers $taskHeaders -TimeoutSec 30
if ([version]$taskManifest.version -ne $taskAvailable -or [version]$taskManifest.minimumVersion -gt $taskInstalled) {
    throw "This update does not support your installed version. See $($taskRelease.html_url)"
}
$taskAsset = $taskRelease.assets | Where-Object name -eq $taskManifest.asset | Select-Object -First 1
if (!$taskAsset -or !$taskAsset.digest -or $taskAsset.digest -ne ('sha256:' + $taskManifest.sha256)) {
    throw 'The published update checksum is missing or inconsistent. No files were changed.'
}
[MDPro3.UltraInstallMigration]::AssertNotRunning($taskGame)
if ($taskManifest.runtimeLayout -ne 'ultra-v1') { throw 'The published package does not support this runtime layout. The game was not changed.' }
$taskZip = Join-Path $taskCache 'update.zip'
Write-Host ('Downloading update only: {0:N1} MB' -f ($taskAsset.size / 1MB))
Invoke-WebRequest -Uri $taskAsset.browser_download_url -Headers $taskHeaders -OutFile $taskZip -UseBasicParsing -TimeoutSec 1200
if ((Get-FileHash -LiteralPath $taskZip -Algorithm SHA256).Hash.ToLowerInvariant() -ne $taskManifest.sha256) {
    throw 'Downloaded update failed checksum verification. No game files were changed.'
}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$taskStage = Join-Path $taskCache 'files'
[IO.Compression.ZipFile]::ExtractToDirectory($taskZip, $taskStage)
try {
    foreach ($taskFile in $taskManifest.files) {
        $taskRelative = [string]$taskFile.path
        if ([MDPro3.UltraInstallMigration]::CanonicalArchivePath($taskRelative) -ne $taskRelative) { throw 'A new update contains obsolete runtime paths.' }
        $taskTarget = [IO.Path]::GetFullPath((Join-Path $taskGame $taskRelative))
        $taskSource = [IO.Path]::GetFullPath((Join-Path $taskStage $taskRelative))
        if (!$taskTarget.StartsWith($taskGame.TrimEnd('\')+'\', [StringComparison]::OrdinalIgnoreCase) -or
            !$taskSource.StartsWith($taskStage+'\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid update path.' }
        if ($taskRelative -match '^(Deck|Replay|Progress)(/|\\)' -or $taskRelative -match '^Data[/\\](config\.conf|mycard_apps\.json|profile\.json|friends-session\.json|Settings\.json)$') {
            throw 'Update attempted to replace personal data.'
        }
        if (!(Test-Path -LiteralPath $taskSource) -or (Get-FileHash -LiteralPath $taskSource -Algorithm SHA256).Hash.ToLowerInvariant() -ne $taskFile.sha256) {
            throw "Update file verification failed: $taskRelative"
        }
    }
    # Nothing in the installation is changed until every payload hash is verified.
    [MDPro3.UltraInstallMigration]::AssertNotRunning($taskGame)
    if (!$BackupDirectory) { $BackupDirectory=Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Ultra Duel\UserData' }
    $taskPersonalBackup=New-Object MDPro3.UltraUserDataBackup($taskGame,$BackupDirectory)
    [void]$taskPersonalBackup.Capture()
    $taskMigration=New-Object MDPro3.UltraInstallMigration($taskGame)
    $taskMigration.Prepare()
    $taskCopies = @($taskManifest.files | ForEach-Object { @{ source=[string]$_.path; destination=[string]$_.path } })
    foreach ($taskCopy in $taskCopies) {
        $taskExisting=Join-Path $taskGame $taskCopy.destination
        $taskExpected=($taskManifest.files | Where-Object path -eq $taskCopy.source | Select-Object -First 1).sha256
        if((Test-Path -LiteralPath $taskExisting) -and (Get-FileHash -LiteralPath $taskExisting -Algorithm SHA256).Hash.ToLowerInvariant() -eq $taskExpected){continue}
        $taskTarget = $taskMigration.PrepareReplacement($taskCopy.destination)
        New-Item -ItemType Directory -Path (Split-Path $taskTarget -Parent) -Force | Out-Null
        $taskTemporary=$taskTarget+'.ultra-install-'+[Guid]::NewGuid().ToString('N')
        try {
            Copy-Item -LiteralPath (Join-Path $taskStage $taskCopy.source) -Destination $taskTemporary -Force
            [MDPro3.UltraInstallMigration]::ReplaceFile($taskTemporary,$taskTarget)
        } finally {
            if(Test-Path -LiteralPath $taskTemporary){Remove-Item -LiteralPath $taskTemporary}
        }
        if ((Get-FileHash -LiteralPath $taskTarget -Algorithm SHA256).Hash.ToLowerInvariant() -ne $taskExpected) { throw 'Installed file checksum mismatch.' }
    }
    [MDPro3.UltraInstallMigration]::ValidateRuntime($taskGame)
    $taskMigration.Complete()
    [void]$taskPersonalBackup.Capture()
    @{ version=$taskManifest.version; runtimeLayout='ultra-v1'; sourceCommit=$taskManifest.sourceCommit; updatedAt=[DateTime]::UtcNow.ToString('o') } |
        ConvertTo-Json | Set-Content -LiteralPath $taskMarker -Encoding UTF8
    if (!$SkipShortcuts) {
        try {
            $taskShell=New-Object -ComObject WScript.Shell
            $taskDesktop=[Environment]::GetFolderPath('DesktopDirectory')
            foreach ($taskLink in Get-ChildItem -LiteralPath $taskDesktop -Filter '*.lnk') {
                $taskShortcut=$taskShell.CreateShortcut($taskLink.FullName)
                if ([MDPro3.UltraInstallMigration]::LegacyExecutables | Where-Object { $taskShortcut.TargetPath -eq (Join-Path $taskGame $_) }) {
                    $taskShortcut.TargetPath=Join-Path $taskGame 'Ultra-Duel.exe';$taskShortcut.WorkingDirectory=$taskGame;$taskShortcut.Save()
                }
            }
            $taskShortcut=$taskShell.CreateShortcut((Join-Path $taskDesktop 'Ultra Duel.lnk'))
            $taskShortcut.TargetPath=Join-Path $taskGame 'Ultra-Duel.exe';$taskShortcut.WorkingDirectory=$taskGame;$taskShortcut.Save()
        } catch { Write-Warning 'Launch Ultra-Duel.exe from the game folder if the desktop shortcut could not be updated.' }
    }
    Write-Host "Updated to $($taskManifest.version). Your decks, settings and progress were preserved." -ForegroundColor Green
} catch {
    if ($taskMigration) { $taskMigration.Rollback() }
    throw
}
Write-Host 'You can now launch Ultra-Duel.exe. Superseded runtime files were archived for recovery.'
