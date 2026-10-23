$ErrorActionPreference = 'Stop'
$taskProject = Split-Path $PSScriptRoot -Parent
$taskVsWhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
$taskInstallation = & $taskVsWhere -latest -all -products '*' -property installationPath
if (!$taskInstallation) { throw 'Visual Studio is not installed.' }
$taskNative = Join-Path $taskProject 'Tools/YGO Classes/build/windows/build'
$taskOutput = Join-Path $taskNative 'bin/x64/Release'
$taskCore = Join-Path $taskProject 'Tools/YGO Classes/ocgcore'
$taskLua = Join-Path $taskProject 'Tools/YGO Classes/lua/src'
$taskObjects = Join-Path $taskNative 'obj/ocgcore/x64/Release/*.obj'
$taskTests = Join-Path $taskProject 'Tests/NativeRuleUnitTests.cpp'
$taskBatch = Join-Path $taskNative 'run-native-rule-tests.cmd'
$taskDevCmd = Join-Path $taskInstallation 'Common7/Tools/VsDevCmd.bat'
@"
@echo off
call "$taskDevCmd" -arch=x64 -host_arch=x64 >nul
if errorlevel 1 exit /b 1
cl /nologo /std:c++17 /EHsc /MD /DWIN32 /I"$taskCore" /I"$taskLua" "$taskTests" "$taskObjects" "$taskOutput/lua.lib" /Fo"$taskOutput/NativeRuleUnitTests.obj" /Fe"$taskOutput/NativeRuleUnitTests.exe"
if errorlevel 1 exit /b 1
"$taskOutput/NativeRuleUnitTests.exe"
exit /b %errorlevel%
"@ | Set-Content -LiteralPath $taskBatch -Encoding ascii
& cmd.exe /d /c $taskBatch
if ($LASTEXITCODE -ne 0) { throw 'Native rule checks failed.' }
