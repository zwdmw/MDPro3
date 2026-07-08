# Quest Debug Automation

Use `Tools\Run-QuestDebugSession.ps1` to run a repeatable Quest diagnostics pass without wearing the headset for every check.

Default behavior:

- clears old debug flags
- enables Quest debug, verbose diagnostics, event logs, auto screenshots, auto duel view framing, and auto Solo
- enables debug-only auto actions during auto Solo, so unattended screenshots can cover the first safe summon/set result
- launches `com.ygo.mdpro3.quest`
- captures `adb logcat`
- takes host-side Quest screenshots every 20 seconds
- pulls app-side `QuestDebug` screenshots created by Unity
- removes old generated `quest-debug-*.png` and `host-screen-*.png` from the device before the run, unless `-KeepRemoteDebugFiles` is used
- writes filtered important log lines
- clears debug flags at the end unless `-KeepFlags` is used

Default auto Solo duel:

- creates/refreshes `Deck\QuestDebugAuto.ydk`
- selects `QuestDebugAuto` only for the debug auto Solo launch
- enables lock-hand and no-shuffle, disables no-check
- starts with 5 cards and draws 1 card per turn
- fixed rock-paper-scissors makes the local player choose first turn for repeatable interaction tests
- fixed opening hand is the first 5 main-deck entries in `QuestDebugAuto.ydk`: `魔术师之杖`, `风魔女-冰铃`, `奥义之魔导书`, `黑魔导阵`, `永远之魂`
- opening hand coverage includes normal summon, hand special summon, spell activation, spell/place behavior, and trap set behavior
- auto actions wait for a stable `SelectIdleCmd`, then choose a conservative card action in this order: normal summon, special summon, set monster, set spell
- auto actions are gated to auto Solo debug runs; `autoactions` alone will not drive a normal duel
- automatically sends local player ready/start in the Solo room
- restores the user's previous `DeckInUse` after the duel starts or after an auto-start timeout

Common commands:

```powershell
powershell -ExecutionPolicy Bypass -File Tools\Run-QuestDebugSession.ps1
```

Install the latest development APK first, then run diagnostics:

```powershell
powershell -ExecutionPolicy Bypass -File Tools\Run-QuestDebugSession.ps1 -InstallApk
```

Run longer and keep flags for repeated manual relaunches:

```powershell
powershell -ExecutionPolicy Bypass -File Tools\Run-QuestDebugSession.ps1 -DurationSeconds 180 -KeepFlags
```

Keep old device-side debug screenshots:

```powershell
powershell -ExecutionPolicy Bypass -File Tools\Run-QuestDebugSession.ps1 -KeepRemoteDebugFiles
```

Disable auto Solo while still collecting logs and screenshots:

```powershell
powershell -ExecutionPolicy Bypass -File Tools\Run-QuestDebugSession.ps1 -NoAutoDuel
```

Disable only auto card actions while still entering auto Solo:

```powershell
powershell -ExecutionPolicy Bypass -File Tools\Run-QuestDebugSession.ps1 -NoAutoActions
```

Use a specific unattended screenshot angle:

```powershell
powershell -ExecutionPolicy Bypass -File Tools\Run-QuestDebugSession.ps1 -ViewPreset overview
```

Available view presets include `field`, `overview`, `hand`, `close`, `leftinfo`, `rightinfo`, `opponent`, and `menu`.

`-NoVerbose`, `-NoCapture`, and `-NoFrameView` remove the corresponding debug flag files. A neutral `settings.flag` is always pushed so debug mode can still be enabled without accidentally enabling verbose logs, screenshots, or forced framing.

Override the debug camera frame explicitly:

```powershell
powershell -ExecutionPolicy Bypass -File Tools\Run-QuestDebugSession.ps1 -ViewOffset "0,4,-18" -ViewLookAt "0,1.6,-1.5" -ViewScale 1.25
```

Output is written under:

```text
D:\game\MDPro3-Quest\DebugSessions\quest-debug-YYYYMMDD-HHMMSS
```

Important files:

- `logcat.txt`: full device log during the session
- `important-log-lines.txt`: filtered Quest/MDPro3/errors/warnings
- `important-app-log-lines.txt`: filtered lines for the launched app process when its PID is available
- `debug-report.md`: machine-generated session summary with health counts, key Quest/UI/duel-state lines, and screenshot counts
- `HostScreenshots\*.png`: screenshots captured through adb
- `AppQuestDebug\QuestDebug\*.png`: screenshots requested inside Unity by `QuestRuntimeDebugSettings.AutoCapture`
- `summary.txt`: session steps and output paths
