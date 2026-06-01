# Alley Dice — Game Design Doc

*A single-player, luck-based dice roguelike. You owe a mobster money. You pay it back rolling dice in the back alleys, one game at a time — upgrading your dice and your hustle to survive escalating stakes.*

Built on the principles in [`RESEARCH.md`](./RESEARCH.md): a tight core loop, juice tiered to payoff, **agency that navigates RNG**, escalating tension via a debt deadline, and "one more run" driven by build variety — never by real money.

---

## The core loop (your spec, locked in)

1. **Pick up dice** — select which dice go in your hand/cup (this is where upgrades matter).
2. **Shake** — build tension; the shake animation/sound intensity hints at "heat" (streak multiplier).
3. **Roll out one die at a time** — sequential reveal. *This is your single most important juice moment.* Each die landing is a beat; the last die is the payoff. Tier the feedback (see §6).
4. **Score → win or lose** — resolve against the current game's rules.

Everything below feeds this loop.

---

## Part 1 — Street dice games (the research)

These are real games gangsters, soldiers, and street hustlers actually played. Each one adds a **new decision layer**, so as the player climbs the ladder the *skill ceiling* rises while the game stays luck-based. That progression of complexity is your difficulty arc.

| # | Game | Dice | New decision layer it teaches | Vibe |
|---|------|------|------|------|
| 1 | **Cee-lo (Four-Five-Six)** | 3 | Reading instant combos, push-or-bank | Iconic back-alley game |
| 2 | **Street Craps** | 2 | Point-chasing, pass / don't-pass bet | The sidewalk classic |
| 3 | **Chuck-a-luck (Birdcage)** | 3 | *Bet placement* before the roll, multiplier chase | Carnival hustler |
| 4 | **Hazard** | 2 | Choosing your "main," reading odds | Old-money backroom |
| 5 | **Farkle / "Six-Dice Push"** | 6 | Push-your-luck: when to bank vs reroll | Deepest skill game |
| 6 | **Liar's Dice (boss)** | 5 | Bluffing & probability vs the mobster himself | The final showdown |

