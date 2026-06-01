# BONES — Economy & Math Model

The numeric backbone for [`GAME_DESIGN_SPEC.md`](./GAME_DESIGN_SPEC.md). Defines the Cee-lo odds, payouts, stakes, the debt treadmill, **The Squeeze** curve, and how items and prices scale. **All concrete values are a starting baseline — final balance needs simulation/playtest (see §10).** The *relationships* and *levers* are the durable part.

---

## 1. Goals (what the numbers must deliver)

1. **You must cheat to win.** Base (no-cheat) odds start fair for the very first game, then fall **hard — to ~15% by the final round** (Vito himself cheats). Loaded dice are the *only* way to stay above water, so the run is about **acquiring and stacking cheats**. The shop is the game.
2. **Busts are rare punctuation — not the failure mode.** Getting caught is ~**1 in 30–40 games** for a normal cheating build, rising as you stack more / higher-proc cheat dice. You lose by going **broke** or **missing tribute**, not by busting. (If busts feel frequent, lower the rates — §5.1. Frequent busts make losing feel predictable and unfun.)
3. **Your build is the counterweight.** Loaded dice + payout charms + Heat must pull a 15% base back up to roughly even-or-better — so the run is winnable *with* a strong cheat build and unwinnable naked.
4. **No safety net = real death.** With 3 games/night and broke = game over, variance can kill a greedy or unlucky player — but good stake discipline + a build should clear early nights most of the time.
5. **The debt outruns flat play.** Tribute grows faster than base earning, forcing the player to cheat harder, buy more, and ride Heat.

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

### 2.1 Banker (player) win probability — set by the tie rule
You're the banker: you roll first; on a point, the mark rolls and you compare. The **tie rule is the single biggest odds lever** (and the core of The Squeeze):

| Tie rule | Banker **win** | Push | Banker **loss** |
|---|---|---|---|
| **Ties → banker** (early game) | **56.25%** | — | 43.75% |
| **Ties → push** (mid game) | 44.68% | 11.57% | 43.75% |
| **Ties → mark** (late / Vito) | 44.68% | — | **55.32%** |

*Derivation: with a banker point k, you beat a mark point j when k ≥ j (ties-to-banker) plus the mark's instant-loss chance, minus the mark's instant-win chance. Summed over the uniform point distribution it gives 56.25%. Flipping ties to the mark moves the 11.57% of tie outcomes from wins to losses.*

This is why the banker seat *feels* favored early — and why simply **drifting the tie rule** turns the screw without the player noticing a number change.

---

## 3. Payouts & Heat

**Payout = stake × multiplier × Heat** (multiplier is *profit* on the stake):

| Win type | Multiplier |
|---|---|
| Point win | **1:1** |
| 4-5-6 | **2:1** |
| Triple | **3:1** (6-6-6 may pay **5:1** — top jackpot) |
| Loss | −stake |

**Average payout given a win ≈ 1.30×** stake, using the ties-to-banker win mix (≈80% point, ≈10% 4-5-6, ≈10% triple). As the win rate falls, instant wins make up a larger *share* of wins, nudging the average up to ~1.44× near Vito.

**Heat** (no meter — felt via juice): `Heat = 1 + 0.5 × (consecutive wins this night)`. Resets on a loss, a bust, or the start of a night.
- Game 1: ×1.0 · after 1 win: ×1.5 · after 2 wins: ×2.0 (the most you can reach in a 3-game night).
- *(Tunable — a steeper +0.75/win or a higher cap makes streaks swingier.)*

---

## 4. Expected value per game (the difficulty arc)

EV per game at stake S, no cheats: `EV = P(win) × avgPayout × S − P(loss) × S`

| Phase | Base win% | avgPayout | EV / game (no cheats) | Feel |
|---|---|---|---|---|
| **Night 1** | 55% | ~1.31 | **+0.27 S** | Favored — learn the loop (and game 1 has no Fence) |
| **Night 3** | 40% | ~1.42 | −0.03 S | Underwater — start cheating |
| **Night 5** | 27% | ~1.62 | −0.29 S | Cheat hard |
| **Vito** | 15% | ~2.11 | **−0.53 S** | Unwinnable naked; wins are jackpot-only |

