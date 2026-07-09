param(
    [Parameter(Mandatory = $true)]
    [string]$SessionRoot,
    [string]$FixtureName = "baseline-opening",
    [string[]]$RequiredPatterns = @(),
    [string[]]$RequiredEventKinds = @(),
    [string[]]$RequiredMoveKinds = @(),
    [string[]]$RequiredSummonKinds = @(),
    [int[]]$RequiredActionCardIds = @(),
    [string[]]$RequiredActions = @(),
    [int[]]$RequiredResponses = @(),
    [int[]]$RequiredFinalCardIds = @(),
    [string[]]$RequiredFinalLocations = @(),
    [string[]]$RequiredCardFlows = @(),
    [int]$MinimumFixtureEvents = 1,
    [int]$MinimumDuelStateSnapshots = 1,
    [int]$MinimumActionableSnapshots = 1,
    [int]$MinimumHostScreenshots = 1,
    [int]$MinimumAppScreenshots = 1,
    [string[]]$AllowedMissingBundlePatterns = @(),
    [switch]$FailOnRuntimeFallbackBundles,
    [string[]]$IgnoredGeneralErrorPatterns = @(
        "TcpHelper\.Join failed ip=127\.0\.0\.1, port=7911: System\.Net\.Sockets\.SocketException .*Connection refused",
        "VulkanLoader::Load(Instance|Device)Functions: Failed to load optional",
        "MemoryBrokerClient: Face: failed to remap FACE_TRACKER: .*missing app permission",
        "UnityGame: GameManager not available\.",
        "Failed to get apps: Cannot resolve destination host"
    ),
    [switch]$AllowGeneralErrors,
    [switch]$FailOnGeneralErrors,
    [switch]$NoFail
)

$ErrorActionPreference = "Stop"

if (!(Test-Path -LiteralPath $SessionRoot -PathType Container)) {
    throw "Quest debug session folder was not found: $SessionRoot"
}

$appImportantPath = Join-Path $SessionRoot "important-app-log-lines.txt"
$importantPath = Join-Path $SessionRoot "important-log-lines.txt"
$logPath = Join-Path $SessionRoot "logcat.txt"
$pidPath = Join-Path $SessionRoot "pidof.txt"
$reportPath = Join-Path $SessionRoot "fixture-validation-$FixtureName.md"
$jsonPath = Join-Path $SessionRoot "fixture-validation-$FixtureName.json"

if ((Test-Path -LiteralPath $appImportantPath -PathType Leaf) -and ((Get-Item -LiteralPath $appImportantPath).Length -gt 0)) {
    $sourcePath = $appImportantPath
}
elseif ((Test-Path -LiteralPath $importantPath -PathType Leaf) -and ((Get-Item -LiteralPath $importantPath).Length -gt 0)) {
    $sourcePath = $importantPath
}
elseif (Test-Path -LiteralPath $logPath -PathType Leaf) {
    $sourcePath = $logPath
}
else {
    throw "No Quest debug log file was found in: $SessionRoot"
}

$lines = @(Get-Content -LiteralPath $sourcePath -Encoding UTF8)
$text = $lines -join "`n"
$rawLogLines = @()
if (Test-Path -LiteralPath $logPath -PathType Leaf) {
    $rawLogLines = @(Get-Content -LiteralPath $logPath -Encoding UTF8)
}

$pidCandidates = @()
if (Test-Path -LiteralPath $pidPath -PathType Leaf) {
    $pidCandidates = @(
        (Get-Content -LiteralPath $pidPath -Encoding UTF8) -split "\s+" |
            Where-Object { $_ -match "^\d+$" } |
            Select-Object -Unique
    )
}

