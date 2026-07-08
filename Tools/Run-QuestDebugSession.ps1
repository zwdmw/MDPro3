param(
    [string]$AdbExe = "D:\Unity\AndroidSDK\platform-tools\adb.exe",
    [string]$PackageName = "com.ygo.mdpro3.quest",
    [string]$OutputRoot = "D:\game\MDPro3-Quest\DebugSessions",
    [string]$ApkPath = "D:\game\MDPro3-Quest\MDPro3-Quest-Development.apk",
    [int]$DurationSeconds = 180,
    [int]$ScreenshotIntervalSeconds = 20,
    [string]$ViewPreset = "field",
    [string]$ViewOffset = "",
    [string]$ViewLookAt = "",
    [Nullable[double]]$ViewYaw = $null,
    [Nullable[double]]$ViewScale = $null,
    [switch]$InstallApk,
    [switch]$NoAutoDuel,
    [switch]$NoCapture,
    [switch]$NoVerbose,
    [switch]$NoFrameView,
    [switch]$NoLaunch,
    [switch]$KeepFlags,
    [switch]$KeepRemoteDebugFiles
)

$ErrorActionPreference = "Stop"

if (!(Test-Path -LiteralPath $AdbExe -PathType Leaf)) {
    throw "adb was not found: $AdbExe"
}

if ($InstallApk -and !(Test-Path -LiteralPath $ApkPath -PathType Leaf)) {
    throw "APK was not found: $ApkPath"
}

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$sessionRoot = Join-Path $OutputRoot "quest-debug-$stamp"
$hostFlagsRoot = Join-Path $sessionRoot "Flags"
$hostScreensRoot = Join-Path $sessionRoot "HostScreenshots"
$pulledDebugRoot = Join-Path $sessionRoot "AppQuestDebug"
New-Item -ItemType Directory -Force -Path $sessionRoot, $hostFlagsRoot, $hostScreensRoot, $pulledDebugRoot | Out-Null

$remoteFilesRoot = "/sdcard/Android/data/$PackageName/files"
$remoteDebugRoot = "$remoteFilesRoot/QuestDebug"
$logcatPath = Join-Path $sessionRoot "logcat.txt"
$logcatErrPath = Join-Path $sessionRoot "logcat.err.txt"
$summaryPath = Join-Path $sessionRoot "summary.txt"
$appPid = $null

function Invoke-Adb {
    param(
        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]]$AdbArgs
    )

    & $AdbExe @AdbArgs
    if ($LASTEXITCODE -ne 0) {
        throw "adb failed with exit code ${LASTEXITCODE}: $($AdbArgs -join ' ')"
    }
}

function Write-SessionLine {
    param([string]$Text)
    $line = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') $Text"
    Write-Host $line
    Add-Content -LiteralPath $summaryPath -Value $line -Encoding UTF8
}

function Set-QuestDebugFlag {
    param(
        [string]$Name,
        [string]$Content
    )

    $localPath = Join-Path $hostFlagsRoot $Name
    Set-Content -LiteralPath $localPath -Value $Content -Encoding ASCII
    Invoke-Adb push $localPath "$remoteDebugRoot/$Name" | Out-Null
}

function Clear-QuestDebugFlags {
    Invoke-Adb shell "rm -f $remoteDebugRoot/settings.flag $remoteDebugRoot/autoduel.flag $remoteDebugRoot/capture.flag $remoteDebugRoot/verbose.flag $remoteDebugRoot/frameview.flag $remoteFilesRoot/Data/quest_debug.flag $remoteFilesRoot/Data/quest_auto_duel.flag $remoteFilesRoot/Data/quest_auto_capture.flag" | Out-Null
}

function Save-QuestScreenshot {
    param([int]$Index)

    $shotStamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $remoteShot = "$remoteDebugRoot/host-screen-$shotStamp-$Index.png"
    $localShot = Join-Path $hostScreensRoot "host-screen-$shotStamp-$Index.png"
    Invoke-Adb shell "screencap -p $remoteShot" | Out-Null
    Invoke-Adb pull $remoteShot $localShot | Out-Null
    Write-SessionLine "Host screenshot pulled: $localShot"
}

