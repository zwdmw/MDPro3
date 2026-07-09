# Quest Immersive Duel Experience Execution Plan

This document is the working plan for turning the current Quest duel mode into a stable, readable, immersive YGO-style duel experience. It is intentionally execution-oriented: each phase has concrete code areas, verification gates, and Git checkpoint rules.

## Product Target

The Quest duel frontend should feel like a virtual YGO duel table:

- The duel field, cards, decks, grave/banish piles, hand, monster standees, LP, phase controls, and logs exist in fixed world space.
- MDPro3 remains the rules engine, card database/resource provider, and duel backend.
- Quest native world-space UI is the only primary duel frontend during VR duel play.
- Interactions happen on the field whenever possible: hover to inspect, click a card to act, point at target cards directly.
- Panels are reserved for persistent context, confirmations, and text-heavy situations; they should not jump around or fill the duel space.

## Non-Negotiable Requirements

- Do not regress normal duel rules. Fusion, Synchro, Xyz, Link, Pendulum, Ritual, normal summon, special summon, chain, attack target selection, and card/effect selection must remain supported.
- Runtime resources must be loaded from the correct Quest-visible locations. Database/script conflicts must keep the current priority rule: newer expansion rows override older duplicates.
- Card art is still card art. Monster standees/closeups are overlays for field presence and must not replace full card faces.
- The user must be able to test without repeatedly operating the headset: debug mode should start a known duel state, position the camera/world, and emit useful logs.
- Every stable step gets a Git checkpoint before risky work continues.

## Git And Release Discipline

1. Start each work block from a clean or understood worktree:
   - `git status --short --branch`
   - `git log --oneline -n 5`
2. Keep commits focused:
   - Docs/checklist commit.
   - Automation/resource validation commit.
   - Interaction layer commit.
   - Presentation/visual polish commit.
3. Before every commit:
   - `git diff --check`
   - `D:\game\MDPro3-src\Tools\CompileCheck-MDPro3Quest.ps1`
4. Before installing to Quest:
   - `D:\game\MDPro3-src\Tools\Build-MDPro3Quest.ps1`
   - `adb install -r D:\game\MDPro3-Quest\MDPro3-Quest.apk`
   - Sync runtime resources when expansion scripts/cards changed.
5. Push stable checkpoints to:
   - `fork quest-immersive-cleanup-msaa4-20260707`

## Phase 1: Rules And Resource Foundation

Goal: prove the duel backend and resources are correct before changing more UI. A script-load pass is necessary but not sufficient: each supported summon/selection path must actually reach a candidate, send a response, and produce the expected duel event.

### Current Implementation Status

- Added `Tools/Test-QuestExpansionResources.ps1`.
  - It follows the same expansion CDB priority model as `ZipHelper.GetExpansionDatabaseFiles()`.
  - It reports target-card duplicate rows, the final winning database, loose/zip Lua presence, and ocgcore script-load status.
  - Latest local run: `Logs/quest-expansion-resource-check-20260709-103830.md`.
  - Current target result: `100083`, `99993`, `100352`, and `100047` all resolve to `IFZCG2.cdb`, have loose and zipped scripts, and pass ocgcore script-load checks.
- Updated `Tools/Run-QuestDebugSession.ps1` so generated reports include Quest extra deck/action/selection diagnostics such as:
  - `Quest location browser opened`
  - `Quest extra deck cards`
  - `Quest duel state changed`
  - `Quest duel action menu shown`
  - `Quest duel action clicked`
  - `Quest duel target`
  - `Quest duel selection`

### Tasks

1. Add a repeatable Quest duel diagnostics runner.
   - File targets:
     - `Tools/Run-QuestDebugSession.ps1`
     - `Assets/Scripts/MDPro3/Helper/QuestRuntimeDebugSettings.cs`
     - `Assets/Scripts/MDPro3/QuestDuelNativeUi.cs`
   - Output should include:
     - Current message/phase/turn.
     - Actionable cards with id/name/location/sequence/button responses.
     - Extra deck cards with id/name/type/location/button responses.
     - Current selectable cards/places.
     - Recent duel log line.
     - Active Quest panel count and panel names during selection prompts.
     - Whether a legacy MDPro3 popup or old duel resource became visible during Quest native duel.

