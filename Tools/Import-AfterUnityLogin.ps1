$ErrorActionPreference = "Stop"

$unity = "D:\Unity\Editors\6000.0.28f1\Editor\Unity.exe"
$project = "D:\game\MDPro3-src"
$logDir = Join-Path $project "Logs"
$log = Join-Path $logDir "unity-import.log"

if (-not (Test-Path -LiteralPath $unity)) {
    throw "Unity editor not found: $unity"
}

if (-not (Test-Path -LiteralPath $project)) {
    throw "Project not found: $project"
}

New-Item -ItemType Directory -Force -Path $logDir | Out-Null

$env:HTTP_PROXY = "http://127.0.0.1:4780"
$env:HTTPS_PROXY = "http://127.0.0.1:4780"
$env:UPM_CACHE_PATH = "D:\Unity\PackageCache"
$env:UNITY_UPM_CACHE_PATH = "D:\Unity\PackageCache"

& $unity -batchmode -nographics -projectPath $project -quit -logFile $log
$exitCode = $LASTEXITCODE

Write-Host "Unity exit code: $exitCode"
Write-Host "Log: $log"

if (Test-Path -LiteralPath $log) {
    Get-Content -LiteralPath $log -Tail 80
}

exit $exitCode
