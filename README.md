<div align="center">

# 🎲 BONES

*A 1940s-noir dice roguelike where you're the banker: load the dice, ride your luck, and pay down a mobster's debt before he collects.*

[![Platform](https://img.shields.io/badge/platform-iOS%20%7C%20Android-1f1d21?style=flat-square)](#)
[![Engine](https://img.shields.io/badge/engine-Unity%206-1f1d21?style=flat-square)](https://unity.com)
[![Status](https://img.shields.io/badge/status-MVP%20in%20development-d4a838?style=flat-square)](https://github.com/itsbirdo/bones/issues)
[![License](https://img.shields.io/badge/license-all%20rights%20reserved-be282c?style=flat-square)](#license)

</div>

<!-- TODO: hero demo GIF of a throw (dice rolling in, the reveal, a HEADCRACK! win). -->
<!-- Capture with Kap (brew install --cask kap), 10-15fps, <5MB, save to assets/demo.gif. -->
<!-- Visuals are placeholder cubes for now (see "Visual style" below), so the hero is held until the art lands. -->

You owe Vito Carbone more than you've got. He gives you a marker and seven nights to clear it. Every night you get **three throws** of the dice to earn your way out, and a Collector comes by at dawn to take what's owed. The honest odds turn against you as the debt climbs, so you'll have to cheat. Get caught, and it's over. Beat Vito on the final night, and you walk away clean.

It's [Cee-lo](./ceelo.md), a fast three-dice street game, wrapped in a Balatro-style roguelike of loaded dice, escalating stakes, and one bad night after another.

## Features

- **You're always the banker:** the best seat in the game, except you still owe Vito and the odds erode every night until the house can't save you.
- **Cheat to survive:** load dice, palm charms, and read the heat; honest play won't beat the final night.
- **Ride your Heat:** consecutive wins stack a payout multiplier. One loss wipes it. Press or bank?
- **Watch your Suspicion:** every crooked die nudges a rare, brutal bust closer. Lay low, or push your luck.
- **Build a run at the Fence:** a roguelike shop of random dice and charms; hone your loadout between nights.
- **Seven nights, one way out:** escalating debt, no safety net. Beat Vito at the Reckoning, or end up in the river.

## Quick start

You need **Unity 6 LTS** (6000.0.x). Then:

```text
1. Open the BONES/ folder in Unity Hub.
2. Menu:  BONES ▸ Generate MVP Data
3. Menu:  BONES ▸ Build Playable Scene
4. Press Play ▸ NEW RUN
```

<details>
<summary>Prerequisites & mobile builds</summary>

- **Unity 6 LTS** with **iOS Build Support** and **Android Build Support** (OpenJDK + Android SDK/NDK).
- **iOS:** build to Xcode, then run on device. **Android:** build an APK/AAB or Build & Run with a device attached.
- Full setup, controls, and per-platform steps: [`BONES/README.md`](./BONES/README.md).

</details>

## How to play

Each night, put up a stake and **flick the dice in** (or tap *Flick to Throw*). They roll in one at a time, the last one lingering:

- **4-5-6 or a triple** wins outright (jackpot). **1-2-3** loses outright.
- Anything else sets a **point** (a pair plus an odd die); the mark counter-rolls to beat it.
- Win streaks build **Heat** (bigger payouts); cheating builds **Suspicion** (rare busts).

Between throws, spend your winnings at **The Fence** (buy and hone dice) and set your three-die loadout in **The Bag**. Miss a Collection or run out of cash and the run ends. Survive to Night 7, beat Vito best-of-three, and burn the marker.

## How it works

The game logic is a **pure C# core** (`BONES/Assets/Scripts/Core`) with no Unity dependencies, so it's fully unit-tested: the 216 Cee-lo outcomes and the exact banker win rates are verified in the test suite. It's **outcome-first**, meaning the result of a roll is decided first (after applying your cheats and the night's difficulty), and the animation is choreographed to that known result. Content (dice, charms, nights) is authored as Unity ScriptableObjects, and runs save to local JSON, fully offline.

Run the tests: **Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All**.

## Visual style

The art is being developed in a separate tool, so the current build uses **placeholder dice** (tinted cubes with pips) and no audio. Real meshes, the inked Sin City look, sound, and juice drop into existing swap-in slots later; the systems are built to receive them. That's why there are no screenshots yet. See the [open issues](https://github.com/itsbirdo/bones/issues) for what's done and what's next.

## Project layout

| Path | What |
|------|------|
| [`BONES/`](./BONES/) | The Unity project (code, data, tests, editor tools) |
| [`GAME_DESIGN_SPEC.md`](./GAME_DESIGN_SPEC.md) | The full design: rules, run structure, systems |
| [`ECONOMY.md`](./ECONOMY.md) · [`LEVELS_AND_FLOW.md`](./LEVELS_AND_FLOW.md) · [`ACHIEVEMENTS.md`](./ACHIEVEMENTS.md) | Money & balance · progression · unlocks |
| [`ceelo.md`](./ceelo.md) · [`RESEARCH.md`](./RESEARCH.md) | Cee-lo rules · design research |
| [`NARRATIVE.md`](./NARRATIVE.md) | Script & UI copy |

The roadmap and tasks live in [GitHub Issues](https://github.com/itsbirdo/bones/issues) (grouped into epics by area).

## License

All rights reserved. © 2026. Not yet licensed for reuse.
