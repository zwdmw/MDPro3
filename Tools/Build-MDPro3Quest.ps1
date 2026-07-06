param(
    [string]$UnityExe = "D:\Unity\Editors\6000.0.28f1\Editor\Unity.exe",
    [string]$ProjectRoot = "D:\game\MDPro3-src",
    [switch]$Development
)

$ErrorActionPreference = "Stop"

if (!(Test-Path -LiteralPath $UnityExe -PathType Leaf)) {
    throw "Unity editor was not found: $UnityExe"
}

if (!(Test-Path -LiteralPath $ProjectRoot -PathType Container)) {
    throw "Project root was not found: $ProjectRoot"
}

$editorDir = Split-Path -Parent $UnityExe
$androidPlayer = Join-Path $editorDir "Data\PlaybackEngines\AndroidPlayer"
if (!(Test-Path -LiteralPath $androidPlayer -PathType Container)) {
    throw @"
Android Build Support is not installed for this Unity editor.
Expected folder:
$androidPlayer

Install Android Build Support, Android SDK & NDK Tools, and OpenJDK for Unity 6000.0.28f1, then run this script again.
"@
}

$logs = Join-Path $ProjectRoot "Logs"
New-Item -ItemType Directory -Force -Path $logs | Out-Null
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$logFile = Join-Path $logs "codex-quest-build-$stamp.log"

$buildRoot = "D:\game\MDPro3-Quest"
$gradleHome = Join-Path $buildRoot "GradleHome"
$tempRoot = Join-Path $buildRoot "Temp"
New-Item -ItemType Directory -Force -Path $buildRoot, $gradleHome, $tempRoot | Out-Null

$env:GRADLE_USER_HOME = $gradleHome
$env:TEMP = $tempRoot
$env:TMP = $tempRoot
$env:TMPDIR = $tempRoot

$arguments = @(
    "-batchmode",
    "-quit",
    "-projectPath", $ProjectRoot,
    "-executeMethod", "MDPro3.EditorTools.QuestBuild.BuildQuestApkCommandLine",
    "-logFile", $logFile
)

if ($Development) {
    $arguments += "-development"
}

Write-Host "Starting Unity Quest build. Log: $logFile"
$process = Start-Process -FilePath $UnityExe -ArgumentList $arguments -Wait -PassThru
if ($process.ExitCode -ne 0) {
    throw "Unity Quest build failed with exit code $($process.ExitCode). Check log: $logFile"
}

Write-Host "Unity Quest build completed. Log: $logFile"
