param([string]$GameDirectory = $PSScriptRoot, [switch]$CheckOnly)
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$taskGame = [IO.Path]::GetFullPath($GameDirectory)
if (!(Test-Path -LiteralPath (Join-Path $taskGame 'Matteo-Duel.exe'))) {
    throw 'Put this updater in the folder containing Matteo-Duel.exe, then run it again.'
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
$taskRunning = Get-Process -ErrorAction SilentlyContinue | Where-Object {
    $_.Path -and ($_.Path -eq (Join-Path $taskGame 'Matteo-Duel.exe') -or $_.Path -eq (Join-Path $taskGame 'MDPro3-TCG-Genesys.exe'))
}
if ($taskRunning) { throw 'Close Ultra Duel before updating, then run this updater again.' }
$taskCache = Join-Path ([IO.Path]::GetTempPath()) ('MatteoDuel-update-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $taskCache | Out-Null
$taskZip = Join-Path $taskCache 'update.zip'
Write-Host ('Downloading update only: {0:N1} MB' -f ($taskAsset.size / 1MB))
Invoke-WebRequest -Uri $taskAsset.browser_download_url -Headers $taskHeaders -OutFile $taskZip -UseBasicParsing -TimeoutSec 1200
if ((Get-FileHash -LiteralPath $taskZip -Algorithm SHA256).Hash.ToLowerInvariant() -ne $taskManifest.sha256) {
    throw 'Downloaded update failed checksum verification. No game files were changed.'
}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$taskStage = Join-Path $taskCache 'files'
[IO.Compression.ZipFile]::ExtractToDirectory($taskZip, $taskStage)
$taskRollback = Join-Path $taskCache 'rollback'
$taskApplied = New-Object 'System.Collections.Generic.List[object]'
try {
    foreach ($taskFile in $taskManifest.files) {
        $taskRelative = [string]$taskFile.path
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
    $taskCopies = New-Object 'System.Collections.Generic.List[object]'
    foreach ($taskFile in $taskManifest.files) {
        $taskRelative = [string]$taskFile.path
        $taskCopies.Add(@{ source=$taskRelative; destination=$taskRelative })
        if (Test-Path -LiteralPath (Join-Path $taskGame 'MDPro3-TCG-Genesys.exe')) {
            if ($taskRelative -eq 'Matteo-Duel.exe') {
                $taskCopies.Add(@{ source=$taskRelative; destination='MDPro3-TCG-Genesys.exe' })
            } elseif ($taskRelative -like 'Matteo-Duel_Data/*') {
                $taskCopies.Add(@{ source=$taskRelative; destination=($taskRelative -replace '^Matteo-Duel_Data/','MDPro3-TCG-Genesys_Data/') })
            }
        }
    }
    foreach ($taskCopy in $taskCopies) {
        $taskTarget = Join-Path $taskGame $taskCopy.destination
        $taskBackup = Join-Path $taskRollback $taskCopy.destination
        $taskExisted = Test-Path -LiteralPath $taskTarget
        if ($taskExisted) {
            New-Item -ItemType Directory -Path (Split-Path $taskBackup -Parent) -Force | Out-Null
            Copy-Item -LiteralPath $taskTarget -Destination $taskBackup
        }
        $taskApplied.Add(@{ target=$taskTarget; backup=$taskBackup; existed=$taskExisted })
        New-Item -ItemType Directory -Path (Split-Path $taskTarget -Parent) -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $taskStage $taskCopy.source) -Destination $taskTarget -Force
    }
    @{ version=$taskManifest.version; sourceCommit=$taskManifest.sourceCommit; updatedAt=[DateTime]::UtcNow.ToString('o') } |
        ConvertTo-Json | Set-Content -LiteralPath $taskMarker -Encoding UTF8
    Write-Host "Updated to $($taskManifest.version). Your decks, settings and progress were preserved." -ForegroundColor Green
} catch {
    for ($taskIndex=$taskApplied.Count-1; $taskIndex -ge 0; $taskIndex--) {
        $taskEntry=$taskApplied[$taskIndex]
        if ($taskEntry.existed) { Copy-Item -LiteralPath $taskEntry.backup -Destination $taskEntry.target -Force }
        elseif (Test-Path -LiteralPath $taskEntry.target) { Remove-Item -LiteralPath $taskEntry.target }
    }
    throw
}
Write-Host 'You can now launch Ultra-Duel.exe (existing Matteo-Duel.exe shortcuts also work).'
