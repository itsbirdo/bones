# BONES: Item Catalog Status

> **2026-08 simplification pass (sync the Unity port to spec §11):** honing/levels are **removed**
> (every item has one fixed proc %), per-item suspicion collapsed to **Light +1% / Heavy +2%**
> tiers, **The Equalizer is cut** (ties go to the banker permanently), and the **Curse category
> plus Greased Palm and Cooler Head are deferred** to a post-launch unlock wave. The web build
> (`web/engine.js`) implements all of this and is the reference.

Bridge document for the dice/charm/favor/trinket build-out (spec §11, issue #2 subtask 3).
Every §11 item is listed below with its category, the `DieEffect` or `effectTag` it maps to in
code, and a status:

- **Implemented**: maps cleanly to an existing, working effect. In `db.startingUnlocks`, balanced to test now.
- **Needs: <system>**: carries a clearly-named no-op stub effect/tag (`// TODO` in `DataBootstrap.cs`
  and `OutcomeResolver.cs`). Registered in the catalog but kept OUT of `startingUnlocks` until the
  named system exists. These do NOT silently behave like a working item.

The 6 original MVP dice and the Lookout favor were not changed, only added to. All content is authored
in `Assets/Editor/DataBootstrap.cs`; a **BONES > Generate MVP Data** run is required to materialize the
`.asset` files.

## Loaded dice

| Name | Category | Maps to (DieEffect/effectTag) | Status |
|---|---|---|---|
| Shaved Edge | Loaded | `BiasHigh` | Implemented |
| Even Steven | Loaded | `BumpParityEven` | Needs: per-die parity-nudge in resolver |
| Snake Killer | Loaded | `KillInstantLoss` | Implemented (original MVP die) |
| Lucky Six | Loaded | `BiasSix` | Implemented (original MVP die) |
| Two-Face | Loaded | `BiasOneOrSix` | Needs: per-die face-restriction in resolver |
| High Roller | Loaded | `BumpLowFaces` | Needs: per-die +2 low-face nudge in resolver |
| Matchmaker | Loaded | `CopyOtherDie` | Needs: cross-die face read in resolver |
| The Sequencer | Loaded | `ForceFourFiveSix` | Implemented |
| The Magnet | Loaded | `ForceTriple` | Implemented |

## Control / trick dice

| Name | Category | Maps to (DieEffect/effectTag) | Status |
|---|---|---|---|
| The Cooler | Control | `LockThroughReroll` | Needs: re-roll system |
| Reroll Bone | Control | `RerollSelf` | Needs: re-roll system |
| The Nudge | Control | `NudgeSelf` | Needs: post-land input system |
| Mulligan Cup | Control | `RerollHand` | Needs: re-roll system |
| Second Wind | Control | `ReviveInstantLoss` | Needs: re-throw system |
| Set-Bone | Control | `SetFace` | Needs: set-a-die input system |

## Charms

| Name | Category | Maps to (DieEffect/effectTag) | Status |
|---|---|---|---|
| Rabbit's Die | Charm | `RefundOnLoss` | Needs: loss-refund hook in EconomyService |
| Gilded Die | Charm | `PayoutCharm` | Implemented (original MVP die) |
| Point Sharp | Charm | `PointSharp` | Needs: point-value bump in settle |
| Hot Hand | Charm | `HeatCharm` | Implemented |
| Headcracker | Charm | `JackpotCharm` | Implemented (original MVP die) |
| Streak Charm | Charm | `HeatCharm` | Implemented (original MVP die) |

## Curses (DEFERRED — post-launch unlock wave, 2026-08)

| Name | Category | Maps to (DieEffect/effectTag) | Status |
|---|---|---|---|
| Gambler's Curse | Curse | `ForceBigStake` | Needs: stake-floor enforcement |
| All-or-Nothing | Curse | `DoubleSwing` | Needs: double-win/double-loss settle |
| Snake Eyes Pact | Curse | `SnakeEyesPact` | Needs: payout-mult + any-1-kills-throw |
| Bloody Knuckles | Curse | `BloodyKnuckles` | Needs: no-suspicion-decay + faster Heat |

## Favors

| Name | Category | Maps to (DieEffect/effectTag) | Status |
|---|---|---|---|
| Lookout | Favor | `suspicion_reduce` | Implemented (original MVP item) |
| Cold Read | Favor | `reveal_odds` | Needs: UI readout (no mechanical effect) |
| Greased Palm | Favor | `suspicion_reduce` | Implemented — **DEFERRED to the post-launch wave (2026-08)** |
| Cooler Head | Favor | `suspicion_reduce` | Implemented — **DEFERRED to the post-launch wave (2026-08)** |
| Smooth Talker | Favor | `bust_negate` | Needs: bust-negation hook |

## Trinkets

| Name | Category | Maps to (DieEffect/effectTag) | Status |
|---|---|---|---|
| Pawn Ticket | Trinket | `sell_die` | Needs: die sell-back economy |
| High Roller's Clip | Trinket | `max_stake_up` | Needs: max-stake cap system |
| Insurance Chit | Trinket | `loss_recover` | Needs: loss-recovery hook in EconomyService |
| Vig Skimmer | Trinket | `loss_skim` | Needs: partial-pot-on-loss hook |
| Lucky Cigarette | Trinket | `free_reroll` | Needs: Fence re-roll system |
| Loaded Coin | Trinket | `broke_bailout` | Needs: broke-bailout 50/50 hook |
| Rabbit's Foot | Trinket | `forgive_first_loss` | Needs: first-loss-forgive hook |
| Brass Knuckles | Trinket | `reprieve` | Needs: missed-Collection reprieve system |
| Vito's Favor | Trinket | `skip_interest` | Needs: debt-interest skip system |
| Marker Shaver | Trinket | `debt_slow` | Needs: debt-growth scalar system |

## Starting unlocks (implemented + balanced to test now)

`snake_killer`, `lucky_six`, `gilded_die`, `streak_charm`, `headcracker`, `shaved_edge`,
`the_sequencer`, `the_magnet`, `hot_hand`

(Greased Palm and Cooler Head dropped from this testing set — deferred to the post-launch wave.
The shipping core pool is the 3-item set in ACHIEVEMENTS.md: `shaved_edge`, `gilded_die`, `lookout`.)

All other items are registered in `db.allDice` / `db.allItems` but excluded from `startingUnlocks`;
they await their named systems and the achievement-unlock work (a separate subtask).
