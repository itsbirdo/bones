# Gambling Dice Game — Design Research

*What makes Cloverpit, Scritchy Scratchy, Gamble with Your Friends, and Balatro addictive but genuinely fun — and how to apply it to a dice roguelike.*

Compiled 2026-05-31. Claims below were fact-checked via adversarial verification (3-vote, ≥2/3 to kill). Confidence and sources are noted; disputed and refuted claims are called out honestly rather than smoothed over.

---

## TL;DR — The shared formula

All four games run on the **same core recipe**:

1. **A tight, fast core loop** you can understand in seconds (pull lever / play hand / scratch card / roll).
2. **Heavy "juice"** — layered animation + sound + feedback that *escalates with the payoff* — wrapped around that loop.
3. **A randomness-vs-skill balance where player agency meaningfully navigates RNG** rather than being subordinate to it. (Important nuance: none of these are "pure skill." Claims that Cloverpit "demands genuine skill" and that Balatro is "won on skill not luck" both *failed* verification. The honest position: **meaningful-but-not-dominant agency layered on top of RNG**.)
4. **Escalating tension** — rising stakes / deadlines / difficulty per run.
5. **"One more run" driven by build variety and theorycrafting**, not by financial hooks.

The ethical exemplar is **Balatro**: it uses gambling's *language and feel* while deliberately **exposing rather than exploiting** manipulative monetization. That distinction is the north star for an ethical design.

---

## Principle 0: "Juice" is your #1 lever for feel (and it's a craft, not decoration)

> **Juice = taking a game that already works and adding layers of satisfying animation and audio to improve its feel.** — *Game Developer*

- The classic reference is Vlambeer / Jonasson & Purho's **"Juice it or lose it"** (2012). A juicy game is full of *"little details, little moments of surprise and delight."* The famous demo took a flat Breakout prototype and "cranked it up to eleven" with particles, screen shake, and cheering crowds — same mechanics, wildly different feel. *(High confidence; sources: [Juice it or lose it (GDC/YouTube)](https://www.youtube.com/watch?v=Fy0aCDmgnxg), [Game Developer](https://www.gamedeveloper.com/design/squeezing-more-juice-out-of-your-game-design-))*

**But — two critical guardrails (high confidence):**

- **Juice cannot rescue weak mechanics.** *"You won't get good juice out of a tasteless fruit."* Build a core loop that's satisfying *naked*, then layer feel on top.
- **Juice must be contextual, not universal.** Screen shake, squash-and-stretch, and bounciness *"are only relevant in specific situations."* Your juice should **echo the core gameplay** — big payoff = big reaction; small win = small reaction. Industry guidance (MoreMountains *Feel*): use effects **"sparingly but violently."** Over-juicing everything flattens the signal and becomes a crutch (Wayline, *"Juice as a Crutch"*).

**→ For the dice game:** The single biggest determinant of whether your game feels good is how the dice *land, settle, glow, and pay out*. Tier your feedback so a 2x win and a 100x jackpot feel categorically different. Make the dice physically satisfying before you add a single charm or modifier.

---

## Game-by-game teardown

### 🎰 Cloverpit — slot machine as psychological horror

