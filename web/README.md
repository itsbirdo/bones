# BONES: web playtest build

A deliberately ugly, fully playable web version of BONES for testing game flow and feel.
This is the tuning sandbox: rules live in one pure-logic file, numbers are easy to change,
and a simulation harness verifies the math after every tweak.

## Run it

No build step, no dependencies:

```text
open web/index.html            # double-click works too
# or, if your browser blocks local file scripts:
python3 -m http.server -d web 8000   # then visit http://localhost:8000
```

Progress (unlocks, lifetime stats) persists in localStorage. To wipe the account:
DevTools console → `localStorage.removeItem('bones_account')`.

Tick **dev numbers** on the title screen to see Heat, accrued Suspicion, the next-game bust
chance, and the night's loading while you play.

## Verify the math

```text
node web/sim.js          # or: cd web && npm run sim
```

Checks: the throw-sequence model (visible re-rolls, honest dice exactly uniform), the Squeeze
curve per night, Night-1 survival, the simplified suspicion model (flat accrual, clean games
halve), the Reckoning flow, and the endgame build arc (point-war rules the mid-game, jackpot
manufacture fights Vito).

## Balance Monte Carlo (the tuning tool)

```text
node web/montecarlo.js                       # 1500 accounts, 20 runs/account cap
node web/montecarlo.js 2500 20               # bigger sample (or: cd web && npm run mc:big)
node web/montecarlo.js 800 15 \
  15,30,55,100,170,290 \                     # tribute schedule override (N1..N6)
  bankroll \                                 # stake cap mode: tribute | double | bankroll
  40 \                                       # seed bankroll override
  0,0.58,0.72,0.82,0.94,1.10,1.20            # loading curve override (N1..N7)
```

Simulates persistent accounts across many runs (unlocks carry over, like a real player session)
with cautious and aggressive bot profiles. Reports the night-by-night funnel (reached, survived,
broke vs whacked), progression by run number, runs-to-first-win, and catalog growth. The
2026-06-12 rebalance (seed $40, bankroll-capped stakes, the x1.7-2.0 tribute ladder) came out of
sweeps with this tool; re-run it after any numbers change.

## What this build implements (the 2026-08 simplification pass)

- Cee-lo as the banker, one-die-at-a-time reveals, natural "nothing" re-rolls shown.
- **Ties always go to the banker** (the rule the player learns on Night 1 never changes).
  The Squeeze runs on severity-scaled mark loading alone.
  Honest win rates by night: 56 / 40 / 36 / 33 / 28 / 21 / 15% (Vito's decider ~11%).
- Seed $40, Night-1 tribute $15 (~85%+ first-night survival target).
- Heat (hidden; the stake line shows WIN PAYS instead). **Simplified Suspicion**: each item is
  Clean, Light (+1%/game) or Heavy (+2%/game); crooked games add the cup's sum, a clean game
  halves what's accrued, a crooked win rolls the total. No stake scaling (the forfeited pot
  already scales the punishment). Lay Low is the cooling decision.
- 3-slot cup where charm dice compete with cheats; The Fence (5 offers, at least one loaded die
  always stocked, escalating re-roll cost). **No honing**: every item has one fixed proc.
- Account-level unlocks: a 3-item starting pool, the rest earned by playing (ACHIEVEMENTS.md).
  Curses plus Greased Palm and Cooler Head are **deferred to a post-launch unlock wave**.
- The endgame dice: **The Finisher** (point-6 becomes 4-5-6), **The Spoiler** (the mark's point
  drops a pip). Build arc: point-war rules the mid-game (~42% at N5), only jackpot manufacture
  fights Vito (jackpot ~50%, point-war ~23%, honest ~15%).
- The Reckoning: best of three vs Vito, cash stakes live, Suspicion off, nastier bones at 1-1.
- **The Teach** (spec §12.9): the first-run diegetic tutorial — 11 one-line, in-voice barks that
  each fire once per account at the moment a rule first matters (point, tie, Heat, the Fence,
  Suspicion…), plus the always-on point-comparison settle line ("his 4 against your 5").
  Accounts with 30+ games are grandfathered as fully taught.

## Files

| File | What |
|---|---|
| `engine.js` | All rules and numbers. Pure logic, no DOM; `require`-able from Node. |
| `app.js` | Screens, reveal sequencing, Bag/Fence overlays, The Teach. No game rules in here. |
| `sim.js` | The rules/odds verification harness (`node web/sim.js`). |
| `ui-smoke.js` | Headless UI test: plays real runs against `app.js` on a mini-DOM; verifies the screen flow and that every Teach beat fires once (`node web/ui-smoke.js`). |
| `montecarlo.js` | Account-lifecycle balance sim (`node web/montecarlo.js`). |
