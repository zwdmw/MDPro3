param(
    [string]$RuntimeRoot = "D:\game\MDPro3",
    [string]$ProjectRoot = "D:\game\MDPro3-src",
    [string]$AndroidAssetsSource = "",
    [switch]$EmbedBasePayloads,
    [switch]$EmbedArt3D,
    [switch]$IncludePictures,
    [switch]$IncludeSound,
    [switch]$IncludeExpansions,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$streamingAssets = Join-Path $ProjectRoot "Assets\StreamingAssets"
New-Item -ItemType Directory -Force -Path $streamingAssets | Out-Null

function New-ZipFromFolder {
    param(
        [Parameter(Mandatory = $true)][string]$SourceFolder,
        [Parameter(Mandatory = $true)][string]$ZipPath,
        [Parameter(Mandatory = $true)][string]$EntryRoot
    )

    if (!(Test-Path -LiteralPath $SourceFolder -PathType Container)) {
        Write-Warning "Missing folder: $SourceFolder"
        return
    }

    if (Test-Path -LiteralPath $ZipPath) {
        if (!$Force) {
            Write-Host "Skip existing payload: $ZipPath"
            return
        }
        Remove-Item -LiteralPath $ZipPath -Force
    }

    Write-Host "Packing $SourceFolder -> $ZipPath"
    $sourceFullPath = [IO.Path]::GetFullPath($SourceFolder).TrimEnd('\', '/')
    $zip = [IO.Compression.ZipFile]::Open($ZipPath, [IO.Compression.ZipArchiveMode]::Create)
    try {
        Get-ChildItem -LiteralPath $sourceFullPath -Recurse -File | ForEach-Object {
            $relative = $_.FullName.Substring($sourceFullPath.Length).TrimStart('\', '/')
            $entryName = ($EntryRoot.TrimEnd('\', '/') + "/" + $relative.Replace('\', '/')).TrimStart('/')
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, $entryName, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
    finally {
        $zip.Dispose()
    }

    $sizeMb = [Math]::Round((Get-Item -LiteralPath $ZipPath).Length / 1MB, 1)
    Write-Host "Created $ZipPath ($sizeMb MB)"
}

function New-ZipFromAndroidAssets {
    param(
        [Parameter(Mandatory = $true)][string]$SourceFolder
    )

    if (!(Test-Path -LiteralPath $SourceFolder -PathType Container)) {
        throw "Android asset folder does not exist: $SourceFolder"
    }

    $SourceFolder = Resolve-AndroidAssetRoot -SourceFolder $SourceFolder
    Assert-AndroidAssetFolder -SourceFolder $SourceFolder
    New-ZipFromFolder -SourceFolder $SourceFolder -ZipPath (Join-Path $streamingAssets "Android.zip") -EntryRoot "Android"
}

function Remove-EmbeddedPayload {
    param(
        [Parameter(Mandatory = $true)][string]$Name
    )

    $zipPath = Join-Path $streamingAssets $Name
    $metaPath = "$zipPath.meta"
    if (Test-Path -LiteralPath $zipPath -PathType Leaf) {
        Remove-Item -LiteralPath $zipPath -Force
        Write-Host "Removed APK embedded payload: $zipPath"
    }
    if (Test-Path -LiteralPath $metaPath -PathType Leaf) {
        Remove-Item -LiteralPath $metaPath -Force
    }
}

function Resolve-AndroidAssetRoot {
    param(
        [Parameter(Mandatory = $true)][string]$SourceFolder
    )

    $fullPath = [IO.Path]::GetFullPath($SourceFolder)
    if ((Test-Path -LiteralPath (Join-Path $fullPath "MasterDuel") -PathType Container) -or
        (Test-Path -LiteralPath (Join-Path $fullPath "MDPro3") -PathType Container)) {
        return $fullPath
    }

    $nestedAndroid = Join-Path $fullPath "Android"
    if (Test-Path -LiteralPath (Join-Path $nestedAndroid "MasterDuel") -PathType Container) {
        Write-Host "Using nested Android asset root: $nestedAndroid"
        return $nestedAndroid
    }

    return $fullPath
}

function Assert-AndroidAssetFolder {
    param(
        [Parameter(Mandatory = $true)][string]$SourceFolder
    )

    $required = @(
        "MasterDuel",
        "MasterDuel\Wallpaper\front0001",
        "MasterDuel\Material",
        "MasterDuel\Effects"
    )

    foreach ($relative in $required) {
        $path = Join-Path $SourceFolder $relative
        if (!(Test-Path -LiteralPath $path)) {
            throw "Android asset folder is incomplete. Missing required path: $path"
        }
    }
}

if (!(Test-Path -LiteralPath $RuntimeRoot -PathType Container)) {
    throw "Runtime root does not exist: $RuntimeRoot"
}

if ($EmbedBasePayloads) {
    New-ZipFromFolder -SourceFolder (Join-Path $RuntimeRoot "Data") -ZipPath (Join-Path $streamingAssets "Data.zip") -EntryRoot "Data"
}
else {
    Remove-EmbeddedPayload -Name "Data.zip"
}

New-ZipFromFolder -SourceFolder (Join-Path $RuntimeRoot "Deck") -ZipPath (Join-Path $streamingAssets "Deck.zip") -EntryRoot "Deck"
New-ZipFromFolder -SourceFolder (Join-Path $RuntimeRoot "Puzzle") -ZipPath (Join-Path $streamingAssets "Puzzle.zip") -EntryRoot "Puzzle"

if ($EmbedArt3D) {
    New-ZipFromFolder -SourceFolder (Join-Path $RuntimeRoot "Picture\Art3D") -ZipPath (Join-Path $streamingAssets "Picture_Art3D.zip") -EntryRoot "Art3D"
}
else {
    Remove-EmbeddedPayload -Name "Picture_Art3D.zip"
}

if ($IncludeExpansions) {
    New-ZipFromFolder -SourceFolder (Join-Path $RuntimeRoot "Expansions") -ZipPath (Join-Path $streamingAssets "Expansions.zip") -EntryRoot "Expansions"
}

if ($IncludePictures) {
    New-ZipFromFolder -SourceFolder (Join-Path $RuntimeRoot "Picture") -ZipPath (Join-Path $streamingAssets "Picture.zip") -EntryRoot "Picture"
}

if ($IncludeSound) {
    New-ZipFromFolder -SourceFolder (Join-Path $RuntimeRoot "Sound") -ZipPath (Join-Path $streamingAssets "Sound.zip") -EntryRoot "Sound"
}

if ($EmbedBasePayloads -and ![string]::IsNullOrWhiteSpace($AndroidAssetsSource)) {
    New-ZipFromAndroidAssets -SourceFolder $AndroidAssetsSource
}
else {
    Remove-EmbeddedPayload -Name "Android.zip"
    Write-Host "Android/Data/Art3D payloads are expected to be deployed externally with Install-QuestRuntimeResources.ps1."
}

Write-Host "Quest runtime payload folder: $streamingAssets"