So **raw dice go from +27% to −53%** across the run — a brand-new player coasts night 1, but by night 3 they're losing money without cheats. **Loaded dice + payout charms + Heat must drag that back up**; a built player can be positive again even at Vito.

*Note: as base win% falls, a larger share of your (rarer) wins are instant **jackpots**, so **avgPayout rises** — you win seldom but big, and variance climbs toward the finale.*

---

## 5. The Squeeze — base win% by night

Two mechanisms, escalating: **tie-rule drift** early, then **mark-loading** late — Vito and his men literally cheat back. Baseline:

| Night | Mechanism | Base win% (no cheats) |
|---|---|---|
| 1 | ties → banker | **55%** |
| 2 | ties → push | 47% |
| 3 | ties → mark | 40% |
| 4 | mark lightly loaded | 33% |
| 5 | mark loaded | 27% |
| 6 | mark heavily loaded | 20% |
| 7 — Vito | Vito fully loaded | **15%** |

**The ~11% floor.** Your own **instant wins (4-5-6 / triples) are ~11% of decisive rolls and can't be taken from you** — base win never drops below ~11–12%. At Vito's 15% you win almost *only* on instant jackpots; the mark takes nearly every point battle.

**This is why you must cheat.** Tie-drift alone floors at ~45% (§2.1). Getting below that requires the **opponent to load their own dice** — exactly what Vito's crew does as you climb. Your loaded dice are the answer to theirs.

### 5.1 Bust-rate calibration (Suspicion)

**Target: ~1 bust per 30–40 games** for a normal cheating build (~2.5–3.3%/game), scaling up as you stack more / higher-proc cheat dice — but always rare enough that a bust is *drama*, never why you lose.

- **Suspicion = the bust % for that game** — the **sum of the small bust-% each equipped cheat die adds** (catalog §11), minus Favors. Rolled when the game settles on a win.
- **Baseline** (~3 mid cheat dice): ~**3%** → ~1 in 33. **Maxed greedy** (3 high-proc cheat dice): ~**6–8%** → ~1 in 12–16. **No cheats:** 0%.
- **Favors reduce it:** Lookout, Greased Palm (per game), Smooth Talker (negate one bust/night), Cooler Head (faster decay).
- **Global dial:** if playtests show busts too frequent, **scale every suspicion value down.** Erring rare is correct.
- **Consequence:** **forfeit the game's staked pot + Heat resets** — nothing more. You can keep cheating in the night's remaining games (disabling cheats would make a cheat-dependent run unwinnable). No losing dice or the run.
- Bust % depends only on your cheat loadout, **not** on The Squeeze — but since harder nights push you to cheat more, risk creeps up naturally while staying in the rare band.

---

## 6. Stakes & seed

| Knob | Baseline | Notes |
|---|---|---|
| **Seed bankroll** | **$18** | Start of every run. Enough to weather a cold opening night. |
| **Min stake** | **$1** | Below this you're **broke → game over**. |
| **Max stake** | **= the night's tribute** (capped at bankroll) | Lets a hot hand clear a night fast; lets the desperate go big. |

The min stake defines the broke threshold: if you can't put up $1 (or whatever min is), the run ends.

---

## 7. The debt treadmill (tribute schedule)

At each night's **Collection**, the tribute is taken from your bankroll; **surplus carries** to the next night. Pay it or get whacked.

| Night | Tribute | Growth | Notes |
|---|---|---|---|
| 1 | $20 | — | Gentle; seed $18 means you must net a little and build a buffer |
| 2 | $55 | ×2.75 | |
| 3 | $140 | ×2.55 | builds start to matter |
| 4 | $375 | ×2.68 | |
| 5 | $950 | ×2.53 | real pressure |
| 6 | $2,400 | ×2.53 | lean on cheats + Heat |
| 7 — Reckoning | *(no tribute)* | — | the IOU showdown vs Vito (a Cee-lo game, not a payment) |

