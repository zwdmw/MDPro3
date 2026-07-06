# MDPro3 Quest Port Notes

This port is staged so the duel engine is validated on Android before the VR table is built.

## Current Target

1. Build an Android ARM64 APK that can run on Quest as a flat app.
2. Verify `ocgcore`, `ygoserver`, `sqlite`, card data, scripts, decks, and solo duel startup.
3. Add OpenXR/Quest interaction after the Android runtime path is proven.

## Local Findings

- Unity editor version: `6000.0.28f1`.
- The local editor currently has only Windows Standalone support installed. Android Build Support is missing.
- `Assets/Plugins/YGO Classes` already contains Android ARM64 native libraries:
  - `libocgcore.so`
  - `libygoserver.so`
  - `libsqlite3.so`
- Android boot logic already extracts `*.zip` files from `StreamingAssets` into `Application.persistentDataPath`.
- `Program.root` becomes `Android/` on Android, so Android-compatible asset bundles must be supplied under an `Android` folder. Do not reuse `StandaloneWindows64` bundles on Quest.

## Commands

Pack the minimum runtime payload:

```powershell
D:\game\MDPro3-src\Tools\Pack-QuestRuntimeResources.ps1 -Force
```

Pack optional heavy resources:

```powershell
D:\game\MDPro3-src\Tools\Pack-QuestRuntimeResources.ps1 -Force -IncludePictures -IncludeSound -IncludeExpansions
```

Pack Android asset bundles when an Android bundle folder is available:

```powershell
D:\game\MDPro3-src\Tools\Pack-QuestRuntimeResources.ps1 -Force -AndroidAssetsSource D:\path\to\Android
```

Build the APK after installing Android Build Support for Unity `6000.0.28f1`:

```powershell
D:\game\MDPro3-src\Tools\Build-MDPro3Quest.ps1
```

The output APK path is:

```text
D:\game\MDPro3-Quest\MDPro3-Quest.apk
```

## Next VR Step

After the flat Android APK starts a solo duel on Quest, add OpenXR and a separate VR duel view. Keep `OcgCore` as the source of truth and replace only the presentation/input layer first.
