# BONES — Unity MVP

A 1940s-noir Cee-lo dice roguelike. You're always the banker, paying down Vito's escalating
marker across nights — riding **Heat**, dodging **Suspicion** busts, and fighting **The Squeeze**
(base odds that erode until you must cheat). This is the **MVP vertical slice**: the Cee-lo
heartbeat, a 3-night run with escalating Collections, Heat + Suspicion + The Squeeze, and a tiny Fence.

See the design docs in the parent folder (`../GAME_DESIGN_SPEC.md`, `../ECONOMY.md`,
`../erinswork/BONES-Asset-Manifest.html`) and the build plan at
`~/.claude/plans/i-want-to-build-sequential-hamming.md`.

## Requirements

- **Unity 6 LTS** (6000.0.x) — install via Unity Hub with **iOS Build Support** and
  **Android Build Support** (+ OpenJDK + Android SDK/NDK).
- macOS + **Xcode** for iOS builds (already present on this machine).

## First-time setup (3 steps)

1. **Open the project** in Unity Hub → Add → select the `BONES` folder. Unity imports packages
   (URP, Input System, TMP, Test Framework) and generates `.meta`/`Library` automatically.
2. **Generate content:** menu **BONES ▸ Generate MVP Data**. Creates the dice, the Lookout favor,
   Nights 1–3, juice tuning, and the wired `Assets/Data/GameDatabase.asset`.
3. **Build the scene:** menu **BONES ▸ Build Playable Scene**, then press **Play**.
   - If the UI is blank, open `Assets/Settings/BonesPanelSettings.asset` and assign a **Theme Style
     Sheet** (create one via *Assets ▸ Create ▸ UI Toolkit ▸ TSS Theme File* if none exists).

## Run the tests

**Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All.** Covers the deterministic core:
- All 216 Cee-lo outcomes and the exact banker win rates (56.25% ties→banker, 44.68% ties→push/mark,
  11.57% push), round resolution, economy/Heat/collection math, outcome-first resolver behaviour
  (faces always match the result, cheats fire, mark loading lowers the win rate), and the bust band.

## Architecture (where things live)

| Layer | Path | Notes |
|---|---|---|
| **Pure logic** (no Unity, unit-tested) | `Assets/Scripts/Core` | `CeeloEngine`, `OutcomeResolver` (outcome-first), `RoundService`, `EconomyService`, `SuspicionService`, `IRng`. `Bones.Core` asmdef has `noEngineReferences`. |
| **Data** (ScriptableObjects) | `Assets/Scripts/Data` + `Assets/Data` | `DieDefinition`, `ItemDefinition`, `NightConfig`/`CampaignConfig`, `JuiceTierConfig`, `GameDatabase`. |
| **State + save** | `Assets/Scripts/Model`, `Save` | `RunState`/`NightState`/`AccountState`; JSON to `persistentDataPath`. |
| **Brain** | `Assets/Scripts/GameController.cs` | Run/night/game flow, settlement, persistence; fires events. |
| **Presentation** | `Assets/Scripts/Presentation` | `DiceRollChoreographer` (sequential reveal, lingering final die), `DieView`/`DieFactory` (placeholder cubes), `AudioDirector`. |
| **Juice** | `Assets/Scripts/Juice` | `JuiceDirector` — tiered feedback (shake/flash/slow-mo + hooks). |
| **UI** | `Assets/Scripts/UI` | `SpotUI.uxml`/`.uss` + `GameUI` (HUD, throw flow, Title/Fence/Collection/Game-Over). |
| **Editor tools** | `Assets/Editor` | `DataBootstrap`, `SceneBuilder`. |

### Key principle: outcome-first dice
Game logic decides every die's face (after procs/cheats/The Squeeze) the instant a game is played;
the roll animation is **choreography to that known result** (manifest §1.4). This is what makes the
one-at-a-time reveal, the lingering final die, and the mid-tumble cheat tell possible.

## Mobile build

- **Player Settings:** Portrait orientation, IL2CPP, ARM64.
- **iOS:** File ▸ Build Settings ▸ iOS ▸ Build → open the Xcode project → run on device.
- **Android:** switch platform to Android ▸ Build (APK) or Build & Run with a device attached.

## What's placeholder (swap-in ready)
- **Dice:** tinted cubes via `DieFactory`; assign `DieDefinition.meshOverride`/`materialOverride`
  (rounded-cube + per-tier silhouettes) when Erin's art lands.
- **Audio:** `AudioDirector` clip slots are empty (runs silent); drop clips in.
- **Juice:** particle/panel-fracture hooks are stubbed in `JuiceDirector`; wire VFX.
- **Heat/Suspicion display:** currently a visible Heat readout (per the build manifest). The spec's
  "felt, never shown" variant is a presentation swap — route through `JuiceDirector`.