**Core loop:** Pull the lever on a slot machine to earn coins and pay off an **ever-increasing debt to an ATM "overlord"** before a deadline. Miss the deadline → a **trapdoor drops you to your death**, ending the run. *(High confidence; [PCGamesN](https://www.pcgamesn.com/cloverpit/slot-machine-roguelike), [Wikipedia](https://en.wikipedia.org/wiki/CloverPit))*

**What makes it work:**
- **Escalating audio-visual payoff chains.** *"The clanking of coins, the aural bombardment of multipliers and charm effects chaining together — and the big payouts — are a perfect cocktail for endorphins."* The reward feedback *compounds* as multipliers and charms combo.
- **Deckbuilder depth grafted onto a slot machine.** Reviewers credit it with injecting *"strategic depth reminiscent of the best deckbuilders"* into an otherwise RNG mechanic — charm selection and multiplier synergies give you something to optimize. *(High confidence; [Metacritic](https://www.metacritic.com/game/cloverpit/) — 90% of ~11,738 Steam reviews positive)*
- **Tension via debt + deadline + death.** The rising debt and the literal trapdoor turn slot-machine RNG into genuine dread and risk management — *"the tension of gambling [elevated] to a form of psychological horror."*

**Disputed:** Exactly how much *skill* it demands is contested — the "demands genuine skill" framing failed verification (0-3), and "RNG dominates" was split (1-2). Treat it as **RNG-forward with real but secondary agency**.

**→ Lesson:** A hard deadline + escalating cost + a real failure state turns idle gambling into *tension*. The synergy layer (charms/multipliers) is what converts a slot machine into a game you can get *better* at.

---

### 🃏 Balatro — the gold standard for both fun *and* ethics

**Core loop:** Build poker hands against escalating score thresholds ("blinds"), buying **Jokers** and other cards between rounds that warp the scoring rules into wild synergy engines.

**What makes it work:**
- **Easy to understand, deceptively deep.** *"Easy-to-understand gameplay and deceptively deep mechanics. With its hundreds of gameplay possibilities, it has fueled endless theorycrafting... as people tell themselves 'just one more run.'"* *(High confidence; [TheGamer](https://www.thegamer.com/balatro-most-fun-best-builds/))*
- **Agency navigates RNG (it doesn't eliminate it).** *"With eagle eyes and good gamesense, you can pick out the pieces you need to win big"* — but RNG still determines *which* pieces are offered. Frank Lantz's design essay: **"RNG feels fair because you always have agency."** (Note: the stronger claims — full-information card-counting making it "skill-based," and "won on skill not luck" — both *failed* verification, 0-3. Don't overclaim the skill ceiling.)

**Why it's the ethical exemplar (high confidence):**
- **No real-money gambling. No microtransactions. No premium currency. No loot boxes.** One-time purchase; **all content unlockable through play.** The only DLC ("Friends of Jimbo") is *free*.
- The creator (**LocalThunk**) **legally stipulated in his will that the Balatro IP can never be sold or licensed to any gambling company or casino** — binding after his death.
- He publicly skewered the hypocrisy of *real* real-money-gambling games being age-rated *lower* than Balatro: *"I'm way more irked at the 3+ for these games with actual gambling mechanics for children."*
- The deeper point (Inverse, TechRaptor): predatory games *"attach monetization structures to game loops which provide players with the illusion of control, while doing everything in their power to stack the deck in their favor."* **Balatro casts the player as *the house*** — full agency, no wagering — and thereby *exposes* the manipulative systems instead of running them.

> **Honest caveat:** Even Balatro is acknowledged to use *"the language of gambling to create a compulsive experience."* It avoids *financial* exploitation — not *psychological* compulsion. That's the line you're walking too.

**→ Lesson:** Maximum depth, maximum compulsion, **zero financial predation**. The "house" framing and "no money in the loop" rule are directly portable.

---

### 🎟️ Scritchy Scratchy — the idle/clicker scratch-card loop

**Core loop:** Buy scratch cards → scratch them (manual at first, **automatable as an unlock**) → decide whether to **reinvest winnings**. Most scratches lose. *(High confidence; [gaming.net](https://www.gaming.net/reviews/scritchy-scratchy-review-pc/), [The GAP Podcast](https://thegapodcast.com/2026/04/02/scritchy-scratchy-review/))*

**What makes it work:**
- **Quick-win dopamine, glossy juice.** *"You scratch, you lose, but you keep purchasing more cards"* — explicitly targeting *"that momentary high that comes with a quick win,"* reinforced by glossy visual feedback.
- **Variety as a content engine.** 10+ card types, *"each card has its own unique theme, effects and rewards."*
- **Roguelike meta-progression.** A **Prestige / "Jack Points"** upgrade tree layered on the incremental loop. Kotaku classes it as a *"scratch card incremental game."* *(Released March 18 2026; 95% of 4,896 Steam reviews positive.)*

**Disputed:** "Almost pure randomness with essentially no skill" *failed* verification (0-3) — the **reinvest-or-bank** and **upgrade-path** decisions provide the agency.

**→ Lesson:** Two things to steal: (1) the **reinvest-or-bank decision** turns a passive scratch into a real choice; (2) **automation-as-an-unlock** lets the early game be tactile and the late game be a satisfying optimization machine.

---

### 🎲 Gamble with Your Friends — social/party gambling *(thin evidence)*

**⚠️ Honesty flag:** *No claims about this game survived verification.* The angle returned mostly low-quality sources. So treat everything here as **unsubstantiated** and design from first principles instead:

- The presumed hook is **social/competitive dynamics** — bluffing, schadenfreude, table talk, and "I'll beat *you*" rivalry layered on top of gambling RNG. (Think the appeal of Liar's Dice / poker night, not solo slots.)
- If multiplayer matters to your vision, this is an **open research question** — worth a dedicated dig or a direct play session before committing.

---

## Cross-cutting principles for *your* dice roguelike

These synthesize the verified findings into build decisions.

### 1. Make the *naked* core loop satisfying first
Dice are even more RNG-forward than cards. Before any charms or modifiers, the act of **rolling, watching dice tumble, and seeing them settle** must feel good. Physics, weight, sound on landing, the pause before the result. *Then* layer juice.

### 2. Tier your juice to payoff magnitude
A small win: a soft chime, a gentle pop. A jackpot: screen shake, coin cascade, music swell, particle storm. *"Sparingly but violently."* The escalation *is* the dopamine — Cloverpit's "chaining multipliers" are the model.

### 3. Give RNG genuine agency — synergy-building is the engine
The dividing line between "fun" and "arbitrary" is whether the player feels they *navigate* luck. For dice, that means a rich layer of **agency tools**:
- **Rerolls** (spend a resource to re-roll some/all dice).
- **Dice modifiers / charms** (weighted dice, "this die counts double," "6s explode," set-collection bonuses).
- **Build synergies** that combo (Cloverpit charms, Balatro Jokers) so theorycrafting emerges.
- **Full information** where possible (show odds, show what's in your pool) — fairness comes from *agency*, not from hidden-odds tricks.

> *Open question worth prototyping:* dice are more RNG-forward than cards, so you may need **more** agency tools than Balatro to keep it feeling skill-expressive. Tune the reroll/modifier density up until losses feel earned, not arbitrary.

### 4. Build tension with stakes, deadlines, and a real failure state
Cloverpit's debt + deadline + trapdoor is the template. Rising score thresholds (Balatro's blinds) work too. The run must be able to **end in failure**, or there's no tension and no "one more run."

### 5. Drive "one more run" with *build variety*, not financial hooks
The replay engine is **combinatorial depth** — hundreds of charm/modifier combinations, unlockable dice types, escapable local maxima. Players return to try a *new build*, not to recover *money*. Variety (Scritchy's 10+ card types, Balatro's 150+ Jokers) is the content flywheel.

### 6. Layer progression: within-run *and* meta — but mind carryover
- **Within-run:** "match-to-match" progress (Cloverpit's praised loop) — your build compounds across a single run.
- **Meta:** an unlock tree (Scritchy's Prestige/Jack Points; Balatro's unlockable decks/Jokers) that adds *variety and options*, not raw power that trivializes difficulty.
- *Open question:* how much should carry between runs? Too much trivializes the challenge; too little feels grindy. Lean toward **unlocking options, not power**.

### 7. Be Balatro-ethical by design — draw the line on purpose
- **No real money in the loop.** No microtransactions, no loot boxes, no premium currency, no real-money wagering.
- **One-time purchase, all content earnable through play.**
- **Cast the player as the house / give full agency** — expose the mechanics, don't run a stacked deck against them.
- Accept the honest tension: you *are* using gambling's language to create compulsion. Avoiding *financial* harm is the bar you can actually hit — so hold it deliberately and don't drift.

---

## Open questions to resolve before building

1. **Multiplayer:** Is "Gamble with Your Friends"-style social play part of the vision? If so, it needs dedicated research — current evidence is empty.
2. **Skill-to-RNG ratio for dice specifically:** Dice are more random than cards. How many agency tools (rerolls, modifiers, set-collection) are needed to feel fair? *Prototype and playtest this.*
3. **Meta-progression carryover:** What persists between runs to sustain "one more run" without trivializing difficulty?
4. **Where's the ethical line** between Balatro-style compulsive-but-non-predatory and manipulative — given even Balatro creates compulsion?

---

## Caveats on this research

- Sources skew to **games-media reviews and design essays**, not peer-reviewed work. "Dopamine/endorphin" language from reviewers is **metaphor, not neuroscience**.
- **Gamble with Your Friends: zero verified claims** — that section is speculative.
- **Skill-vs-luck ratios are genuinely disputed** for all four games. Several strong "it's skill-based" claims were *refuted* in verification. The defensible position is "meaningful-but-not-dominant agency over RNG."
- **Scritchy Scratchy (Mar 2026) and Cloverpit (late 2025) are very recent** — community consensus may still be shifting.
- **Balatro's PEGI rating history is muddled** across sources; the specific "18+" claim was split (1-2) and should be re-verified before being stated as fact.
- The **"juice" principles are stable and well-established** (2012 Vlambeer lineage) and not time-sensitive.

---

### Key sources
- *Juice it or lose it* — Jonasson & Purho ([GDC Vault](https://www.gdcvault.com/play/1016487/Juice-It-or-Lose), [YouTube](https://www.youtube.com/watch?v=Fy0aCDmgnxg))
- *Squeezing more juice out of your game design* — [Game Developer](https://www.gamedeveloper.com/design/squeezing-more-juice-out-of-your-game-design-)
- Cloverpit — [PCGamesN](https://www.pcgamesn.com/cloverpit/slot-machine-roguelike), [Wikipedia](https://en.wikipedia.org/wiki/CloverPit), [Metacritic](https://www.metacritic.com/game/cloverpit/)
- Balatro — [TheGamer (builds)](https://www.thegamer.com/balatro-most-fun-best-builds/), [Inverse (gambling/addiction design)](https://www.inverse.com/gaming/balatro-poker-gambling-addiction-design-roguelike-rec), [Game Developer (no-gambling licensing)](https://www.gamedeveloper.com/business/balatro-creator-says-he-will-never-let-it-be-licensed-for-gambling-even-after-death), [ScreenRant (PEGI)](https://screenrant.com/balatro-creator-points-out-hypocrisy-of-pegi-rating/)
- Scritchy Scratchy — [gaming.net](https://www.gaming.net/reviews/scritchy-scratchy-review-pc/), [The GAP Podcast](https://thegapodcast.com/2026/04/02/scritchy-scratchy-review/)