**The buffer trap (key tension):** paying tribute can leave you with too little to stake next night. You don't want to *barely* make rent — you want to overshoot and bank a cushion, because next night's tribute is ~2.5× bigger and you still need stake money. This is what forces aggressive, build-driven play.

> Schedule matches spec §6.3. These magnitudes vs. the +EV early / −EV late curve are the crux to validate by simulation — the gap between "what you can earn" and "what you owe" must be closeable *with* a good build and *not* without one.

---

## 8. Item value (how a build claws back the odds)

Rough models for how purchases shift EV — enough to price them; exact values need simulation.

**Loaded dice** convert some losing/weak rolls into wins. Approx win-rate gain:
`Δwin ≈ proc% × P(the situation it rescues is live) × P(that rescue flips the result)`
- *Snake Killer* (a rolled 1 → 2–6): kills 1-2-3 auto-losses and lifts low points. A rolled 1 appears often; at ~50% proc it meaningfully trims the loss column — call it **+3–6% win** equipped, scaling with level.
- *Lucky Six* / *High Roller*: push toward high points, 4-5-6, and triples — boosts both **win% and the payout mix** (more 2:1/3:1).
- *The Sequencer* / *The Magnet*: low proc but manufacture **instant wins** (4-5-6 / triples) — high payout-mix value, the jackpot enablers.

**Payout charms** lift the multiplier, not the win rate:
- *Gilded Die* (+50% on a win, ~30% proc) ≈ **+0.15× to avgPayout** → turns avgPayout 1.30 → ~1.45, a real EV swing.
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
| Tie-rule drift schedule | banker→push→mark | The Squeeze's main lever; ±11.5% win swing |
| Base win% per night | 56→38% | overall difficulty arc |
| Payout multipliers | 1 / 2 / 3 (×) | reward magnitude & jackpot feel |
| Heat formula | 1 + 0.5×wins | streak reward / swinginess |
| Seed bankroll | $18 | early-run survivability |
| Tribute schedule | $20…$2,400 | the earning treadmill |
| Item EV & prices | §8–9 | how fast a build comes online |
| Re-roll cost curve | 10% tribute, ×1.8 | shop-churn vs stake tension |

**Validation — a Monte Carlo simulation (deferred, not yet).** Eventually these interlocking dials want a sim: thousands of automated runs under a few play policies (cautious / aggressive / cheater), reading **win-rate-to-Reckoning** and **night-of-death distribution**. Target shape: most early runs die N3–N4 (short, punchy), competent runs reach the Reckoning, the curve lengthens with skill.

**Not ready to build it yet** — and when we are, it must also model:
- **Store randomness:** which dice/charms/favors the Fence offers (it shows **5 at a time**, drawn from the unlocked pool), how often, and the re-roll churn — a build only comes online if the shop *offers* the pieces. The sim must include the offer distribution, not assume the player can always buy what they need.
- **Unlock gating:** not every item is available from the start (see [`ACHIEVEMENTS.md`](./ACHIEVEMENTS.md)) — the sim must respect which items a given player has unlocked, since that bounds the builds they can assemble.

Until the sim exists, the numbers above are an *educated baseline*, not measured values.

---

## 11. Open questions

1. **Seed vs N1 tribute:** is $18 seed / $20 tribute too tight (near-broke after paying) or correctly tense? Sim will tell.
2. **Max stake = tribute** — or a flat per-night cap, or a % of bankroll? Affects how all-in the player can go.
3. **Heat cap** — with only 3 games, ×2.0 is the ceiling. Worth a bigger per-win step so streaks feel explosive?
4. **Price scaling basis** — night-number (predictable) vs tribute/bankroll (proportional)?
5. **Does surplus carry fully**, or does Vito skim interest on what you keep (tightening the buffer trap further)?
