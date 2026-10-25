param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$taskCompiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$taskOutput=[IO.Path]::GetFullPath($OutputDirectory)
$taskRoot=Split-Path $PSScriptRoot -Parent
New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
& $taskCompiler /nologo /target:library /optimize+ /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll "/out:$taskOutput\Ultra-Duel-Migration.dll" (Join-Path $taskRoot 'Assets\Scripts\MDPro3\Game\UltraInstallMigration.cs') (Join-Path $taskRoot 'Assets\Scripts\MDPro3\Game\UltraUserDataBackup.cs')
if ($LASTEXITCODE -ne 0) { throw 'Ultra Duel migration tools compilation failed.' }
foreach ($taskName in @('Update-Ultra-Duel.ps1','Update-Ultra-Duel.cmd')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $taskName) -Destination $taskOutput -Force
}