2. Extend and enforce the expansion resource verifier.
   - Existing tool: `Tools/Test-QuestExpansionResources.ps1`.
   - It should check:
     - `D:\game\MDPro3\Expansions\*.cdb`
     - `D:\game\MDPro3\Expansions\script.zip`
     - loose `D:\game\MDPro3\Expansions\script\c*.lua`
   - It should report:
     - duplicate card ids and final winning database.
     - missing Lua scripts for effect/extra monsters.
     - Lua load errors through ocgcore for target ids.
     - Quest device-visible copies under `/sdcard/Android/data/com.ygo.mdpro3.quest/files/Expansions`.
   - First required target cards:
     - `100083` Life Forest / Link
     - `99993` White Hole Spirit / Fusion
     - `100352` Xyz monster with compatibility fallback
     - `100047` Link monster with category fallback

3. Create deterministic test duel fixtures.
   - No shuffle for debug fixture.
   - Known hand/field/extra contents.
   - Separate fixtures for:
     - Normal summon
     - Generic special summon
     - Link summon
     - Fusion via effect/card
     - Synchro
     - Xyz
     - Pendulum
     - Ritual
     - Chain/effect activation
     - Attack and direct attack
     - SelectPlace / Extra Monster Zone
     - SelectPosition
     - Xyz material detach / overlay count

4. Record fixture success, not only absence of errors.
   - Every fixture must produce grep-able lines proving:
     - candidate generation.
     - response sent.
     - expected summon/selection/message transition.
     - card reached expected location/state.

### Acceptance Gates

- ocgcore script-load checks pass for the target expansion scripts.
- `Tools/Test-QuestExpansionResources.ps1` reports `failure_count=0`.
- Quest device-side expansion files are present and match the host resource version for changed scripts/databases.
- Quest logs show extra deck candidates and button responses when opening the extra deck.
- Debug session can start a known duel without manual deck setup.
- Each fixture path has at least one successful log line:
  - Normal
  - Special
  - Fusion
  - Synchro
  - Xyz
  - Link
  - Pendulum
  - Ritual
  - Chain
  - Attack/direct attack
  - SelectPlace
  - SelectPosition
- The debug report shows zero new `NullReferenceException`, `InvalidKeyException`, missing Lua, or ocgcore script errors.

## Phase 2: Field-First Interaction Layer

Goal: remove the "panels everywhere" feeling and make the field itself interactive.

### Interaction Model

1. Hover card:
   - Show a readable card info panel near the card/standee.
   - Include card name, type, current state, ATK/DEF/level/rank/link/material count, and effect text.
   - Fade out when ray leaves.

2. Click actionable card:
   - Do not open a large card-selection panel for normal card actions.
   - Show compact card-local action buttons near the card:
     - Summon
     - Special Summon
     - Set/Place
     - Activate
     - Attack
     - View
   - Keep buttons short, icon-like where possible, and never longer than the card/standee zone.

3. Select target card:
   - Do not ask the user to choose from a flat list when targets are visible on field.
   - Mark selectable cards directly with target rings, glow, and slight hover.
   - Ray-clicking a marked card sends the response.

4. Select multiple cards:
   - Use field marking first.
   - If the source cards are not on field, use a curved floating card strip around the player, not a rectangular panel.

5. Confirm/cancel:
   - Use a small fixed confirmation console near the player, not a large center popup.

### File Targets

- `Assets/Scripts/MDPro3/QuestXrBootstrap.cs`
- `Assets/Scripts/MDPro3/QuestDuelNativeUi.cs`
- `Assets/Scripts/MDPro3/QuestDuelWorldPresenter.cs`
- `Assets/Scripts/MDPro3/Duel/DuelButton.cs`
- `Assets/Scripts/MDPro3/Servants/OcgCore.cs`

### Acceptance Gates

