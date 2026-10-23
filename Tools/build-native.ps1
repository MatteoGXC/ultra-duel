param([string]$PremakePath = '')
$ErrorActionPreference = 'Stop'
$taskProject = Split-Path $PSScriptRoot -Parent
if (!$PremakePath) { $PremakePath = Join-Path (Split-Path $taskProject -Parent) 'toolchain/premake/premake5.exe' }
$taskVsWhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
if (!(Test-Path -LiteralPath $taskVsWhere)) { throw 'Visual Studio Installer is not installed.' }
$taskCompiler = & $taskVsWhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -find 'VC/Tools/MSVC/*/bin/Hostx64/x64/cl.exe'
$taskMSBuild = & $taskVsWhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -find 'MSBuild/Current/Bin/MSBuild.exe'
if (!$taskCompiler) {
    # The installer may have laid down usable tools before its registration is final.
    $taskInstallation = & $taskVsWhere -latest -all -products '*' -property installationPath
    if ($taskInstallation) {
        $taskCompiler = Get-ChildItem -Path (Join-Path $taskInstallation 'VC/Tools/MSVC/*/bin/Hostx64/x64/cl.exe') -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
        if ($taskCompiler) { $taskMSBuild = Join-Path $taskInstallation 'MSBuild/Current/Bin/MSBuild.exe' }
    }
}
if (!$taskCompiler) { throw 'Install the Desktop development with C++ workload and a Windows SDK.' }
if (!$taskMSBuild) { throw 'MSBuild was not found in the C++ installation.' }
$taskMSBuild = @($taskMSBuild)[0]
$taskNative = Join-Path $taskProject 'Tools/YGO Classes/build/windows'
Push-Location -LiteralPath $taskNative
try {
    $taskMajor = [int]((& $taskMSBuild -version -nologo | Select-Object -Last 1).Split('.')[0])
    $taskGenerator = if ($taskMajor -ge 18) { 'vs2026' } else { 'vs2022' }
    & $PremakePath $taskGenerator
    if ($LASTEXITCODE -ne 0) { throw 'Native solution generation failed.' }
    $taskSolution = if ($taskGenerator -eq 'vs2026') { 'build/YGO Classes.slnx' } else { 'build/YGO Classes.sln' }
    & $taskMSBuild $taskSolution /m:4 /p:Configuration=Release /p:Platform=x64 /verbosity:minimal /nologo
    if ($LASTEXITCODE -ne 0) { throw 'Native rules-engine build failed.' }
    foreach ($taskLibrary in @('ocgcore.dll','ygoserver.dll')) {
        $taskSource = Join-Path $taskNative "build/bin/x64/Release/$taskLibrary"
        if (!(Test-Path -LiteralPath $taskSource)) { throw "Expected library missing: $taskLibrary" }
        Copy-Item -LiteralPath $taskSource -Destination (Join-Path $taskProject "Assets/Plugins/Windows/$taskLibrary") -Force
    }
    Write-Output 'Native TCG/Genesys libraries built and copied into the Unity project.'
} finally { Pop-Location }
