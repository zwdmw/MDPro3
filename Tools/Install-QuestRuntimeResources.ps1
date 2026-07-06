param(
    [string]$PackageName = "com.ygo.mdpro3.quest",
    [string]$RuntimeRoot = "D:\game\MDPro3",
    [string]$ZipRoot = "D:\game\MDPro3-AndroidAIO",
    [string]$Adb = "D:\Unity\Editors\6000.0.28f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe",
    [string]$TempRoot = "D:\game\MDPro3-Quest\ResourceDeploy",
    [switch]$ForceExtractPayloadZips,
    [switch]$KeepStagedZips,
    [switch]$SkipPictures,
    [switch]$SkipSound
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

if (!(Test-Path -LiteralPath $Adb -PathType Leaf)) {
    throw "adb.exe was not found: $Adb"
}

if (!(Test-Path -LiteralPath $ZipRoot -PathType Container)) {
    throw "Resource zip root was not found: $ZipRoot"
}

New-Item -ItemType Directory -Force -Path $TempRoot | Out-Null

$remoteRoot = "/storage/emulated/0/Android/data/$PackageName/files"
$remoteStaging = "$remoteRoot/.resource-staging"

$payloads = @(
    @{ Zip = "Data-V1.4.4F.zip"; Apk = "MDPro3_V1.4.4-Part2.apk"; Destination = "." },
    @{ Zip = "Puzzle-V1.4.4F.zip"; Apk = "MDPro3_V1.4.4-Part2.apk"; Destination = "." },
    @{ Zip = "Replay-V1.4.4F.zip"; Apk = "MDPro3_V1.4.4-Part2.apk"; Destination = "." },
    @{ Zip = "Video-V1.4.4F.zip"; Apk = "MDPro3_V1.4.4-Part2.apk"; Destination = "." },
    @{ Zip = "Android-V1.4.4F.zip"; Apk = "MDPro3_V1.4.4-Part2.apk"; Destination = "." },
    @{ Zip = "Android_CrossDuel-V1.4.4F.zip"; Apk = "MDPro3_V1.4.4-Part2.apk"; Destination = "Android" },
    @{ Zip = "Android_MonsterCutin-V1.4.4F.zip"; Apk = "MDPro3_V1.4.4-Part2.apk"; Destination = "Android" },
    @{ Zip = "Android_MonsterCutin2-V1.4.4F.zip"; Apk = "MDPro3_V1.4.4-Part2.apk"; Destination = "Android" },
    @{ Zip = "Picture_Art-V1.4.4F.zip"; Apk = "MDPro3_V1.4.4-Part3.apk"; Destination = "Picture"; Group = "Picture" },
    @{ Zip = "Picture_Art2-V1.4.4F.zip"; Apk = "MDPro3_V1.4.4-Part3.apk"; Destination = "Picture"; Group = "Picture" },
    @{ Zip = "Picture_Closeup-V1.4.4F.zip"; Apk = "MDPro3_V1.4.4-Part3.apk"; Destination = "Picture"; Group = "Picture" },
    @{ Zip = "Picture_DIY-V1.4.4F.zip"; Apk = "MDPro3_V1.4.4-Part3.apk"; Destination = "Picture"; Group = "Picture" },
    @{ Zip = "Picture_OverFrame-V1.4.4F.zip"; Apk = "MDPro3_V1.4.4-Part3.apk"; Destination = "Picture"; Group = "Picture" },
    @{ Zip = "Sound-V1.4.4F.zip"; Apk = "MDPro3_V1.4.4-Part4.apk"; Destination = "."; Group = "Sound" }
)

if ($SkipPictures) {
    $payloads = @($payloads | Where-Object { $_.Group -ne "Picture" })
}

if ($SkipSound) {
    $payloads = @($payloads | Where-Object { $_.Group -ne "Sound" })
}

function Invoke-Adb {
    param(
        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]]$Arguments
    )

    & $Adb @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "adb failed: $($Arguments -join ' ')"
    }
}

function Expand-MissingPayloadsFromApk {
    param(
        [Parameter(Mandatory = $true)][array]$Payloads
    )

    $missingByApk = @{}
    foreach ($payload in $Payloads) {
        $zipPath = Join-Path $ZipRoot $payload.Zip
        if ((Test-Path -LiteralPath $zipPath -PathType Leaf) -and !$ForceExtractPayloadZips) {
            continue
        }

        if (!$missingByApk.ContainsKey($payload.Apk)) {
            $missingByApk[$payload.Apk] = New-Object System.Collections.Generic.List[string]
        }
        $missingByApk[$payload.Apk].Add($payload.Zip)
    }

    foreach ($apkName in $missingByApk.Keys) {
        $apkPath = Join-Path $ZipRoot $apkName
        if (!(Test-Path -LiteralPath $apkPath -PathType Leaf)) {
            throw "Required AIO apk was not found: $apkPath"
        }

        $needed = @{}
        foreach ($zipName in $missingByApk[$apkName]) {
            $needed[$zipName] = $true
        }

        Write-Host "Extracting payload zips from $apkPath"
        $apkArchive = [IO.Compression.ZipFile]::OpenRead($apkPath)
        try {
            foreach ($entry in $apkArchive.Entries) {
                if (!$entry.FullName.StartsWith("assets/", [StringComparison]::OrdinalIgnoreCase)) {
                    continue
                }

                $fileName = [IO.Path]::GetFileName($entry.FullName)
                if (!$needed.ContainsKey($fileName)) {
                    continue
                }

                $target = Join-Path $ZipRoot $fileName
                if ((Test-Path -LiteralPath $target -PathType Leaf) -and !$ForceExtractPayloadZips) {
                    continue
                }

                [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $target, $true)
                Write-Host "Extracted $target"
            }
        }
        finally {
            $apkArchive.Dispose()
        }
    }
}