$rawAppLines = New-Object System.Collections.Generic.List[string]
if ($rawLogLines.Count -gt 0 -and $pidCandidates.Count -gt 0) {
    foreach ($line in $rawLogLines) {
        foreach ($appPidCandidate in $pidCandidates) {
            if ($line -match "^\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}\.\d+\s+$([regex]::Escape($appPidCandidate))\s+") {
                [void]$rawAppLines.Add($line)
                break
            }
        }
    }
}

$errorLines = $lines
$errorSourcePath = $sourcePath
if ($rawAppLines.Count -gt 0) {
    $errorLines = @($rawAppLines.ToArray())
    $errorSourcePath = "$logPath (app pid: $($pidCandidates -join ','))"
}
elseif ($rawLogLines.Count -gt 0) {
    $errorLines = $rawLogLines
    $errorSourcePath = $logPath
}

$errorText = $errorLines -join "`n"

function Count-Regex {
    param([string]$Pattern)
    return ([regex]::Matches($text, $Pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)).Count
}

function Count-ErrorRegex {
    param([string]$Pattern)
    return ([regex]::Matches($errorText, $Pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)).Count
}

function Test-Regex {
    param([string]$Pattern)
    return [regex]::IsMatch($text, $Pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
}

function Test-IgnoredGeneralErrorLine {
    param([string]$Line)

    foreach ($pattern in $IgnoredGeneralErrorPatterns) {
        if ([string]::IsNullOrWhiteSpace($pattern)) {
            continue
        }

        if ([regex]::IsMatch($Line, $pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
            return $true
        }
    }

    return $false
}

function Get-UnignoredMatchingLines {
    param(
        [string]$Pattern,
        [string[]]$SourceLines
    )

    $matches = New-Object System.Collections.Generic.List[string]
    foreach ($line in $SourceLines) {
        if (![regex]::IsMatch($line, $Pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
            continue
        }

        if (Test-IgnoredGeneralErrorLine $line) {
            continue
        }

        [void]$matches.Add($line)
    }

    return @($matches.ToArray())
}

function ConvertTo-FlowSpec {
    param([string]$Spec)

    $flow = [ordered]@{
        raw = $Spec
    }

    foreach ($part in ($Spec -split ";")) {
        if ([string]::IsNullOrWhiteSpace($part)) {
            continue
        }

        $pieces = $part -split "=", 2
        if ($pieces.Count -eq 1) {
            if (!$flow.Contains("card")) {
                $flow["card"] = $pieces[0].Trim()
            }
            continue
        }

        $key = $pieces[0].Trim().ToLowerInvariant()
        $value = $pieces[1].Trim()
        if (![string]::IsNullOrWhiteSpace($key)) {
            $flow[$key] = $value
        }
    }

    return $flow
}

function Get-FlowValue {
    param(
        [System.Collections.IDictionary]$Flow,
        [string[]]$Keys
    )

    foreach ($key in $Keys) {
        $normalized = $key.ToLowerInvariant()
        if ($Flow.Contains($normalized) -and ![string]::IsNullOrWhiteSpace([string]$Flow[$normalized])) {
            return [string]$Flow[$normalized]
        }
    }

    return $null
}

function Test-CardActionCandidate {
    param(
        [int]$CardId,
        [string]$Action,
        [string]$Response
    )

    foreach ($line in $lines) {
        if ($line -notmatch "actions=") {
            continue
        }

        $match = [regex]::Match($line, "\b$([regex]::Escape($CardId.ToString()))@[^\[]*\[([^\]]*)\]")
        if (!$match.Success) {
            continue
        }

        if ([string]::IsNullOrWhiteSpace($Action) -and [string]::IsNullOrWhiteSpace($Response)) {
            return $true
        }

        foreach ($part in ($match.Groups[1].Value -split ",")) {
            $button = $part.Trim()
            if ([string]::IsNullOrWhiteSpace($button)) {
                continue
            }

            $buttonMatch = [regex]::Match($button, "^([^=]+)(?:=(-?\d+))?$")
            if (!$buttonMatch.Success) {
                continue
            }

            $buttonAction = $buttonMatch.Groups[1].Value
            $buttonResponse = if ($buttonMatch.Groups[2].Success) { $buttonMatch.Groups[2].Value } else { $null }
            if (![string]::IsNullOrWhiteSpace($Action) -and ![string]::Equals($buttonAction, $Action, [StringComparison]::OrdinalIgnoreCase)) {
                continue
            }
            if (![string]::IsNullOrWhiteSpace($Response) -and $buttonResponse -ne $Response) {
                continue
            }

            return $true
        }
    }

    return $false
}

function Test-CardResponseSent {
    param(
        [int]$CardId,
        [string]$Response
    )

    foreach ($line in $lines) {
        if ($line -notmatch "Quest duel response sent:") {
            continue
        }
        if (![string]::IsNullOrWhiteSpace($Response) -and $line -notmatch "response=$([regex]::Escape($Response))(\b|,| )") {
            continue
        }
        if ($CardId -gt 0 -and $line -notmatch "(selected|actions)=[^\r\n]*\b$([regex]::Escape($CardId.ToString()))@") {
            continue
        }

        return $true
    }

    return $false
}

function Test-CardFixtureEvent {
    param(
        [int]$CardId,
        [string]$EventKind,
        [string]$MoveKind,
        [string]$SummonKind,
        [string]$FromLocation,
        [string]$ToLocation
    )

    foreach ($line in $lines) {
        if ($line -notmatch "Quest fixture event:") {
            continue
        }
        if ($CardId -gt 0 -and $line -notmatch "card=$([regex]::Escape($CardId.ToString()))@") {
            continue
        }
        if (![string]::IsNullOrWhiteSpace($EventKind) -and $line -notmatch "kind=$([regex]::Escape($EventKind))(\b|,)") {
            continue
        }
        if (![string]::IsNullOrWhiteSpace($MoveKind) -and $line -notmatch "move=$([regex]::Escape($MoveKind))(\b|,)") {
            continue
        }
        if (![string]::IsNullOrWhiteSpace($SummonKind) -and $line -notmatch "summon=$([regex]::Escape($SummonKind))(\b|,)") {
            continue
        }
        if (![string]::IsNullOrWhiteSpace($FromLocation) -and $line -notmatch "from=$([regex]::Escape($FromLocation))(,|$)") {
            continue
        }
        if (![string]::IsNullOrWhiteSpace($ToLocation) -and $line -notmatch "to=$([regex]::Escape($ToLocation))(,|$)") {
            continue
        }

        return $true
    }

    return $false
}

function Test-ExtraDeckCandidate {
    param([int]$CardId)

    foreach ($line in $lines) {
        if ($line -match "Quest extra deck cards:" -and $line -match "\b$([regex]::Escape($CardId.ToString())):") {
            return $true
        }
    }

    return $false
}

function Normalize-BundleKey {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return ""
    }

    $normalized = $Path.Trim().Replace('\', '/')
    if ($normalized.StartsWith("Android/", [StringComparison]::OrdinalIgnoreCase)) {
        $normalized = $normalized.Substring("Android/".Length)
    }

    return $normalized.TrimStart('/')
}

function Test-AllowedMissingBundle {
    param([string]$BundleKey)

    foreach ($pattern in $AllowedMissingBundlePatterns) {
        if ([string]::IsNullOrWhiteSpace($pattern)) {
            continue
        }

        if ([regex]::IsMatch($BundleKey, $pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
            return $true
        }
    }

    return $false
}

function Add-Failure {
    param([System.Collections.Generic.List[string]]$Failures, [string]$Message)
    [void]$Failures.Add($Message)
}

$failures = New-Object System.Collections.Generic.List[string]
$checks = New-Object System.Collections.Generic.List[object]

$defaultRequiredPatterns = @(
    "Quest debug settings:",
    "Quest debug auto solo setup:",
    "Room\.StocMessage_DuelStart",
    "Quest fixture event:",
    "Quest duel state changed:",
    "actions="
)

foreach ($pattern in $defaultRequiredPatterns + $RequiredPatterns) {
    $matched = Test-Regex $pattern
    [void]$checks.Add([ordered]@{
        kind = "pattern"
        value = $pattern
        passed = $matched
    })
    if (!$matched) {
        Add-Failure $failures "Missing required log pattern: $pattern"
    }
}

$criticalPatterns = [ordered]@{
    NullReferenceException = "NullReferenceException"
    InvalidKeyException = "InvalidKeyException"
    IndexOutOfRangeException = "IndexOutOfRangeException"
    ArgumentOutOfRangeException = "ArgumentOutOfRangeException"
    AddressablesError = "Addressables.*(InvalidKey|Exception|Error)|InvalidKeyException"
    ResourceLoadError = "(AddressablesSafe|Items|TextureLoader|Quest resource|Pic File|Voice File).*(missing|failed|error|not found)|failed to load (asset|resource|card|texture|sprite|material)|load.*failed.*(asset|resource|card|texture|sprite|material)"
    MissingLua = "missing lua|lua script missing|script file missing|missing script file|script missing|c\d+\.lua.*missing|missing.*c\d+\.lua"
    OcgCoreScriptError = "ocgcore.*error|script load.*error|lua.*error"
}

$criticalCounts = [ordered]@{}
foreach ($item in $criticalPatterns.GetEnumerator()) {
    $count = Count-ErrorRegex $item.Value
    $criticalCounts[$item.Key] = $count
    if ($count -gt 0) {
        Add-Failure $failures "Critical log pattern matched: $($item.Key) count=$count"
    }
}

$generalExceptionLines = @()
$generalErrorLines = @()
if (!$AllowGeneralErrors) {
    $generalExceptionLines = @(Get-UnignoredMatchingLines "\b[A-Za-z0-9_]*Exception\b" $errorLines)
    $generalErrorLines = @(Get-UnignoredMatchingLines "\b(Unity\s+)?Error\b| E Unity\s+:" $errorLines)
    if ($generalExceptionLines.Count -gt 0) {
        Add-Failure $failures "General exception lines matched: count=$($generalExceptionLines.Count)"
    }
    if ($FailOnGeneralErrors -and $generalErrorLines.Count -gt 0) {
        Add-Failure $failures "General error lines matched: count=$($generalErrorLines.Count)"
    }
}

$missingBundleKeys = New-Object System.Collections.Generic.HashSet[string] ([StringComparer]::OrdinalIgnoreCase)
$runtimeFallbackBundleKeys = New-Object System.Collections.Generic.HashSet[string] ([StringComparer]::OrdinalIgnoreCase)
foreach ($line in $errorLines) {
    $missingMatch = [regex]::Match($line, "AssetBundle file missing:\s+(?<path>.+?)\s+->", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if ($missingMatch.Success) {
        $key = Normalize-BundleKey $missingMatch.Groups["path"].Value
        if (![string]::IsNullOrWhiteSpace($key)) {
            [void]$missingBundleKeys.Add($key)
        }
    }

    $fallbackMatch = [regex]::Match($line, "AssetBundle runtime fallback created:\s+(?<path>.+)$", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if ($fallbackMatch.Success) {
        $key = Normalize-BundleKey $fallbackMatch.Groups["path"].Value
        if (![string]::IsNullOrWhiteSpace($key)) {
            [void]$runtimeFallbackBundleKeys.Add($key)
        }
    }
}

$unresolvedMissingBundles = New-Object System.Collections.Generic.List[string]
foreach ($key in $missingBundleKeys) {
    if ($runtimeFallbackBundleKeys.Contains($key)) {
        continue
    }
    if (Test-AllowedMissingBundle $key) {
        continue
    }

    [void]$unresolvedMissingBundles.Add($key)
}

if ($unresolvedMissingBundles.Count -gt 0) {
    Add-Failure $failures "Unresolved missing AssetBundle files: count=$($unresolvedMissingBundles.Count)"
}

if ($FailOnRuntimeFallbackBundles -and $runtimeFallbackBundleKeys.Count -gt 0) {
    Add-Failure $failures "Runtime AssetBundle fallbacks were used: count=$($runtimeFallbackBundleKeys.Count)"
}

$fixtureEventCount = Count-Regex "Quest fixture event:"
$duelStateSnapshotCount = Count-Regex "Quest duel state changed:"
$actionableSnapshotCount = Count-Regex "Quest duel state (changed|unchanged):.*actions=(?!none)"

if ($fixtureEventCount -lt $MinimumFixtureEvents) {
    Add-Failure $failures "Fixture event count too low: $fixtureEventCount < $MinimumFixtureEvents"
}

if ($duelStateSnapshotCount -lt $MinimumDuelStateSnapshots) {
    Add-Failure $failures "Duel-state snapshot count too low: $duelStateSnapshotCount < $MinimumDuelStateSnapshots"
}

if ($actionableSnapshotCount -lt $MinimumActionableSnapshots) {
    Add-Failure $failures "Actionable snapshot count too low: $actionableSnapshotCount < $MinimumActionableSnapshots"
}

foreach ($eventKind in $RequiredEventKinds) {
    if ([string]::IsNullOrWhiteSpace($eventKind)) {
        continue
    }

    $pattern = "Quest fixture event:.*kind=$([regex]::Escape($eventKind))"
    $matched = Test-Regex $pattern
    [void]$checks.Add([ordered]@{
        kind = "eventKind"
        value = $eventKind
        passed = $matched
    })
    if (!$matched) {
        Add-Failure $failures "Missing fixture event kind: $eventKind"
    }
}

foreach ($moveKind in $RequiredMoveKinds) {
    if ([string]::IsNullOrWhiteSpace($moveKind)) {
        continue
    }

    $pattern = "Quest fixture event:.*move=$([regex]::Escape($moveKind))"
    $matched = Test-Regex $pattern
    [void]$checks.Add([ordered]@{
        kind = "moveKind"
        value = $moveKind
        passed = $matched
    })
    if (!$matched) {
        Add-Failure $failures "Missing fixture move kind: $moveKind"
    }
}

foreach ($summonKind in $RequiredSummonKinds) {
    if ([string]::IsNullOrWhiteSpace($summonKind)) {
        continue
    }

    $pattern = "Quest fixture event:.*kind=CardSummoned.*summon=$([regex]::Escape($summonKind))"
    $matched = Test-Regex $pattern
    [void]$checks.Add([ordered]@{
        kind = "summonKind"
        value = $summonKind
        passed = $matched
    })
    if (!$matched) {
        Add-Failure $failures "Missing summon fixture event: $summonKind"
    }
}

foreach ($cardId in $RequiredActionCardIds) {
    if ($cardId -le 0) {
        continue
    }

    $pattern = "actions=[^\r\n]*\b$cardId@"
    $matched = Test-Regex $pattern
    [void]$checks.Add([ordered]@{
        kind = "actionCard"
        value = $cardId
        passed = $matched
    })
    if (!$matched) {
        Add-Failure $failures "Missing actionable card snapshot for id: $cardId"
    }
}

foreach ($action in $RequiredActions) {
    if ([string]::IsNullOrWhiteSpace($action)) {
        continue
    }

    $pattern = "actions=[^\r\n]*\b$([regex]::Escape($action))(=|,|\])"
    $matched = (Test-Regex $pattern) -or (Test-Regex "Quest debug auto action executed:[^\r\n]*type=$([regex]::Escape($action))") -or (Test-Regex "Quest duel action clicked:[^\r\n]*type=$([regex]::Escape($action))")
    [void]$checks.Add([ordered]@{
        kind = "actionType"
        value = $action
        passed = $matched
    })
    if (!$matched) {
        Add-Failure $failures "Missing required action type: $action"
    }
}

foreach ($response in $RequiredResponses) {
    $pattern = "(Quest duel response sent:[^\r\n]*response=$response\b|actions=[^\r\n]*=$response\b|Quest debug auto action executed:[^\r\n]*response=$response\b|Quest duel action clicked:[^\r\n]*response=$response\b)"
    $matched = Test-Regex $pattern
    [void]$checks.Add([ordered]@{
        kind = "response"
        value = $response
        passed = $matched
    })
    if (!$matched) {
        Add-Failure $failures "Missing required response: $response"
    }
}

foreach ($cardId in $RequiredFinalCardIds) {
    if ($cardId -le 0) {
        continue
    }

    $pattern = "Quest fixture event:[^\r\n]*card=$cardId@"
    $matched = Test-Regex $pattern
    [void]$checks.Add([ordered]@{
        kind = "finalCardEvent"
        value = $cardId
        passed = $matched
    })
    if (!$matched) {
        Add-Failure $failures "Missing required final card event for id: $cardId"
    }
}

foreach ($location in $RequiredFinalLocations) {
    if ([string]::IsNullOrWhiteSpace($location)) {
        continue
    }

    $pattern = "(Quest fixture event:[^\r\n]* to=$([regex]::Escape($location))|Quest fixture event:[^\r\n]*card=[0-9]+@$([regex]::Escape($location)))"
    $matched = Test-Regex $pattern
    [void]$checks.Add([ordered]@{
        kind = "finalLocation"
        value = $location
        passed = $matched
    })
    if (!$matched) {
        Add-Failure $failures "Missing required final location evidence: $location"
    }
}

foreach ($flowSpec in $RequiredCardFlows) {
    if ([string]::IsNullOrWhiteSpace($flowSpec)) {
        continue
    }

    $flow = ConvertTo-FlowSpec $flowSpec
    $cardText = Get-FlowValue $flow @("card", "id", "cardid")
    $cardId = 0
    if ([string]::IsNullOrWhiteSpace($cardText) -or ![int]::TryParse($cardText, [ref]$cardId) -or $cardId -le 0) {
        Add-Failure $failures "Invalid required card flow, missing positive card id: $flowSpec"
        continue
    }

    $label = Get-FlowValue $flow @("label", "name")
    if ([string]::IsNullOrWhiteSpace($label)) {
        $label = "card-$cardId"
    }

    $action = Get-FlowValue $flow @("action", "button")
    $response = Get-FlowValue $flow @("response", "resp")
    $eventKind = Get-FlowValue $flow @("event", "kind")
    $moveKind = Get-FlowValue $flow @("move")
    $summonKind = Get-FlowValue $flow @("summon", "summonkind")
    $fromLocation = Get-FlowValue $flow @("from", "fromlocation")
    $toLocation = Get-FlowValue $flow @("to", "location", "finallocation")
    $source = Get-FlowValue $flow @("source", "zone")
    $requireCandidateText = Get-FlowValue $flow @("candidate", "requirecandidate")
    $requireResponseText = Get-FlowValue $flow @("sent", "requiresentsresponse", "requireresponse")

    $requiresCandidate = (![string]::IsNullOrWhiteSpace($action) -or ![string]::IsNullOrWhiteSpace($response))
    if (![string]::IsNullOrWhiteSpace($requireCandidateText)) {
        $requiresCandidate = [string]::Equals($requireCandidateText, "true", [StringComparison]::OrdinalIgnoreCase)
    }

    if ($requiresCandidate) {
        $matched = Test-CardActionCandidate $cardId $action $response
        [void]$checks.Add([ordered]@{
            kind = "cardFlowCandidate"
            value = $flowSpec
            passed = $matched
        })
        if (!$matched) {
            Add-Failure $failures "Card flow missing actionable candidate: $label ($flowSpec)"
        }
    }

    if (![string]::IsNullOrWhiteSpace($source) -and [string]::Equals($source, "extra", [StringComparison]::OrdinalIgnoreCase)) {
        $matched = Test-ExtraDeckCandidate $cardId
        [void]$checks.Add([ordered]@{
            kind = "cardFlowExtraCandidate"
            value = $flowSpec
            passed = $matched
        })
        if (!$matched) {
            Add-Failure $failures "Card flow missing extra deck candidate: $label ($flowSpec)"
        }
    }

    $requiresResponseSent = ![string]::IsNullOrWhiteSpace($response)
    if (![string]::IsNullOrWhiteSpace($requireResponseText)) {
        $requiresResponseSent = [string]::Equals($requireResponseText, "true", [StringComparison]::OrdinalIgnoreCase)
    }

    if ($requiresResponseSent) {
        $matched = Test-CardResponseSent $cardId $response
        [void]$checks.Add([ordered]@{
            kind = "cardFlowResponseSent"
            value = $flowSpec
            passed = $matched
        })
        if (!$matched) {
            Add-Failure $failures "Card flow missing sent response: $label ($flowSpec)"
        }
    }

    if (![string]::IsNullOrWhiteSpace($eventKind) -or ![string]::IsNullOrWhiteSpace($moveKind) -or ![string]::IsNullOrWhiteSpace($summonKind) -or ![string]::IsNullOrWhiteSpace($fromLocation) -or ![string]::IsNullOrWhiteSpace($toLocation)) {
        $matched = Test-CardFixtureEvent $cardId $eventKind $moveKind $summonKind $fromLocation $toLocation
        [void]$checks.Add([ordered]@{
            kind = "cardFlowFixtureEvent"
            value = $flowSpec
            passed = $matched
        })
        if (!$matched) {
            Add-Failure $failures "Card flow missing matching fixture event: $label ($flowSpec)"
        }
    }
}

$hostScreenshots = @(Get-ChildItem -LiteralPath (Join-Path $SessionRoot "HostScreenshots") -Filter "*.png" -ErrorAction SilentlyContinue)
$appScreenshots = @(Get-ChildItem -LiteralPath (Join-Path $SessionRoot "AppQuestDebug") -Recurse -Filter "*.png" -ErrorAction SilentlyContinue)

if ($hostScreenshots.Count -lt $MinimumHostScreenshots) {
    Add-Failure $failures "Host screenshot count too low: $($hostScreenshots.Count) < $MinimumHostScreenshots"
}

if ($appScreenshots.Count -lt $MinimumAppScreenshots) {
    Add-Failure $failures "App screenshot count too low: $($appScreenshots.Count) < $MinimumAppScreenshots"
}

$result = [ordered]@{
    fixtureName = $FixtureName
    sessionRoot = $SessionRoot
    sourcePath = $sourcePath
    errorSourcePath = $errorSourcePath
    passed = ($failures.Count -eq 0)
    failureCount = $failures.Count
    failures = @($failures.ToArray())
    fixtureEventCount = $fixtureEventCount
    duelStateSnapshotCount = $duelStateSnapshotCount
    actionableSnapshotCount = $actionableSnapshotCount
    rawAppLogLineCount = $errorLines.Count
    generalExceptionCount = $generalExceptionLines.Count
    generalErrorCount = $generalErrorLines.Count
    missingBundleCount = $missingBundleKeys.Count
    runtimeFallbackBundleCount = $runtimeFallbackBundleKeys.Count
    unresolvedMissingBundleCount = $unresolvedMissingBundles.Count
    unresolvedMissingBundles = @($unresolvedMissingBundles.ToArray())
    hostScreenshotCount = $hostScreenshots.Count
    appScreenshotCount = $appScreenshots.Count
    minimumHostScreenshots = $MinimumHostScreenshots
    minimumAppScreenshots = $MinimumAppScreenshots
    failOnRuntimeFallbackBundles = [bool]$FailOnRuntimeFallbackBundles
    criticalCounts = $criticalCounts
    ignoredGeneralErrorPatterns = $IgnoredGeneralErrorPatterns
    allowGeneralErrors = [bool]$AllowGeneralErrors
    failOnGeneralErrors = [bool]$FailOnGeneralErrors
    checks = @($checks.ToArray())
}

$result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonPath -Encoding UTF8

$report = New-Object System.Collections.Generic.List[string]
[void]$report.Add("# Quest Fixture Validation")
[void]$report.Add("")
[void]$report.Add("- Fixture: ``$FixtureName``")
[void]$report.Add("- Session: ``$SessionRoot``")
[void]$report.Add("- Source log: ``$sourcePath``")
[void]$report.Add("- Error scan log: ``$errorSourcePath``")
[void]$report.Add("- Passed: ``$($result.passed)``")
[void]$report.Add("- Failures: $($failures.Count)")
[void]$report.Add("")
[void]$report.Add("## Counts")
[void]$report.Add("")
[void]$report.Add("- Fixture events: $fixtureEventCount")
[void]$report.Add("- Duel-state snapshots: $duelStateSnapshotCount")
[void]$report.Add("- Actionable snapshots: $actionableSnapshotCount")
[void]$report.Add("- Raw app/error log lines: $($errorLines.Count)")
[void]$report.Add("- General exceptions: $($generalExceptionLines.Count)")
[void]$report.Add("- General errors: $($generalErrorLines.Count) / fail on general errors: $([bool]$FailOnGeneralErrors)")
[void]$report.Add("- Missing AssetBundles: $($missingBundleKeys.Count)")
[void]$report.Add("- Runtime AssetBundle fallbacks: $($runtimeFallbackBundleKeys.Count) / fail on fallbacks: $([bool]$FailOnRuntimeFallbackBundles)")
[void]$report.Add("- Unresolved missing AssetBundles: $($unresolvedMissingBundles.Count)")
[void]$report.Add("- Host screenshots: $($hostScreenshots.Count) / minimum $MinimumHostScreenshots")
[void]$report.Add("- App screenshots: $($appScreenshots.Count) / minimum $MinimumAppScreenshots")
[void]$report.Add("")
[void]$report.Add("## Critical Counts")
[void]$report.Add("")
foreach ($item in $criticalCounts.GetEnumerator()) {
    [void]$report.Add("- $($item.Key): $($item.Value)")
}
[void]$report.Add("- Allow general errors: $([bool]$AllowGeneralErrors)")
if ($IgnoredGeneralErrorPatterns.Count -gt 0) {
    [void]$report.Add("- Ignored general error patterns: $($IgnoredGeneralErrorPatterns.Count)")
}

if ($failures.Count -gt 0) {
    [void]$report.Add("")
    [void]$report.Add("## Failures")
    [void]$report.Add("")
    foreach ($failure in $failures) {
        [void]$report.Add("- ``$failure``")
    }
}

if ($unresolvedMissingBundles.Count -gt 0) {
    [void]$report.Add("")
    [void]$report.Add("## Unresolved Missing AssetBundles")
    [void]$report.Add("")
    foreach ($bundle in $unresolvedMissingBundles) {
        [void]$report.Add("- ``$bundle``")
    }
}

[void]$report.Add("")
[void]$report.Add("## Machine Report")
[void]$report.Add("")
[void]$report.Add("- JSON: ``$jsonPath``")

Set-Content -LiteralPath $reportPath -Value $report -Encoding UTF8
Write-Host "Quest fixture validation report: $reportPath"
Write-Host "Quest fixture validation JSON: $jsonPath"

if ($failures.Count -gt 0 -and !$NoFail) {
    throw "Quest fixture validation failed: $FixtureName"
}