- A normal hand monster can be summoned by pointing at the card and selecting a local action.
- Attack target selection highlights field cards and accepts direct ray selection.
- Extra deck actionable cards are reachable without a generic rectangular list when possible.
- Large card panels are no longer used for common single-card actions.
- `SelectIdleCmd`, `SelectBattleCmd`, `SelectCard`, `SelectPlace`, and `SelectPosition` each have a logged interaction path.
- Debug screenshots from `Run-QuestDebugSession.ps1` show no legacy rectangular popup covering the duel field during common actions.
- Quest logs include active panel counts/poses for selection prompts so panel drift can be detected without wearing the headset.

## Phase 3: Readability And HUD Layout

Goal: make the duel state readable without blocking the monsters.

### Tasks

1. Persistent side wall HUD:
   - Duel log and current prompt on the right side wall.
   - LP/phase/buttons near the player, separated from the log.
   - No overlap between LP, log, surrender/exit, and phase controls.

2. Card-local HUD:
   - Monster ATK/DEF/level/rank/link/material HUD above standees.
   - Transparent dark plate, strong outline, clear color coding.
   - Never hidden behind a standee.

3. Card info:
   - Hover info belongs above/near the card but should not cover the monster face.
   - Side detail panel remains available only for pinned detailed reading.

### Acceptance Gates

- From the default Quest pose, LP, phase, latest duel log, and card stats are readable.
- Monster standees do not cover their own ATK/DEF labels.
- Card effect text is readable on hover without turning far to the side.

## Phase 4: Immersive Presentation Pack

Goal: make summons, effects, movement, and attacks feel like a VR duel rather than a flat port.

Before adding heavier visuals, add a lightweight event/pool/cache foundation:

- Reuse transient objects for rings, lines, labels, and impact flashes.
- Cache materials/textures for repeated card effects.
- Log summon kind, attacker, target, damage, and chain index from fixtures.
- Do not add a new effect if its event cannot be triggered by a deterministic fixture.

### Summon Effects

- Normal summon: card lift, landing ring, short sound.
- Special summon: vertical light column, standee rise.
- Fusion: material pull-in, color vortex, merged flash.
- Synchro: level/star rings, white circular rails.
- Xyz: material stack, black/gold orbit rings.
- Link: blue network lines, link marker pulses.
- Pendulum: left/right scale gates and arc.

### Attack Effects

- Attacking standee lunges toward target standee front edge.
- Target standee reacts with shake/flash.
- Dynamic attack line and impact slash.
- LP panel shakes on damage.
- Direct attack has a longer lane effect.
- Final blow has heavier timing, sound, and color.

### Effect/Chain Effects

- Activating card lifts slightly and emits typed color.
- Chain number appears above the activated card.
- Chain 3+ gets stronger sound/visual feedback.

### Acceptance Gates

- Attack visually communicates attacker, target, and damage.
- Each major summon family has a distinct visual identity.
- Important actions feel stronger than ordinary card movement without slowing the duel too much.

## Phase 5: Visual Polish And Performance

Goal: improve beauty without reintroducing resource flicker or Quest performance problems.

### Tasks

- Replace harsh ground grid with a calmer virtual duel arena floor.
- Use MDPro3/YGOPro2 resources only after verifying they load without old-scene flicker.
- Add object pooling for repeated effects and HUD elements.
- Add texture/material caching for card faces, card backs, standees, and particles.
- Keep MSAA 4 unless profiling proves it is the bottleneck.

### Acceptance Gates

- No old MDPro3 trees/rabbits/card backs flash during duel entry.
- No black/gray card faces in normal card zones.
- No repeated per-frame material churn in logs/profiler.

## Phase 6: Regression Checklist

Run this before calling a Quest duel version stable:

- Main menu visible, upright, and interactable.
- Solo/manual selection works; no unwanted auto-start unless debug flag is enabled.
- Deck editor cards show image and readable info.
- Enter duel without old MDPro3 scene resource flash.
- Hand card hover shows info and effect.
- Hand card action works.
- Monster summon, spell/trap set, activation, chain prompt, attack, target selection, surrender, and return-to-menu work.
- Extra deck count is correct.
- Link/Fusion/Synchro/Xyz/Pendulum/Ritual paths have at least one verified fixture.
- Quest log has no new `NullReferenceException`, `InvalidKeyException`, missing Lua, or script load errors.
