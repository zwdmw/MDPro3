# Quest Debug Automation

Use `Tools\Run-QuestDebugSession.ps1` to run a repeatable Quest diagnostics pass without wearing the headset for every check.

Default behavior:

- clears old debug flags
- enables Quest debug, verbose diagnostics, event logs, auto screenshots, auto duel view framing, and auto Solo
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
- enables lock-hand, no-check, and no-shuffle
- starts with 5 cards and draws 1 card per turn
- fixed opening hand is arranged to cover normal summon, hand special summon, spell activation, spell/place behavior, and trap set behavior
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
- `HostScreenshots\*.png`: screenshots captured through adb
- `AppQuestDebug\QuestDebug\*.png`: screenshots requested inside Unity by `QuestRuntimeDebugSettings.AutoCapture`
- `summary.txt`: session steps and output paths
