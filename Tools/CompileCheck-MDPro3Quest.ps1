param(
    [string]$UnityExe = "D:\Unity\Editors\6000.0.28f1\Editor\Unity.exe",
    [string]$ProjectRoot = "D:\game\MDPro3-src"
)

$ErrorActionPreference = "Stop"

if (!(Test-Path -LiteralPath $UnityExe -PathType Leaf)) {
    throw "Unity editor was not found: $UnityExe"
}

if (!(Test-Path -LiteralPath $ProjectRoot -PathType Container)) {
    throw "Project root was not found: $ProjectRoot"
}

$logs = Join-Path $ProjectRoot "Logs"
New-Item -ItemType Directory -Force -Path $logs | Out-Null
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$logFile = Join-Path $logs "codex-quest-compile-check-$stamp.log"

Write-Host "Starting Unity compile/import check. Log: $logFile"
$process = Start-Process -FilePath $UnityExe -ArgumentList @(
    "-batchmode",
    "-quit",
    "-projectPath", $ProjectRoot,
    "-logFile", $logFile
) -Wait -PassThru

if ($process.ExitCode -ne 0) {
    throw "Unity compile/import check failed with exit code $($process.ExitCode). Check log: $logFile"
}

$errors = Select-String -Path $logFile -Pattern "error CS|Scripts have compiler errors|Compilation failed" -SimpleMatch:$false
if ($errors) {
    $errors | Select-Object -First 20 | ForEach-Object { Write-Host $_.Line }
    throw "Unity compile/import check found compiler errors. Check log: $logFile"
}

Write-Host "Unity compile/import check completed. Log: $logFile"
