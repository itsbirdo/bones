# BONES — Economy & Math Model

The numeric backbone for [`GAME_DESIGN_SPEC.md`](./GAME_DESIGN_SPEC.md). Defines the Cee-lo odds, payouts, stakes, the debt treadmill, **The Squeeze** curve, and how items and prices scale. **All concrete values are a starting baseline — final balance needs simulation/playtest (see §10).** The *relationships* and *levers* are the durable part.

---

## 1. Goals (what the numbers must deliver)

1. **You must cheat to win.** Base (no-cheat) odds start fair for the very first game, then fall **hard — to ~15% by the final round** (Vito himself cheats). Loaded dice are the *only* way to stay above water, so the run is about **acquiring and stacking cheats**. The shop is the game.
2. **Busts are rare punctuation — not the failure mode.** Getting caught is ~**1 in 30–40 games** for a normal cheating build, rising as you stack more / higher-proc cheat dice. You lose by going **broke** or **missing tribute**, not by busting. (If busts feel frequent, lower the rates — §5.1. Frequent busts make losing feel predictable and unfun.)
3. **Your build is the counterweight.** Loaded dice + payout charms + Heat must pull a 15% base back up to roughly even-or-better — so the run is winnable *with* a strong cheat build and unwinnable naked.
4. **No safety net = real death.** With 3 games/night and broke = game over, variance can kill a greedy or unlucky player — but good stake discipline + a build should clear early nights most of the time.
5. **The debt outruns flat play.** Tribute grows faster than base earning, forcing the player to cheat harder, buy more, and ride Heat.
6. **Survival targets (the Cloverpit model — decided 2026-06, confirmed 2026-08).** **Night 1 survival ≈ 85%+** for an honest, sensible new player (the night must feel winnable or people abandon; with the $40 seed vs the $15 tribute, cautious play is effectively safe and **Night 2 is the first wall at ~40%**). Beyond that, the campaign is **effectively unwinnable on a fresh account** (~1% of first runs win): winning requires good dice that only enter the Fence pool through unlocks ([`ACHIEVEMENTS.md`](./ACHIEVEMENTS.md)), so runs-to-first-win is the long arc, exactly like Cloverpit. Measured (Monte Carlo, 2026-08, post-simplification): **median ~11–12 runs to first win**; ~26–28% of accounts win within 20 runs.

---

## 2. Cee-lo base odds (exact)

Three dice, 216 equally-likely outcomes. Classify each roll:

| Outcome | Combos / 216 | Notes |
|---|---|---|
| Instant **win** (4-5-6 or any triple) | 12 | 4-5-6 = 6; triples = 6 |
| Instant **loss** (1-2-3) | 6 | |
| **Point** (pair + odd die) | 90 | 15 per point value 1–6 (uniform) |
| **No score** (re-roll) | 108 | exactly 50% — re-rolled until decisive |

Conditioning on a **decisive** roll (the 108 non-re-roll outcomes):

| Result | Prob (of decisive roll) |
|---|---|
| Instant win | 12/108 = **11.11%** |
| Instant loss | 6/108 = **5.56%** |
| Point = k (each k) | 15/108 = **13.89%** |

### 2.1 Banker (player) win probability — and why the tie rule is FIXED
You're the banker: you roll first; on a point, the mark rolls and you compare. **In Bones, ties always go to the banker** (permanent rule, decided 2026-08). The math reference for the alternatives, kept for the record:

