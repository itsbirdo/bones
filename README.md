# BONES

A 1940s-noir **Cee-lo dice roguelike** for mobile. You're always the banker, paying down Vito
Carbone's escalating marker across seven nights — building a cup of loaded and charmed dice, riding
**Heat** (a win-streak multiplier), dodging **Suspicion** (rare cheating busts), and fighting **The
Squeeze** (base odds that erode each night until you *have* to cheat). Beat Vito at the final-night
Reckoning and burn the marker, or end up in the river.

> **Status:** Playable MVP vertical slice (Unity 6). Core systems work end-to-end. Art, audio, and
> juice are intentionally deferred until the visual style is locked (being authored separately) — so
> the dice are placeholder cubes-with-pips for now. See [`TASKLIST.md`](./TASKLIST.md).

## Play it

Requires **Unity 6 LTS** (6000.0.x) with iOS/Android build support. Then:

1. Open the **`BONES/`** folder in Unity Hub.
2. Menu **BONES ▸ Generate MVP Data**.
3. Menu **BONES ▸ Build Playable Scene** → press **Play → NEW RUN**.

Full setup, controls, and the on-device build steps are in [`BONES/README.md`](./BONES/README.md).
Tests: **Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All**.

## How it plays

Each night you get **three throws** at the marker. Put up a stake, flick the dice in, and read the
result one die at a time:

- **4-5-6 or a triple** → instant win (jackpot). **1-2-3** → instant loss.
- A **point** (pair + odd die) → the mark counter-rolls to beat it.
- Win streaks build **Heat** (bigger payouts); cheating builds **Suspicion** (rare busts).
- Between nights, spend at **The Fence** (buy/hone dice) and set your loadout in **The Bag**.
- Miss a Collection or go broke → game over. Beat Vito on Night 7 → freedom.

## Repository layout

| Path | What |
|---|---|
| [`GAME_DESIGN_SPEC.md`](./GAME_DESIGN_SPEC.md) | The full design (rules, run structure, systems) |
| [`LEVELS_AND_FLOW.md`](./LEVELS_AND_FLOW.md) · [`ECONOMY.md`](./ECONOMY.md) · [`ACHIEVEMENTS.md`](./ACHIEVEMENTS.md) | Progression, money/balance, unlocks |
| [`ceelo.md`](./ceelo.md) · [`RESEARCH.md`](./RESEARCH.md) | Cee-lo rules deep-dive · design research |
| [`BONES-Asset-Manifest.html`](./BONES-Asset-Manifest.html) | Build/asset manifest ("2D is the world, 3D is the dice") |
| [`NARRATIVE.md`](./NARRATIVE.md) | Script & UI copy (noir voice) |
| [`TASKLIST.md`](./TASKLIST.md) | What's done / deferred / left to build |
| [`BONES/`](./BONES/) | The Unity project |

## Tech

- **Unity 6**, UI Toolkit for HUD/screens, 2D world + 3D dice (URP-ready), mobile-first (iOS + Android).
- **Pure-C# game core** (`BONES/Assets/Scripts/Core`) with no Unity dependencies — fully unit-tested
  (the 216 Cee-lo outcomes and exact banker win rates are verified). Logic is **outcome-first**: the
  result is decided first, and the roll is choreography to it.
- Designer-tunable content via ScriptableObjects; JSON save/resume; fully offline.

## License

All rights reserved (for now).