function Get-QuestAppPid {
    try {
        $pidText = (& $AdbExe shell "pidof $PackageName").Trim()
        if ([string]::IsNullOrWhiteSpace($pidText)) {
            return $null
        }

        return ($pidText -split "\s+" | Select-Object -First 1)
    }
    catch {
        return $null
    }
}

Write-SessionLine "Quest debug session started."
Write-SessionLine "Output: $sessionRoot"

$devices = & $AdbExe devices
Add-Content -LiteralPath (Join-Path $sessionRoot "adb-devices.txt") -Value $devices -Encoding UTF8
if (($devices | Select-String -Pattern "`tdevice" | Measure-Object).Count -lt 1) {
    throw "No adb device is online. Check Quest USB connection and authorization."
}

if ($InstallApk) {
    Write-SessionLine "Installing APK: $ApkPath"
    Invoke-Adb install -r -d $ApkPath | Out-Null
}

Invoke-Adb shell "mkdir -p $remoteDebugRoot $remoteFilesRoot/Data" | Out-Null
Clear-QuestDebugFlags
if (!$KeepRemoteDebugFiles) {
    Invoke-Adb shell "rm -f $remoteDebugRoot/quest-debug-*.png $remoteDebugRoot/host-screen-*.png" | Out-Null
    Write-SessionLine "Old Quest debug screenshots cleared."
}

$flagTokens = @("debug", "events")
if (!$NoVerbose) { $flagTokens += "verbose" }
if (!$NoCapture) { $flagTokens += "capture" }
if (!$NoFrameView) { $flagTokens += "frameview" }
if (!$NoAutoDuel) { $flagTokens += "autoduel" }
if (!$NoFrameView -and ![string]::IsNullOrWhiteSpace($ViewPreset)) { $flagTokens += "view=$ViewPreset" }
if (!$NoFrameView -and ![string]::IsNullOrWhiteSpace($ViewOffset)) { $flagTokens += "offset=$ViewOffset" }
if (!$NoFrameView -and ![string]::IsNullOrWhiteSpace($ViewLookAt)) { $flagTokens += "lookat=$ViewLookAt" }
if (!$NoFrameView -and $null -ne $ViewYaw) { $flagTokens += ("yaw=" + $ViewYaw.Value.ToString([Globalization.CultureInfo]::InvariantCulture)) }
if (!$NoFrameView -and $null -ne $ViewScale) { $flagTokens += ("scale=" + $ViewScale.Value.ToString([Globalization.CultureInfo]::InvariantCulture)) }
$flagContent = ($flagTokens -join " ")

Set-QuestDebugFlag -Name "settings.flag" -Content $flagContent
if (!$NoVerbose) { Set-QuestDebugFlag -Name "verbose.flag" -Content $flagContent }
if (!$NoAutoDuel) { Set-QuestDebugFlag -Name "autoduel.flag" -Content $flagContent }
if (!$NoCapture) { Set-QuestDebugFlag -Name "capture.flag" -Content $flagContent }
if (!$NoFrameView) { Set-QuestDebugFlag -Name "frameview.flag" -Content $flagContent }
Write-SessionLine "Debug flags set: $flagContent"

Invoke-Adb logcat -c
$logcatProcess = Start-Process `
    -FilePath $AdbExe `
    -ArgumentList @("logcat", "-v", "threadtime") `
    -RedirectStandardOutput $logcatPath `
    -RedirectStandardError $logcatErrPath `
    -WindowStyle Hidden `
    -PassThru
Write-SessionLine "logcat capture started: $logcatPath"

