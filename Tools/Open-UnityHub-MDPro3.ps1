$ErrorActionPreference = "Stop"

$hub = "D:\Unity\Unity Hub\Unity Hub.exe"
$project = "D:\game\MDPro3-src"

if (-not (Test-Path -LiteralPath $hub)) {
    throw "Unity Hub not found: $hub"
}

if (-not (Test-Path -LiteralPath $project)) {
    throw "Project not found: $project"
}

Start-Process -FilePath $hub
Start-Process -FilePath "D:\Unity\Editors\6000.0.28f1\Editor\Unity.exe" -ArgumentList @("-projectPath", $project)
