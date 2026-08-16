# BONES — Achievements & Unlocks

Not every die, charm, or trinket is available from the start. New items are **unlocked by playing** — the catalog reveals itself over many runs, giving a **collection / discovery arc** on top of the roguelike. Companion to [`GAME_DESIGN_SPEC.md`](./GAME_DESIGN_SPEC.md) §11 (the catalog) and [`ECONOMY.md`](./ECONOMY.md).

---

## How unlocks work

- **Account-level & permanent.** Earned once, kept forever across all runs.
- **Unlocking ≠ owning.** An unlock only adds the item to the pool the **Fence can offer** (5 shown at a time). You still have to be *offered* it and **buy it with cash**. Unlocks widen your *possible* builds; they don't hand you power. (Research: meta-progression should grant **options, not raw strength**, so difficulty stays meaningful.)
- **Earned mid-run, buyable next round.** Many achievements **fire the moment they happen mid-round** — hit a big pot, roll a 6-6-6, bust, etc. The reward becomes purchasable **from the next round onward** (you can't grab it the instant it pops mid-game), and stays available forever after.
- **Not all achievements are "good."** Plenty are neutral or outright **failures** — *Got busted*, *Went broke*, *Got whacked* — and they still grant items. Losing *is* progression: a bad run still ends with something new for the next one.
- **Discovery, no nags.** Unlocks land as a quick noir beat ("a fence slides you a new pair of bones…"), not a popup. Browse what you've unlocked in the Bag.

---

## Starting core pool (a new account's first 3 — decided 2026-06-12)

Deliberately small and **deliberately low-powered**: one mild lean, one modest charm, one favor — enough to taste each system, never enough to feel armed. The real cheats arrive fast through the first run's natural beats (first loss, fifth game). This is **only the very-first-time set**; the pool grows steadily as you unlock.

| Type | Item | Why it's a starter |
|---|---|---|
| Loaded | **Shaved Edge** | the gentlest lean in the catalog — a first taste of crooked, nothing more |
| Charm | **Gilded Die** | a clean payout boost — shows charms aren't cheats, and helps wins pay |
| Favor | **Lookout** | bust management from day one — the player meets the Suspicion valve early |

Everything else is locked and earned below. (Your honest default **Bone dice** fill any empty cup slots for free.)

---

## Unlock table

Conditions are **tuning placeholders** (exact thresholds to balance later; the live values are in `web/engine.js`, which this table mirrors — 2026-08 sync). Grouped by flavour.

### Early staples (fast, fill out the basics from the 3-item core)
| Achievement | Unlocks | Notes |
|---|---|---|
| **Lose your first game** | **Snake Killer** | you met the bottom; learn to dodge 1-2-3 (the intuitive first real cheat) |
| **Play your 3rd game** | **Even Steven** | a gentle extra cheat, almost immediately |
| **Play your 5th game** | **Lucky Six** | the readable "make big numbers" cheat, earned within run one |
| **Play your 8th game** | **Cold Read** | you're a regular now; start reading the marks (and your own odds) |
| **Win your first game** | **Streak Charm** | a first pot, and a reason to chase the next one |
| **Win 3 games (lifetime)** | **Hot Hand** | they start calling you the hot hand |
| **Make your first Fence purchase** | **Pawn Ticket** | basic emergency cash (he buys back, too) |
| **Use a re-roll for the first time** | **Reroll Bone** | a taste of direct control |
| **Lose a game to 1-2-3** (auto-loss) | **Second Wind** | dodge the auto-loss next time |
| **Lose 5 games (lifetime)** | **Rabbit's Die** | a refund cushion for the punch-drunk |

### First-times (early, generous — teach the systems)
| Achievement | Unlocks | Notes |
|---|---|---|
| **First death** (any loss — broke or whacked) | **Two-Face** | "Nothing left to lose." The classic first-death reward. |
| **Get busted for the first time** | **Smooth Talker** | you learn to talk your way out |
| **First 4-5-6 (headcrack)** | **Headcracker** | lean into the jackpot you just felt |
| **First triple** | **Matchmaker** | a pair-completing cheat |
| **First 6-6-6** | **The Magnet** | the triple-maker — chase the top jackpot |
| **Win a game using a re-roll** | **The Cooler** | reward learning the re-roll tool |

### Survival depth (reach further into a run)
| Achievement | Unlocks | Notes |
|---|---|---|
| **Reach Night 3** | **The Finisher** | the marks' bones are turning; turn your best point into a headcrack |
| **Reach Night 4** | **High Roller** | you'll need a stronger cheat by now |
| **Survive 3 nights in one run** | **Rabbit's Foot** | a deeper safety net |
| **Reach Night 5** | **The Sequencer** | a 4-5-6 manufacturer for the deep end |
| **Reach Night 6** | **Insurance Chit** | cushion the brutal late nights |
| **Reach the Reckoning (1st time)** | **The Spoiler** | you've seen Vito's bones; learn to spoil them |
| **Reach the Reckoning 3 times** | **Marker Shaver** | chip at the debt itself |
| **Beat Vito — win a run** | **Set-Bone** | the ultimate control die, fittingly the hardest unlock |

### Feats (skill / build flexes)
| Achievement | Unlocks | Notes |
|---|---|---|
| **Win 10 games (lifetime)** | **The Nudge** | your hands have learned the pavement *(replaced "Hone any die to Lv3" — honing removed 2026-08)* |
| **Clear a night's debt from a single game's winnings** | **Point Sharp** | reward one huge win |
| **Run a full cheat cup (3 loaded dice) in a game** | **Vig Skimmer** | embrace the crooked build |
| **Re-roll the Fence 3+ times in one night** | **Lucky Cigarette** | a free re-roll for the shop-hunter |
| **Win a single pot of $100+ in one game** (fires mid-round) | **High Roller's Clip** | a big-score flex → bigger stakes |
| **Clear a $250+ Collection** | **Vito's Favor** | you moved real money *(was "$1,000+" — the tuned schedule tops at $290)* |
| **Win a second run** | **Mulligan Cup** | a powerful re-roll for the practiced |

### Failures & deaths (lose your way to new toys)
| Achievement | Unlocks | Notes |
|---|---|---|
| **Go broke (lose by running out of money)** | **Loaded Coin** | the 50/50 bailout — thematically perfect |
| **Get whacked (miss a Collection)** | **Brass Knuckles** | survive one missed Collection next time |

### Deferred: the post-launch unlock wave (2026-08)
The **Curse category** plus **Greased Palm** and **Cooler Head** sit out the launch catalog (one less concept class on day one — see SPEC §11). Their achievements ship with them, ready-paired:

| Achievement | Unlocks | Notes |
|---|---|---|
| **Survive your first night** | **Greased Palm** | reward clearing a Collection; a clutch bribe |
| **Get busted twice (lifetime)** | **Cooler Head** | you learn to keep your cool |
| **Get busted 3 times (lifetime)** | **Bloody Knuckles** | you stopped fearing the heat |
| **Die 3 times (lifetime)** | **All-or-Nothing** | a high-variance swing for the desperate |
| **Die 5 times (lifetime)** | **Snake Eyes Pact** | bigger swing, bigger risk |
| **Die 10 times (lifetime)** | **Gambler's Curse** | for players who keep flaming out — lean in |

*(Retired with the 2026-08 tie-rule change: "Win a full night playing completely clean → The Equalizer" — ties go to the banker permanently now, so there is no old rule to buy back.)*

---

## Design notes

- **Pace the reveals:** front-load generous, frequent unlocks (first-times, first death) so new players feel the catalog blooming fast; back-load rare feats (beat Vito, win clean) for long-term chase.
- **Every unlock should change a build, not just pad a number** — favour items that open a *new line of play* (a new cheat angle, a new safety valve) over straight upgrades.
- **Failure-gated unlocks are deliberate generosity:** a player on a losing streak keeps getting new toys, so "I lost but I unlocked X" softens the sting and pulls the next run.
- **Unlocks feed the Fence's offer pool**, which the eventual balance **sim must model** (a wider pool dilutes the chance of being offered any *specific* item — ECONOMY §10).

---

## Open questions

1. **Core pool size:** 3 is intentionally lean (2 cheat dice + a charm). Too punishing for a first-timer, or correct? Could seed a 4th (a Favor) if early busts frustrate.
2. **Do unlocks ever gate by *difficulty/NG+* tier**, or purely by these feats?
3. **Should a few unlocks be *run-scoped* surprises** (found mid-run, like Cloverpit's drawers) rather than all account-level?
4. **Anti-frustration:** should the very first unlock be guaranteed within run 1 (e.g. first death) so everyone sees the system immediately? (Currently yes — first death is near-inevitable.)
5. **Thresholds** for all the cumulative ones ($X earned, N deaths) — set during balancing.