function Update-RemoteConfigMarkers {
    param(
        [Parameter(Mandatory = $true)][string[]]$Markers
    )

    $remoteConfig = "$remoteRoot/Data/config.conf"
    $localConfig = Join-Path $TempRoot "config.conf"
    $forcedSettings = [ordered]@{
        Language = "zh-CN"
        CardLanguage = "zh-CN"
    }

    $existingLines = & $Adb shell "cat '$remoteConfig' 2>/dev/null"
    if ($LASTEXITCODE -ne 0) {
        $existingLines = @()
    }

    $markerSet = @{}
    foreach ($marker in $Markers) {
        $markerSet[$marker] = $true
    }
    foreach ($key in $forcedSettings.Keys) {
        $markerSet[$key] = $true
    }

    $lines = New-Object System.Collections.Generic.List[string]
    foreach ($line in $existingLines) {
        $cleanLine = ($line -replace "`r", "").TrimEnd()
        if ([string]::IsNullOrWhiteSpace($cleanLine)) {
            continue
        }

        $key = ($cleanLine -split "->", 2)[0]
        if (!$markerSet.ContainsKey($key)) {
            $lines.Add($cleanLine)
        }
    }

    foreach ($marker in $Markers) {
        $lines.Add("$marker->1")
    }
    foreach ($key in $forcedSettings.Keys) {
        $lines.Add("$key->$($forcedSettings[$key])")
    }

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($localConfig, (($lines -join "`r`n") + "`r`n"), $utf8NoBom)
    Invoke-Adb shell "mkdir -p '$remoteRoot/Data'"
    Invoke-Adb push $localConfig $remoteConfig
    Invoke-Adb shell "chmod g+rw '$remoteConfig' 2>/dev/null || true"
}

function Ensure-RemoteAppDirectory {
    param(
        [Parameter(Mandatory = $true)][string]$RemotePath
    )

    & $Adb shell "run-as $PackageName mkdir -p '$RemotePath' 2>/dev/null"
    if ($LASTEXITCODE -eq 0) {
        return
    }

    & $Adb shell "mkdir -p '$RemotePath' 2>/dev/null"
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Could not create remote directory directly. Existing directory is required for adb push: $RemotePath"
    }
}