### 1. Cee-lo / Four-Five-Six — *the flagship starter*
Three dice, banker vs player, resolves in seconds — perfect for teaching the dice-by-dice reveal.
- **Instant outcomes** (the "headcracks"): **4-5-6** = auto-win; **triples** = auto-win; **1-2-3** = auto-loss.
- **A pair + a single** sets your *point* (the odd die, 1–6). Higher point beats lower.
- Anything else (e.g. 1-2-4) = **re-roll**, which naturally creates repeated dice-by-dice reveals = repeated tension beats.
- *Why it's tier 1:* fast, readable, and the instant-combos are a built-in jackpot moment.
- Sources: [Cee-lo — Wikipedia](https://en.wikipedia.org/wiki/Cee-lo), [Play Party Game](https://playpartygame.com/dice-games/ceelo-rules/)

### 2. Street Craps — *the point game*
Two dice, you're the shooter.
- **Come-out roll:** 7 or 11 = win; 2, 3, 12 = lose; anything else becomes your **point**.
- Then re-roll until you hit your point again (**win**) or roll a 7 (**lose**).
- Adds a **pass / don't-pass** style bet so the player makes a call before rolling.
- *Why it's tier 2:* introduces a multi-roll arc within a single hand — tension stretches over several reveals.
- Sources: [Street Craps — PokerNews](https://www.pokernews.com/casino/casino-terms/street-craps.htm)

### 3. Chuck-a-luck (Birdcage) — *the multiplier chase*
Three dice in a cage; you **bet on numbers before the roll**.
- Single-number bet pays **1:1 / 2:1 / 3:1** if your number shows on one / two / three dice.
- **"Any triple" pays 30:1** — your first Cloverpit-style jackpot moment.
- Side bets: High (11+), Low (≤10), Odd, Even, Field.
- *Why it's tier 3:* the player now commits *before* the roll and chases big multipliers — pure dopamine, and the bet-placement is a real decision.
- Sources: [Chuck-a-luck — Wikipedia](https://en.wikipedia.org/wiki/Chuck-a-luck), [Dice Game Depot](https://www.dicegamedepot.com/dice-n-games-blog/chuckaluck-dice-game-rules/)

### 4. Hazard — *the historical deep cut*
The medieval ancestor of craps (the word comes from Arabic *al-zahr*, "die"). Two dice.
- You pick a **main** (5–9). Roll: a "nick" wins instantly, "out" loses instantly, anything else is your **chance** — roll until chance (win) or main (lose) comes up.
- *Why it's tier 4:* **choosing your main** is a genuine odds decision — the first time skill clearly bends the math. Great gritty period flavor.
- Sources: [Hazard — Wikipedia](https://en.wikipedia.org/wiki/Hazard_(game)), [Wizard of Odds](https://wizardofodds.com/games/hazard/)

### 5. Farkle / "Six-Dice Push" — *the skill peak (à la KCD2)*
Six dice, push-your-luck. **This is the game that best showcases special dice** — directly modeled on Kingdom Come: Deliverance 2.
- Roll 6 dice, set aside scoring dice (1s = 100, 5s = 50, three-of-a-kind = face×100, straight = 750…), re-roll the rest.
- **Bust** = roll no scoring dice → lose everything banked that turn.
- The core tension: **bank now or push for more?** This is the single most skill-expressive loop and the heart of your "arc" feel.
- *Why it's tier 5:* highest skill ceiling, highest variance, the natural home for your deepest dice upgrades.
- Sources: [KCD2 Dice (Farkle) — Reality Remake](https://www.realityremake.com/articles/ultimate-guide-to-the-dice-game-in-kingdom-come-deliverance-2-farkle), [KCD Wiki](https://kingdom-come-deliverance.fandom.com/wiki/Dice/KCD2)

### 6. Liar's Dice — *the boss fight*
Five dice under a cup, **you vs the mobster**. Escalating bids about how many dice of a value are on the table; call "liar" to challenge.
- A bluffing/probability duel — the perfect climactic, debt-clearing showdown against a *named character* rather than a faceless table.
- Source: [Liar's dice — Wikipedia](https://en.wikipedia.org/wiki/Liar%27s_dice)

*(Bench options for variety/events: **Ship Captain Crew** — sequential 6-5-4 then high cargo; **Bunco** — round target-number game. Good for side characters or daily mini-events.)*

---

## Part 2 — Game mechanics

### 2.1 The macro structure: the Debt (your tension engine)

This is your Cloverpit "ever-increasing debt to the ATM overlord," made literal and human.

- A mobster — call him **"The Cobbler"** (he breaks legs) — sets your debt, e.g. **$2,500**.
- You have **N nights** (e.g. 7) to pay. **Interest accrues each night** (e.g. +10%) if the balance stands → the target *grows while you sleep*. This is the escalation that forces bigger risks.
- **Night structure:** each night you can play a limited number of games (an energy/"the alley closes at dawn" limiter). End the night → the Cobbler's collector visits → pay what you owe that installment or lose a "life" (a beating, an upgrade confiscated, a night lost).
- **Miss the final deadline → game over** (the trapdoor moment). Pay it off → you've earned your freedom, unlock a harder loop (NG+ with a bigger debt and a tougher Cobbler).

> This gives you *two* nested arcs, exactly like the research recommends: the **macro arc** (the debt deadline across the whole run) and the **micro arc** (the streak/push within a single sit-down).

### 2.2 The economy & the "basic activity"

**The basic activity (your $1 safety net).** You asked me to come up with one. Three options, recommendation first:

1. **★ "Run the Shell Game" (recommended).** You stand on the corner and hustle a passerby with three cups and a ball — a 5-second skill-lite minigame (track the cup) that nets **$1** on success. It's *thematically perfect* (you're the small-time hustler), it's tactile, and it can fail occasionally so it never feels free — but it's reliable enough to always crawl back from $0. As you upgrade, it can auto-run or pay more.
2. **"Odds or Evens."** Toss a single die against the wall vs a drunk; call odd/even for $1. Pure and fast, fits the dice theme, but less distinct from the real games.
3. **"Scrounge."** Dig through the gutter/trash for dropped coins — a tiny idle/clicker tap (nods to *Scritchy Scratchy*). Cheapest to build, but the least "alley-hustler" cool.

The basic activity is deliberately **grindy and low** — it's the floor that guarantees you can never hard-lose, but it's slow enough that you *want* to graduate to the dice games. (This is the *Scritchy Scratchy* "always one more cheap pull" loop kept honest.)

**Unlock ladder (your thresholds, extended).** Games unlock by bankroll, and each new tier has higher stakes + higher variance:

| Bankroll | Unlocks | Typical stake | Player edge (base) |
|---|---|---|---|
| $0 | Basic activity | — | +$1 / success |
| **$3** | **Cee-lo** | $1–3 | **~60% win** (favored) |
| **$25** | **Street Craps** | $5–15 | ~52% |
| $100 | Chuck-a-luck | $10–40 | ~48% (multiplier chase) |
| $400 | Hazard | $25–100 | ~50% (skill-dependent) |
| $1,500 | Six-Dice Push (Farkle) | $50–300 | swingy / skill-driven |
| can ante the debt | Liar's Dice (boss) | all-in | the showdown |

**The difficulty arc, expressed as math.** Tier 1 is **player-favored (~60%, positive EV)** so grinding works and the player feels powerful early. Each higher tier tightens toward neutral or slightly negative EV. By the mid-game, raw rolling is no longer enough — **you must lean on dice upgrades, streak multipliers, and smart push/bank decisions to stay ahead.** That's how a luck game develops a skill curve without stopping being a luck game. (Matches the research: *meaningful-but-not-dominant agency over RNG.*)

> Tuning note: if a game pays 1:1 and you win 60%, EV per $1 bet = +$0.20. Across many rolls that's a steady but slow climb — fine for tier 1. For tiers where EV ≤ 0, the player's *only* path up is upgrades + streaks. Keep that lever explicit.

### 2.3 The micro arc: Heat (streak multiplier)

Your Cloverpit "chaining multipliers," reframed for the street:

- Consecutive wins build **Heat**: ×1.2 → ×1.5 → ×2 → ×3… applied to payouts.
- **Banking** (walking away) locks your winnings and resets Heat. **A loss wipes Heat.**
- The shake animation and crowd noise intensify with Heat — so the *feel* escalates with the stakes (juice tied to payoff).
- The decision "push my streak or bank it?" is the within-session tension that makes every game a mini-arc, not just a coin flip.

### 2.4 The Cheating / Suspicion system (theme + tension goldmine)

Loaded dice and sleight-of-hand are *part of the alley fantasy* — but they're risky.

- Many upgrades are **cheats** (weighted dice, palmed swaps, shaved edges). Using them raises a hidden **Suspicion** meter at the table.
- Cross a threshold → the other players/banker **call you out**: best case you forfeit the pot, worst case the Cobbler's goons take an upgrade or a night.
- Creates a constant risk/reward read — *how greedy can I get before they notice?* — which is genuine, fun tension and 100% on-theme.
- Counter-upgrades exist (misdirection, a lookout, greasing the banker) to lower Suspicion → a whole upgrade sub-tree.

### 2.5 "One more run" without money hooks

Per the research, replayability comes from **build variety, not financial pressure**:
- Dozens of dice and upgrades → many viable "builds" (a weighted-dice build, a reroll-control build, a high-Heat gambler build, a cheater build).
- Randomized shop offerings between nights (roguelike).
- NG+ with bigger debts, new games, rarer dice.
- **No real money. No microtransactions. No loot boxes.** This is the Balatro line — hold it on purpose.

---

## Part 3 — Dice upgrades & advantage items

Modeled on KCD2's 40+ special dice + badges, organized into a tree so distinct **builds** emerge. Each die equipped is one of (typically) six slots, so loadout choice matters.

### A. Weighted / Loaded dice — *bend the probabilities*
| Item | Effect | Risk |
|---|---|---|
| **Lucky Six** | Rolls 6 more often (e.g. 30% vs 16.7%) | +Suspicion |
| **Devil's Bones** | Rolls 1 more often — useless alone, but *combos* with "low is good" games/upgrades | +Suspicion |
| **Even Steven** | Biased toward even faces | mild +Suspicion |
| **Mirror Die** | Can't roll the same number twice in a row | — |
| **Heavy Die** | Never rolls its lowest face | high +Suspicion |
| **Two-Face** | Only shows two values (e.g. 1 and 6) — high variance | high +Suspicion |

### B. Manipulation / Control — *agency over the roll (the "fair" tools)*
| Item | Effect |
|---|---|
| **Reroll Token** | Re-roll one die after seeing it (consumable or per-game charge) |
| **Nudge Die** | Adjust one die ±1 after the roll |
| **Set Die** | Once per game, set one die to a chosen face (very strong, rare) |
| **The Cooler** | Lock a die so it keeps its value on the next reroll (great in Farkle) |
| **Second Wind** | One free "un-bust" per Farkle turn |
| **Mulligan Cup** | Re-roll the *entire* hand once per game |

### C. Economy / Payout — *scaling*
| Item | Effect |
|---|---|
| **Golden Die** | +25% on all payouts from hands including this die |
| **Hot Hand** | Heat multiplier builds faster / decays slower |
| **High Roller** | Bigger max bet, bigger payouts, bigger losses |
| **Insurance Chit** | Recover 50% of a single loss per night |
| **Vig Skimmer** | Take a small cut of every pot even on a loss |
| **Compounder** | Each consecutive win this night adds flat bonus cash |

### D. Information — *fairness via knowledge (Balatro-style transparency)*
| Item | Effect |
|---|---|
| **Marked Dice** | See the next die's result a beat before it lands (peek) |
| **Odds Reader** | Show live win probability before you commit a bet |
| **Tell Reader** | (Liar's Dice / banker games) reveal opponent bluff likelihood |
| **Counter's Eye** | Track which faces are "due" / history of recent rolls |

### E. Cheats — *high power, high Suspicion*
| Item | Effect | Risk |
|---|---|---|
| **Palmed Die** | Swap in a loaded die mid-shake | big +Suspicion |
| **Shaved Edges** | Subtle bias, low detection | small +Suspicion |
| **The Switch** | Replace your whole cup once per game | huge +Suspicion |
| **Lookout** *(counter)* | Lowers Suspicion gain | — |
| **Greased Palm** *(counter)* | Bribe the banker; Suspicion threshold raised this game | costs cash |
| **Smooth Talker** *(counter)* | Talk your way out of one call-out per night | — |

### F. Curse / Trade-off dice — *risk builds (the spice)*
| Item | Effect |
|---|---|
| **Snake Eyes Pact** | Huge payout multiplier, but rolling a 1 anywhere = instant bust |
| **All-or-Nothing** | Double winnings, but a loss costs double |
| **Bloody Knuckles** | +Heat gain, but Suspicion never resets |
| **The Gambler's Curse** | Can't bank below ×3 Heat — forces you to push |

### G. Charms / Trinkets (non-dice passives — Cloverpit's charms)
- **Rabbit's Foot** — first loss each night is forgiven.
- **Loaded Coin** — re-flip the basic activity once.
- **Brass Knuckles** — survive one beating from the collector.
- **Pawn Ticket** — sell back a die for partial cash in emergencies.
- **The Cobbler's Favor** — one night of zero interest.

---

## How it all ties to the research

| Research principle | How this design delivers it |
|---|---|
| Naked core loop must satisfy first | The pick-up → shake → **dice-by-dice reveal** → score beat, juiced before any upgrades |
| Juice tiered to payoff | Reveal intensity scales with stake + Heat; instant-combos (4-5-6, triples, 30:1) are jackpot moments |
| Agency navigates RNG | Reroll/nudge/set dice, bet placement, push-or-bank, choosing your main — luck you can *steer* |
| Tension via deadline + failure state | The Cobbler's debt + nightly interest + final deadline = the trapdoor |
| "One more run" via build variety | Dozens of dice/charms → many builds; randomized shops; NG+ |
| Ethical, non-predatory | No real money, no MTX; the *Suspicion* system even dramatizes that cheating has consequences |

---

## Open questions for you

1. **Setting/era:** 1940s noir back-alley? Prohibition-era? Gritty modern? (Affects art, music, the games' flavor — Hazard leans period, Cee-lo leans timeless-urban.)
2. **Skill-to-luck dial for dice specifically:** dice are more random than cards — do you want the reroll/control upgrades to be *common* (high agency, feels fair) or *rare* (high luck, feels wild)? This is the #1 thing to prototype.
3. **Run length:** how many nights to clear the debt — a tight ~30-min run, or a longer grind?
4. **Does the Cheating/Suspicion system appeal?** It's optional but I think it's the most thematically rich tension lever here.

Want me to prototype the **core loop (pick up → shake → dice-by-dice reveal → score)** for Cee-lo first, so you can feel the juice before we build the meta?
