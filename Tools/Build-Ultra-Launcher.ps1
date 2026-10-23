param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$taskCompiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$taskOutput=[IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
& $taskCompiler /nologo /target:winexe /r:System.Windows.Forms.dll "/out:$taskOutput\Ultra-Duel.exe" (Join-Path $PSScriptRoot 'UltraDuelLauncher.cs')
if ($LASTEXITCODE -ne 0) { throw 'Ultra Duel launcher compilation failed.' }
foreach ($taskName in @('Update-Ultra-Duel.ps1','Update-Ultra-Duel.cmd','Update-Matteo-Duel.ps1','Update-Matteo-Duel.cmd')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $taskName) -Destination $taskOutput -Force
}