try {
    if (!$NoLaunch) {
        Write-SessionLine "Launching package: $PackageName"
        Invoke-Adb shell "am force-stop $PackageName" | Out-Null
        Start-Sleep -Seconds 1
        Invoke-Adb shell "monkey -p $PackageName -c android.intent.category.LAUNCHER 1" | Out-Null

        $pidDeadline = (Get-Date).AddSeconds(45)
        while ((Get-Date) -lt $pidDeadline -and [string]::IsNullOrWhiteSpace($appPid)) {
            $appPid = Get-QuestAppPid
            if ([string]::IsNullOrWhiteSpace($appPid)) {
                Start-Sleep -Seconds 1
            }
        }

        if (![string]::IsNullOrWhiteSpace($appPid)) {
            Write-SessionLine "App PID detected: $appPid"
        }
        else {
            Write-SessionLine "App PID was not detected during startup wait."
        }
    }

    $duration = [Math]::Max(5, $DurationSeconds)
    $shotInterval = [Math]::Max(5, $ScreenshotIntervalSeconds)
    $deadline = (Get-Date).AddSeconds($duration)
    $nextShot = (Get-Date).AddSeconds(5)
    $shotIndex = 1

    while ((Get-Date) -lt $deadline) {
        if (!$NoCapture -and (Get-Date) -ge $nextShot) {
            try {
                Save-QuestScreenshot -Index $shotIndex
            }
            catch {
                Write-SessionLine "Host screenshot failed: $($_.Exception.Message)"
            }

            $shotIndex += 1
            $nextShot = (Get-Date).AddSeconds($shotInterval)
        }

        Start-Sleep -Milliseconds 500
    }
}
finally {
    if ($logcatProcess -and !$logcatProcess.HasExited) {
        Stop-Process -Id $logcatProcess.Id -Force
        Start-Sleep -Milliseconds 500
    }

    if (!$KeepFlags) {
        try {
            Clear-QuestDebugFlags
            Write-SessionLine "Debug flags cleared."
        }
        catch {
            Write-SessionLine "Debug flag cleanup failed: $($_.Exception.Message)"
        }
    }
}

try {
    Write-SessionLine "Pulling app QuestDebug directory."
    Invoke-Adb pull $remoteDebugRoot $pulledDebugRoot | Out-Null
}
catch {
    Write-SessionLine "Pull QuestDebug failed: $($_.Exception.Message)"
}

try {
    & $AdbExe shell "dumpsys package $PackageName" > (Join-Path $sessionRoot "package.txt")
    & $AdbExe shell "ls -la $remoteDebugRoot" > (Join-Path $sessionRoot "remote-questdebug-ls.txt")
    & $AdbExe shell "pidof $PackageName" > (Join-Path $sessionRoot "pidof.txt")
    if ([string]::IsNullOrWhiteSpace($appPid)) {
        $appPid = Get-QuestAppPid
    }
}
catch {
    Write-SessionLine "Extra adb diagnostics failed: $($_.Exception.Message)"
}

if (Test-Path -LiteralPath $logcatPath -PathType Leaf) {
    $importantPath = Join-Path $sessionRoot "important-log-lines.txt"
    Select-String -Path $logcatPath -Pattern @(
        "Quest debug",
        "Quest XR",
        "Quest native duel",
        "MDPro3 resource check",
        "NullReferenceException",
        "InvalidKeyException",
        "Exception",
        "Error",
        "Warning",
        "no card action",
        "auto solo",
        "auto frame",
        "auto room",
        "DuelStart",
        "deck restored"
    ) -CaseSensitive:$false | ForEach-Object { $_.Line } | Set-Content -LiteralPath $importantPath -Encoding UTF8
    Write-SessionLine "Important log lines: $importantPath"

    if (![string]::IsNullOrWhiteSpace($appPid)) {
        $importantAppPath = Join-Path $sessionRoot "important-app-log-lines.txt"
        $pidPattern = "\s+$([regex]::Escape($appPid))\s+"
        Get-Content -LiteralPath $logcatPath |
            Where-Object { $_ -match $pidPattern } |
            Select-String -Pattern @(
                "Quest debug",
                "Quest XR",
                "Quest native duel",
                "MDPro3 resource check",
                "NullReferenceException",
                "InvalidKeyException",
                "Exception",
                "Error",
                "Warning",
                "no card action",
                "auto solo",
                "auto frame",
                "auto room",
                "DuelStart",
                "deck restored"
            ) -CaseSensitive:$false |
            ForEach-Object { $_.Line } |
            Set-Content -LiteralPath $importantAppPath -Encoding UTF8
        Write-SessionLine "Important app log lines: $importantAppPath"
    }
}

Write-SessionLine "Quest debug session finished."
Write-Host ""
Write-Host "Session output:"
Write-Host $sessionRoot