| Tie rule | Banker **win** | Push | Banker **loss** |
|---|---|---|---|
| **Ties → banker** (the game's rule, always) | **56.25%** | — | 43.75% |
| Ties → push *(not used)* | 44.68% | 11.57% | 43.75% |
| Ties → mark *(not used)* | 44.68% | — | **55.32%** |

*Derivation: with a banker point k, you beat a mark point j when k ≥ j (ties-to-banker) plus the mark's instant-loss chance, minus the mark's instant-win chance. Summed over the uniform point distribution it gives 56.25%.*

**Why the tie flip was cut as a Squeeze lever (2026-08):** it maxes out at an ~11.5pp swing, and a visible *rule* that silently changes mid-run reads as a bug, not as fate — it even needed a dedicated item (The Equalizer) to patch the confusion it caused. Mark-loading alone now carries the whole curve (§5), and the rule the player learns on Night 1 holds forever.

---

## 3. Payouts & Heat

**Payout = stake × multiplier × Heat** (multiplier is *profit* on the stake):

| Win type | Multiplier |
|---|---|
| Point win | **1:1** |
| 4-5-6 | **2:1** |
| Triple | **3:1** (6-6-6 may pay **5:1** — top jackpot) |
| Loss | −stake |

**Average payout given a win ≈ 1.30×** stake, using the ties-to-banker win mix (≈80% point, ≈10% 4-5-6, ≈10% triple). As the win rate falls, instant wins make up a larger *share* of wins, lifting the average to **~2.11× at Vito** (you win seldom but big — see §4).

**Heat** (no meter — felt via juice): `Heat = 1 + 0.5 × (consecutive wins this night)`. Resets on a loss, a bust, or the start of a night.
- Game 1: ×1.0 · after 1 win: ×1.5 · after 2 wins: ×2.0 (the most you can reach in a 3-game night).
- *(Tunable — a steeper +0.75/win or a higher cap makes streaks swingier.)*

---

## 4. Expected value per game (the difficulty arc)

EV per game at stake S, no cheats: `EV = P(win) × avgPayout × S − P(loss) × S`

| Phase | Base win% | avgPayout | EV / game (no cheats) | Feel |
|---|---|---|---|---|
| **Night 1** | 56% | ~1.30 | **+0.29 S** | Favored — learn the loop (and game 1 has no Fence) |
| **Night 2** | 40% | ~1.42 | −0.04 S | Underwater — start cheating |
| **Night 3** | 36% | ~1.46 | −0.11 S | Cheat or fall behind |
| **Night 5** | 28% | ~1.60 | −0.28 S | Cheat hard |
| **Vito** | 15% | ~2.11 | **−0.53 S** | Unwinnable naked; wins are jackpot-only |

So **raw dice go from +29% to −53%** across the run — a brand-new player coasts night 1, but from night 2 they're losing money without cheats (the squeeze-earlier rebalance, 2026-06). **Loaded dice + payout charms + Heat must drag that back up**; a built player can be positive again even at Vito.

*Note: as base win% falls, a larger share of your (rarer) wins are instant **jackpots**, so **avgPayout rises** — you win seldom but big, and variance climbs toward the finale.*

---

## 5. The Squeeze — base win% by night

**One mechanism (simplified 2026-08): mark-loading.** Ties always go to the banker; from Night 2 on, Vito and his men literally cheat back. Loading is **severity-scaled**: at light loading the mark's crooked dice shave toward 4–6; heavier loading pinches them to 5–6; Vito-grade bones run to instant-win monsters. (The loading level is both the chance a roll is crooked *and* how crooked it is.) Measured curve (Monte Carlo vs honest player dice, ties → banker throughout, 200k+ games/night, 2026-08):

| Night | Loading | Base win% (no cheats) | Mark's wins come from |
|---|---|---|---|
| 1 | 0.00 | **56%** | honest dice |
| 2 | 0.58 | 40% | mostly high points |
| 3 | 0.72 | 36% | high points |
| 4 | 0.82 | 33% | high points, more monsters |
| 5 | 0.94 | 28% | points and monsters, ~even |
| 6 | 1.10 | 21% | mostly instant monsters |
| 7 — Vito | 1.20 | **15%** (deciding game: 1.40 → ~11%) | almost all instant monsters |

**The ~11% floor.** Your own **instant wins (4-5-6 / triples) are ~11% of decisive rolls and can't be taken from you** — base win never drops below ~11–12%. At Vito's 15% you win almost *only* on instant jackpots: his bones barely set points anymore, they hit 4-5-6s and triples that no point of yours can answer (ties included — that's how the curve gets under the tie-win floor without touching the rule).

**This is why you must cheat.** The opponent's loaded dice are the entire difficulty curve — and your loaded dice are the answer to theirs. It also shapes the build arc: point-quality dice (backed by the permanent tie rule) rule the mid-game while the marks still set points; by Vito only manufacturing your own instant wins fights back (SPEC §11).

### 5.1 Bust-rate calibration (Suspicion)

**Target: ~1 bust per 30–40 games** for a normal cheating build, scaling up as you stack heavier cheat dice — but always rare enough that a bust is *drama*, never why you lose.

**Three rules (simplified 2026-08; the same marks watch you all night):**

- **Accrual:** each crooked game adds the cup's summed tier value to the night's accrued suspicion. Every item is **Clean (0), Light (+1%) or Heavy (+2%)** — *leaning your own faces is Light; rewriting the hand or touching the mark's bones is Heavy* (SPEC §9.1/§11). A light + a heavy cheat = **3% per crooked game**. **The night's last crooked win is the riskiest** — a real push-your-luck arc, and management is the skill.
- **Cooling:** a clean game (**Lay Low**, or an honest cup) **halves** the accrued suspicion. This is what makes Lay Low a real decision: cool off mid-night before the big staked game, at the cost of playing one game at honest odds. Accrued suspicion zeroes at the start of each night.
- **The bust roll:** when a **crooked** game settles on a win, roll the **accrued total minus Favors**. A clean game's win is never checked (they have nothing on you this hand).

Notes:
- **No stake scaling (removed 2026-08).** The old `1 + stake/tribute` accrual multiplier was invisible, unreadable, and double-counted the deterrent: a bust **forfeits the staked pot**, so the cost of cheating big already scales with the stake on its own.
- **Baseline** (a light + a heavy): 3%/game accrual → checks at ~3/6/9% across a greedy night. **Maxed greedy** (three Heavies, never cooling): peaks ~**12–18%** on the night's last win. **No cheats:** 0%. Measured in the account-lifecycle Monte Carlo (2026-08): ~**1.2% busts/game overall** for a bot that manages (Lay Low, sit-outs, favors) — comfortably in the rare band.
- **Favors reduce the rolled chance:** Lookout (per game), Smooth Talker (negate one bust/night). Cold Read shows the exact number. *(Greased Palm and Cooler Head: deferred to the post-launch wave.)*
- **Global dial:** if playtests show busts too frequent, **scale the two tier values down**. Erring rare is correct.
- **Consequence:** **forfeit the game's staked pot + Heat resets** — nothing more. You can keep cheating in the night's remaining games (disabling cheats would make a cheat-dependent run unwinnable). No losing dice or the run.
- Accrual depends only on your cheat loadout, **not** on The Squeeze — but since harder nights push you to cheat more, the risk creeps up naturally while staying in the rare band.

---

## 6. Stakes & seed

| Knob | Baseline | Notes |
|---|---|---|
| **Seed bankroll** | **$40** | Monte Carlo-tuned (2026-06-12): $25 left no Night-1 war chest and Night 2 killed ~80% of runs on money, not odds. |
| **Min stake** | **$1** (or **sit out**: skip a game, risk nothing, cool the heat) | Below this you're **broke → game over**. Sitting out spends one of the night's 3 games and resets your streak. |
| **Night float** | **+$10** each new night | Walking-around money: there is always one more gamble in you. MC-tuned ($5 → 26% N2 survival, $10 → ~37%). |
| **Reckoning table minimum** | **25% of bankroll per game** | Vito names the table; min-staking the finale is not a thing, so the money you bring matters. |
| **Max stake** | **= your bankroll** | Monte Carlo verdict: tribute-capped stakes prevent the snowball that makes late nights winnable (0.4% vs ~7% session win rate). The favored first night is for farming a war chest; pot-forfeiting busts and broke-death are the discipline. |

The min stake defines the broke threshold: if you can't put up $1 (or whatever min is), the run ends.

---

## 7. The debt treadmill (tribute schedule)

At each night's **Collection**, the tribute is taken from your bankroll; **surplus carries** to the next night. Pay it or get whacked.

| Night | Tribute | Growth | Notes |
|---|---|---|---|
| 1 | $15 | — | Gentle by design; seed $40 leaves real farming room on the favored night |
| 2 | $30 | ×2.0 | The first wall: the squeeze has turned (40% honest win) and the chase begins |
| 3 | $55 | ×1.83 | builds start to matter |
| 4 | $100 | ×1.82 | |
| 5 | $170 | ×1.7 | real pressure |
| 6 | $290 | ×1.7 | lean on cheats + Heat |
| 7 — Reckoning | *(no tribute)* | — | the IOU showdown vs Vito (a Cee-lo game, not a payment) |

*Monte Carlo-tuned 2026-06-12 (`web/montecarlo.js`): the previous ×2.5-3.7 treadmill ($55…$2,400) outran what any build could earn; campaign win rate was ~0.003% even for mature accounts. Re-measured 2026-08 after the simplification pass: the ~×1.7-2.0 ladder holds the intended Cloverpit shape — **median ~11–12 runs to first win**, ~26–28% of accounts win within 20 runs, ~1% of first runs win, modal death Night 2.*

**The buffer trap (key tension):** paying tribute can leave you with too little to stake next night. You don't want to *barely* make rent — you want to overshoot and bank a cushion, because next night's tribute is ~2.5× bigger and you still need stake money. This is what forces aggressive, build-driven play.

> Schedule matches spec §6.3. These magnitudes vs. the +EV early / −EV late curve are the crux to validate by simulation — the gap between "what you can earn" and "what you owe" must be closeable *with* a good build and *not* without one.

---

## 8. Item value (how a build claws back the odds)

Rough models for how purchases shift EV — enough to price them; exact values need simulation.

**Loaded dice** convert some losing/weak rolls into wins. Approx win-rate gain:
`Δwin ≈ proc% × P(the situation it rescues is live) × P(that rescue flips the result)`
- *Snake Killer* (a rolled 1 → 2–6): kills 1-2-3 auto-losses and lifts low points. A rolled 1 appears often; at its 50% proc it meaningfully trims the loss column — call it **+3–6% win** equipped.
- *Lucky Six* / *High Roller*: push toward high points, 4-5-6, and triples — boosts both **win% and the payout mix** (more 2:1/3:1).
- *The Sequencer* / *The Magnet*: low proc but manufacture **instant wins** (4-5-6 / triples) — high payout-mix value, the jackpot enablers.

**Payout charms** lift the multiplier, not the win rate:
- *Gilded Die* (+50% on a win, 40% proc) ≈ **+0.20× to avgPayout** → turns avgPayout 1.30 → ~1.50, a real EV swing.
- *Headcracker* boosts the 11% instant-win payouts; *Streak Charm* / *Hot Hand* amplify Heat.

**Rule of thumb for pricing:** an item that adds ~+5% win or ~+0.15× payout is worth roughly one night's-worth of edge — price it so a player can afford ~1–2 impactful items per shop, not a full build in one night.

---

## 9. Price & re-roll scaling

**Item prices** (the catalog lists **base price** = Night-1 cost):
`price(night N) = base × (1 + 0.5 × (N − 1))`  → N1 ×1.0, N4 ×2.5, N7 ×4.0 (linear baseline).
*(Alternative to test: scale to the night's tribute so prices stay proportional to how flush you are.)*

**Fence re-roll** (refresh the shop; resets each night):
- Base (first re-roll of the night) ≈ **10% of the night's tribute** → N1 ~$2, N3 ~$14, N5 ~$95.
- Each further re-roll that night **×1.8** (e.g., $2 → $3.6 → $6.5 → …).
- Keeps early re-rolls cheap and discovery-friendly, while deep-run re-rolling is a genuine luxury that competes with stake money.

---

## 10. Tuning knobs (summary) & validation

The dials, most impactful first:

| Knob | Baseline | Effect |
|---|---|---|
| Tie rule | **ties → banker, always** | fixed rule (2026-08); no longer a difficulty lever |
| Mark-loading per night | 0.58 → 1.20 (severity-scaled; decider 1.40) | **the whole Squeeze** from N2 on |
| Base win% per night | 56→15% | overall difficulty arc (measured, §5) |
| Payout multipliers | 1 / 2 / 3 (×) | reward magnitude & jackpot feel |
| Heat formula | 1 + 0.5×wins | streak reward / swinginess |
| Seed bankroll + night float | $40, +$10/night | early-run survivability (N1 safe; N2 is the wall) |
| Tribute schedule | $15…$290 | the earning treadmill |
| Suspicion tiers | Light 1% / Heavy 2% | bust rarity (the global dial: scale both) |
| Item EV & prices | §8–9 | how fast a build comes online |
| Re-roll cost curve | 10% tribute, ×1.8 | shop-churn vs stake tension |

**Validation — the Monte Carlo exists (`web/montecarlo.js`; run it after any numbers change).** It simulates persistent accounts across runs under cautious/aggressive bot policies, models **store randomness** (5 offers from the unlocked pool, the loaded-die quota slot, re-roll churn) and **unlock gating** (the Fence pool grows run over run per [`ACHIEVEMENTS.md`](./ACHIEVEMENTS.md)), and reports the night-by-night funnel, night-of-death distribution, bust rate, runs-to-first-win, and catalog growth. The 2026-06 rebalance (seed $40, bankroll-capped stakes, the ×1.7-2.0 ladder) and the 2026-08 re-measure (median ~11–12 runs to first win) both came out of this tool. `web/sim.js` verifies the per-night odds curve and the suspicion model against the values in this document.

---

## 11. Open questions

1. ~~**Seed vs N1 tribute**~~ **Resolved (2026-06, retuned):** $40 seed / $15 N1 tribute / +$10 night float, tuned by Monte Carlo (§1 goal 6, §6).
2. ~~**Max stake**~~ **Resolved (2026-06):** max stake = your whole bankroll (§6) — the tribute cap strangled the favored night's war-chest farming.
3. **Heat cap** — with only 3 games, ×2.0 is the ceiling. Worth a bigger per-win step so streaks feel explosive?
4. **Price scaling basis** — night-number (predictable) vs tribute/bankroll (proportional)?
5. **Does surplus carry fully**, or does Vito skim interest on what you keep (tightening the buffer trap further)?