function Deploy-RuntimeFolder {
    param(
        [Parameter(Mandatory = $true)][string]$LocalFolder,
        [Parameter(Mandatory = $true)][string]$RemoteFolder
    )

    if (!(Test-Path -LiteralPath $LocalFolder -PathType Container)) {
        Write-Warning "Optional runtime folder was not found: $LocalFolder"
        return
    }

    $sourceRoot = [IO.Path]::GetFullPath($LocalFolder).TrimEnd('\', '/')
    $files = @(Get-ChildItem -LiteralPath $sourceRoot -Recurse -File)
    $totalBytes = ($files | Measure-Object -Property Length -Sum).Sum
    if ($null -eq $totalBytes) {
        $totalBytes = 0
    }

    $archiveBase = $RemoteFolder
    if ($archiveBase.StartsWith($remoteRoot, [StringComparison]::Ordinal)) {
        $archiveBase = $archiveBase.Substring($remoteRoot.Length).TrimStart('/')
    }
    if ([string]::IsNullOrWhiteSpace($archiveBase)) {
        $archiveBase = [IO.Path]::GetFileName($sourceRoot)
    }

    $archiveName = "Runtime_" + ([Regex]::Replace($archiveBase.Replace('/', '_'), '[^A-Za-z0-9._-]+', '_')) + ".zip"
    $localZip = Join-Path $TempRoot $archiveName
    $remoteZip = "$remoteStaging/$archiveName"

    if (Test-Path -LiteralPath $localZip -PathType Leaf) {
        Remove-Item -LiteralPath $localZip -Force
    }

    $sizeMb = [Math]::Round($totalBytes / 1MB, 1)
    Write-Host "Packing runtime folder $LocalFolder ($($files.Count) files, $sizeMb MB)"
    [IO.Compression.ZipFile]::CreateFromDirectory($sourceRoot, $localZip, [IO.Compression.CompressionLevel]::Fastest, $false)

    $zipSizeMb = [Math]::Round((Get-Item -LiteralPath $localZip).Length / 1MB, 1)
    Write-Host "Deploying $archiveName ($zipSizeMb MB) -> $RemoteFolder"
    Invoke-Adb push $localZip $remoteZip

    if (!$RemoteFolder.StartsWith("$remoteRoot/", [StringComparison]::Ordinal)) {
        throw "Refusing to replace remote folder outside app data: $RemoteFolder"
    }

    Invoke-Adb shell "rm -rf '$RemoteFolder' && mkdir -p '$RemoteFolder' && unzip -oq '$remoteZip' -d '$RemoteFolder'"
    Invoke-Adb shell "chmod -R g+rwX '$RemoteFolder' 2>/dev/null || true"

    if (!$KeepStagedZips) {
        Invoke-Adb shell "rm -f '$remoteZip'"
    }
}

Expand-MissingPayloadsFromApk -Payloads $payloads

Invoke-Adb start-server
Invoke-Adb shell "mkdir -p '$remoteRoot' '$remoteStaging'"

$markers = New-Object System.Collections.Generic.List[string]

foreach ($payload in $payloads) {
    $zipPath = Join-Path $ZipRoot $payload.Zip
    if (!(Test-Path -LiteralPath $zipPath -PathType Leaf)) {
        throw "Required payload zip was not found after extraction: $zipPath"
    }

    $remoteZip = "$remoteStaging/$($payload.Zip)"
    $remoteDestination = $remoteRoot
    if ($payload.Destination -ne ".") {
        $remoteDestination = "$remoteRoot/$($payload.Destination)"
    }

    $sizeMb = [Math]::Round((Get-Item -LiteralPath $zipPath).Length / 1MB, 1)
    Write-Host "Deploying $($payload.Zip) ($sizeMb MB) -> $remoteDestination"
    Invoke-Adb push $zipPath $remoteZip
    Invoke-Adb shell "mkdir -p '$remoteDestination' && unzip -oq '$remoteZip' -d '$remoteDestination'"
    Invoke-Adb shell "chmod -R g+rwX '$remoteDestination' 2>/dev/null || true"

    if (!$KeepStagedZips) {
        Invoke-Adb shell "rm -f '$remoteZip'"
    }

    $marker = [IO.Path]::GetFileNameWithoutExtension($payload.Zip) + "_install"
    $markers.Add($marker)
}

Update-RemoteConfigMarkers -Markers $markers.ToArray()

Deploy-RuntimeFolder `
    -LocalFolder (Join-Path $RuntimeRoot "Expansions") `
    -RemoteFolder "$remoteRoot/Expansions"

if (!$SkipPictures) {
    Deploy-RuntimeFolder `
        -LocalFolder (Join-Path $RuntimeRoot "Picture\Art") `
        -RemoteFolder "$remoteRoot/Picture/Art"

    Deploy-RuntimeFolder `
        -LocalFolder (Join-Path $RuntimeRoot "Picture\Art2") `
        -RemoteFolder "$remoteRoot/Picture/Art2"

    Deploy-RuntimeFolder `
        -LocalFolder (Join-Path $RuntimeRoot "Picture\Art3D") `
        -RemoteFolder "$remoteRoot/Picture/Art3D"

    Deploy-RuntimeFolder `
        -LocalFolder (Join-Path $RuntimeRoot "Picture\Card") `
        -RemoteFolder "$remoteRoot/Picture/Card"

    Deploy-RuntimeFolder `
        -LocalFolder (Join-Path $RuntimeRoot "Picture\CardGenerated") `
        -RemoteFolder "$remoteRoot/Picture/CardGenerated"

    Deploy-RuntimeFolder `
        -LocalFolder (Join-Path $RuntimeRoot "Picture\Closeup") `
        -RemoteFolder "$remoteRoot/Picture/Closeup"
}

Deploy-RuntimeFolder `
    -LocalFolder (Join-Path $RuntimeRoot "Android\MasterDuel") `
    -RemoteFolder "$remoteRoot/Android/MasterDuel"

Deploy-RuntimeFolder `
    -LocalFolder (Join-Path $RuntimeRoot "Android\MonsterCutin") `
    -RemoteFolder "$remoteRoot/Android/MonsterCutin"

Deploy-RuntimeFolder `
    -LocalFolder (Join-Path $RuntimeRoot "Android\MonsterCutin2") `
    -RemoteFolder "$remoteRoot/Android/MonsterCutin2"

Write-Host "Quest runtime resource diagnostics:"
& $Adb shell "for d in Picture/Card Picture/CardGenerated Picture/Art Picture/Art2 Picture/Closeup Expansions Expansions/pics Expansions/art Android/MasterDuel Android/MonsterCutin Android/MonsterCutin2; do p='$remoteRoot/'`$d; if [ -d `"`$p`" ]; then c=`$(find `"`$p`" -maxdepth 1 -type f 2>/dev/null | wc -l); echo `"`$d files=`$c`"; else echo `"`$d missing`"; fi; done"

if (!$KeepStagedZips) {
    Invoke-Adb shell "rmdir '$remoteStaging' 2>/dev/null || true"
}

Invoke-Adb shell "chmod -R g+rwX '$remoteRoot'"

Write-Host "Quest runtime resources deployed to $remoteRoot"
