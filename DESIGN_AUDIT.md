# BONES: Design Audit (2026-06-11)

A full review of the design documentation: game logic, mechanics, economy, game theory, and
playability. **Scope: docs only** (GAME_DESIGN_SPEC, ECONOMY, LEVELS_AND_FLOW, ACHIEVEMENTS,
CATALOG, NARRATIVE, ceelo, RESEARCH, README, archive/DESIGN). Unity code and implementation were
not reviewed, with one exception: the `balance/squeeze-earlier` commit message was read because it
contradicts the docs (see finding D1).

Verdict up front: **the design is coherent, well-researched, and the math that exists is correct.**
The identity (high luck, cheat-to-win, finite noir campaign) is consistent across every document,
which is rare. The serious problems are (a) two systems whose definitions contradict each other
across docs (Suspicion, the debt model), (b) several game-theory degeneracies where the "decision"
the design celebrates has a single dominant answer, and (c) a first-run difficulty wall that the
recent squeeze-earlier change makes worse than the docs intend.

---

## 1. What was verified and checks out

The numeric backbone in ECONOMY.md was re-derived independently. All of the following are correct:

- **216-outcome classification:** 12 instant wins, 6 instant losses, 90 points (15 per point
  value, uniform), 108 re-rolls (exactly 50%). Correct.
- **Banker win probabilities by tie rule:** ties-to-banker = **56.25%** (exactly 9/16), tie
  probability = 11.57%, ties-to-push win = 44.68%, ties-to-mark loss = 55.32%. All correct.
- **avgPayout mix:** at 56% win the mix is ~80% point / ~10% 4-5-6 / ~10% triple, giving ~1.30x.
  At Vito's 15%, instant wins are 74% of wins, giving ~2.11x. Both correct.
- **EV table (§4):** +0.27S at Night 1 through -0.53S at Vito. All four rows re-derived and
  confirmed.
- **The 11% floor:** instant wins are 11.11% of decisive rolls; 11.11% + (83.3% point rate x ~5%
  point-battle wins) = ~15.3% at Vito. Internally consistent.

The structural design choices are also sound and well-grounded in RESEARCH.md: one game only,
banker seat as the early edge, two stacked difficulty engines, finite narrative win, juice as the
comic language, failure-gated unlocks, options-not-power meta. The "buffer trap" (overshoot the
tribute or die next night) is a genuinely good piece of economy design.

---

## 2. Critical findings: contradictions and undefined systems

### C1. Suspicion is defined two incompatible ways (the biggest spec bug)

- **Stateless model:** SPEC §9.2 and ECONOMY §5.1 define Suspicion as the *static per-game sum of
  the bust-% of equipped cheat dice*. "Bust % depends only on your cheat loadout, not on The
  Squeeze." No memory between games.
- **Accumulating model:** LEVELS §4 says "Suspicion accrues across the night's games (the same
  marks watch you all night) and resets next night... decays when you play clean." The LEVELS §7
  state table lists Suspicion scope as "the night (builds across its 3 games)." SPEC §9.2 itself
  says "a dread that builds across the night as they keep cheating."
- The catalog is split too: **Cooler Head** ("faster decay") and **Bloody Knuckles** ("your bust %
  never cools") only make sense in the accumulating model; the per-die bust-% columns only make
  sense in the stateless model.

This is not cosmetic: the two models produce different games. **Recommendation: adopt the
accumulating model** (per-die base bust-% as the *rate of accrual*, decaying on clean games),
because it fixes finding G3 below (Lay Low and Favors are otherwise never worth using), makes the
"dread builds across the night" juice honest, and creates a real push-your-luck arc inside a
night. Whichever you pick, update all four documents to one definition.

Also undefined: **what "forfeit the staked pot" means on a bust.** Do you lose only the winnings
(win becomes a wash) or stake plus winnings (win becomes a loss)? The EV difference is ~2x. Define
it.

### C2. The Reckoning has no money model

SPEC §6.5 never says what you stake in the three Vito games, whether bankroll matters on Night 7
at all, or whether "broke" can still kill you there. Two further wording problems:

- "You always play all three" contradicts "win 2 of the 3": a best-of-3 normally ends 2-0. If a
  dead third game is intended, say why (it is anticlimactic as written); if not, fix the wording.
- If bankroll is irrelevant at the Reckoning, then Night 6 surplus has value *only* via the Night-7
  Fence. That is fine, but it should be a stated design decision because it changes Night 6
  strategy (spend everything before the finale).

**Recommendation:** the marker itself is the stake; no cash stakes at the Reckoning; bankroll's
only Night-7 use is the Fence. State it in §6.5.

### C3. The debt is a fixed table, but items assume a debt formula

The tribute schedule is hardcoded ($20...$2,400), yet **Marker Shaver** ("debt grows a little
slower"), **Vito's Favor** ("skip one night's interest"), and ECONOMY open question 5 (interest
skim on surplus) all assume a generative model with principal and interest. As specced, neither
trinket can function.

**Recommendation:** define tribute as a formula, e.g. `T(n) = T1 x g^(n-1)` with g ~ 2.55 (which
reproduces the current table almost exactly). Marker Shaver lowers g; Vito's Favor sets one
night's growth to 1.0. The narrative already calls it "principal + interest," so the fiction is
ready for it.

### C4. Cup slots, charm slots, and proc resolution are undefined

The build layer is the heart of the game and its resource constraints are not specified:

- Cee-lo rolls exactly 3 dice, and the achievement "run a full cheat cup (3 loaded dice)" implies
  the cup is 3 slots. But several **Charms are named as dice** (Rabbit's Die, Gilded Die) with
  proc rates, while others are clearly passive trinket-like (Hot Hand, Point Sharp). Does Gilded
  Die occupy one of the 3 rolled slots? If yes, that is a great tension (a payout die costs you a
  cheat die); if no, what limits how many charms you can stack? SPEC §12.4 ("your cup... plus
  equipped charms") implies separate charm slots with **no stated cap**. Without a cap, charm
  stacking is strictly additive power with no opportunity cost, which collapses build decisions.
- **Proc ordering and stacking is undefined.** Matchmaker copies another die's face; Lucky Six
  rewrites low faces; The Sequencer completes 4-5-6; The Magnet completes triples. When several
  fire on one throw, what order do they resolve in, and can two rewrite the same die? The
  outcome-first resolver in code presumably has an answer; the spec should own it, because order
  changes item value significantly (e.g. Matchmaker after Lucky Six is much stronger than before
  it).

**Recommendation:** 3 dice slots + N charm slots (start N=2, maybe expandable by a trinket), and a
documented resolution order (suggest: face-rewrite dice in cup order, then completion dice
(Sequencer/Magnet), then payout charms at settle).

### C5. The Fence with a 3-item pool and 5 display slots

A fresh account's pool is 3 items but the shop "shows 5 at a time." Undefined: duplicates? blanks?
hone-offers filling the gap? Also undefined whether the shop can offer dice you already own (and
whether owning duplicates is allowed). Small, but it is literally the first shop a player ever
sees. Recommend: shop shows min(5, pool size) cards with hone offers for owned dice padding the
remainder; that also quietly teaches honing.

---

## 3. Game-theory findings

### G1. Late-game stake sizing collapses to "bet the maximum"

The docs repeatedly call stake sizing "the central risk dial" (SPEC §9.4, LEVELS §8). That is true
early, and degenerate late. Once per-game EV is negative and you face a must-hit threshold (the
tribute), classic gambling theory (Dubins and Savage, bold play) applies: **the probability-
maximizing strategy against a subfair game with a target is to bet as boldly as possible.**
Concretely: on Nights 5-6 the tribute is ~2.5x your realistic bankroll, EV per game is negative
even with a decent build, and the only question is which sequence of near-max stakes reaches the
number. The "decision" has one right answer.

Where real decisions survive:
- **Early nights (positive EV):** genuine Kelly-style tension between growth and ruin, because
  broke = death. This is good and worth protecting.
- **The ladder problem:** how much to hold back so that a game-1 loss still leaves a viable
  game-2/game-3 path. This is a real, interesting calculation. The UI should make it *possible* to
  reason about (see P1).

If you want late-game stake sizing to stay a decision rather than a ritual, you need something for
the player to protect besides the threshold. Options (pick one, not all):
1. **Partial collections:** missing the tribute by a little costs something survivable but nasty
   (the Collector takes a die, or adds the shortfall to the next night at brutal interest) while a
   big miss is still the whack. Now "how short dare I fall" is a live question. This is the
   strongest option, and it softens the N1/N2 wall (G5) for free.
2. **Carryover value:** Vito skims surplus (ECONOMY Q5), so overshooting has diminishing returns
   and exact-sizing matters.
3. Accept the degeneracy: bold-play-or-die *is* noir. But then stop describing stake sizing as the
   central late-game skill; the late-game skills are build construction and consumable timing.

### G2. Magnitude beats frequency, and the late game collapses to one build

Two related effects:

- **Threshold-clearing favors payout multipliers over win rate.** To turn $400 into $2,400 in
  three games, one big-multiplier win does it; three 1:1 wins barely do. With avgPayout ~2x, P(at
  least one win in 3) at a 50% win rate is 87.5%, versus needing 2-of-3 (50%) on a 1:1 build. So
  Gilded Die / Headcracker / Heat amplifiers are structurally stronger than equivalent win-rate
  points. Price them accordingly or this becomes the only shop strategy.
- **Mark-loading invalidates point-quality dice.** The Squeeze's late mechanism is that the mark
  wins ~95% of point battles (Vito). If that 95% holds *regardless of your point*, then Lucky Six,
  Shaved Edge, Even Steven, and High Roller (dice whose value is "set a better point") are nearly
  worthless from Night 5 on, and the only scaling builds are **instant-win manufacture**
  (Sequencer, Magnet) plus payout charms, because instant wins bypass the point battle entirely.
  Build diversity collapses exactly when builds matter most.

**Recommendations:**
- Make mark-loading a *distribution shift*, not a flat point-battle win rate: load Vito toward
  points 4-5 and extra instant wins, so a player point of 6 still wins a meaningful share. Then
  point-builds remain a viable (cheaper, steadier) alternative to the jackpot build.
- Add an **anti-mark item category**, which is currently a complete gap: every item in the catalog
  affects your own dice or your payout, none touch the opponent's roll, even though the entire
  late-game problem is the opponent's loaded dice. Thematically rich and mechanically the direct
  counter to The Squeeze. Examples: *Heavy Pavement* (chance the mark's highest die drops 1),
  *Crooked Lookout* (once per night, force the mark to re-roll one die), *Switched Bones* (chance
  the mark rolls your honest cast-offs: cap his point at 4). This also fixes P5 (the mark's roll is
  currently a zero-agency spectator phase).

### G3. Cheating strictly dominates; Lay Low and most Favors are trap options

With the stateless Suspicion model and the stated tuning, the numbers are lopsided:

- A typical cheat die adds ~+5pp win rate, worth ~+0.11S EV per game. Its bust contribution
  (~+1%) costs ~P(win) x 1% x pot ~ 0.013S. **Benefit is ~9x cost.** Equipping every cheat you own
  is always right; "how greedy a stack do I run" (SPEC §9.2) has one answer: maximally greedy.
- **Lay Low** (play a game clean) gives up the build's entire win-rate edge (~0.1-0.3S) to avoid
  ~0.05S of expected bust loss. It is essentially never correct. As designed it will be discovered
  as a noob trap and never touched.
- **Lookout ($18, cuts ~2pp bust for a game)** saves ~0.025S per game; it breaks even only at
  stakes of roughly $240+. So Favors are dead purchases until Night 4-5, then suddenly fine. That
  is a confusing value arc for a shop category.

This is fixable, and the fix is the same as C1: **make Suspicion accumulate across the night**
(and make the bust check meaningfully scale with it, e.g. accrued suspicion is the bust % and a
clean game halves it). Then mid-night states exist where the accrued bust chance on your next big
win is 10-15%, Lay Low is a real cooling decision before a max-stake Heat game, and Favors are
state repair rather than flat insurance. The "rare punctuation" target (~3% average) can still
hold globally; what changes is that the *variance* of the risk becomes player-managed, which is
exactly what §9.2 claims the system is about.

### G4. Compounding night survival means the campaign win rate is probably ~2-5%. Choose that number on purpose.

Per-night survival multiplies. Rough estimate with the documented curve and a competent,
developing build: N1 ~75% (honest), N2 ~55%, N3-N6 ~55-65% each, Reckoning ~45% built. Product:
**~3-4% campaign win rate**, and far below 1% for a fresh account on its first runs. For
comparison, Balatro's base difficulty lands closer to 10-25% for competent players.

There is nothing wrong with brutal, but it should be a chosen number, not an emergent one, because
it drives everything: how generous the Fence must be, how strong items must be, how fast unlocks
arrive. The docs defer the Monte Carlo sim (ECONOMY §10) as "not ready." **The squeeze-earlier
change makes the sim ready now**: the dial interactions are exactly the kind that intuition gets
wrong, the core odds are already exact math, and a few hundred lines of policy simulation
(cautious / aggressive / payout-build / jackpot-build) would answer G1-G5 empirically. This is the
single highest-leverage piece of design work currently not scheduled.

### G5. The first-run wall: Night 1 is tighter than "gentle," and squeeze-earlier makes Night 2 a cliff

Quantified, honest play, $18 seed, $20 tribute:

- At flat stakes, **a 1-win night loses money** (net = 1.3S - 2S = -0.7S). You need 2+ wins
  (~57.5% at a 55% win rate) or escalating stakes after a loss. Realistic Night-1 survival for a
  new player who stakes sensibly: ~65-75%. So roughly **a quarter to a third of first nights end
  in death**, against a stated feel of "gentle; learn the loop."
- The survivor typically clears $20 with only a few dollars of buffer. Night 2 then demands $55,
  and the squeeze-earlier commit makes Night 2 *negative EV on honest dice* (ties-to-mark plus
  0.22 loading) when the docs still say 47% / break-even. Turning ~$5 into $55+ in three games at
  sub-45% win odds requires winning all three with Heat, ~6-12%. **The modal first-run death is
  now Night 2 with the player barely having met the Fence.** ECONOMY's stated target shape ("most
  early runs die N3-N4") no longer matches the curve the code implements.

First-death-as-progression is a fine philosophy (the unlock table leans into it well), but dying
on N1-N2 *before the build layer has demonstrably mattered* risks reading as "this game is just a
coin flip," which is the one impression a luck-forward game cannot afford. Recommendations, in
order of preference:

1. **Make Night 1's collection unloseable by fiat, diegetically.** The Collector takes what you
   have and lets it slide once ("Vito says everybody's light the first night. Once.") and the real
   schedule starts Night 2. You keep the no-tutorial purity, guarantee every player reaches the
   Fence with agency intact, and lose nothing: Night 1 retains stake-sizing stakes via the broke
   condition.
2. Or raise the seed to ~$25 vs the $20 tribute and let the original N2 ties-to-push curve stand
   (i.e. partially revert squeeze-earlier; cheating "required by Night 3" was already plenty
   aggressive given a 3-item starting pool).
3. At minimum, re-sync ECONOMY.md to the implemented curve and re-run the N1/N2 survival math
   deliberately (see G4: sim).

Related: the seed bankroll is $10 in SPEC §8.1 and $18 in ECONOMY §6. Pick one.

### G6. The shop can brick a mandatory-cheat run

Cheating is mandatory (ECONOMY goal #1) but the Fence is 5 random draws from the unlocked pool. As
the pool grows (40+ items), the probability that a given night's shop offers *no loaded die at
all* becomes material, and a run can become unwinnable through pure offer-RNG, with re-rolls as
the only (expensive) recourse. Balatro survives this because no single category is mandatory; in
BONES, Loaded is.

**Recommendation: category quotas in shop generation.** E.g. of 5 slots: at least 1 Loaded, at
least 1 Charm/Trinket, the rest free draws. Keeps discovery variance, removes the unwinnable
tail. The sim (G4) should model offer distribution either way, which ECONOMY §10 already notes.

### G7. Price scaling lags the economy by two orders of magnitude

Prices scale x4 from N1 to N7 (linear) while tributes scale x120 and stakes/pots scale with them.
So items are painfully expensive on Night 1-2 (when you most need that first cheat die: $14
Snake Killer vs an $18 bankroll) and trivially cheap by Night 5-6 ($192 for a x3.5 item while
moving $2,400). The binding late-game constraint becomes shop *offers* (G6), not cash, and the
re-roll cost (10% of tribute) quietly becomes the real price of everything late.

That may be intentional ("rich late" as the reward), but note the consequence: the interesting
buy-or-bank tension lives almost entirely in Nights 1-3. If you want Fence decisions to bite all
run, test tribute-proportional pricing (ECONOMY §9's own alternative) for dice, keeping favors and
consumables cheap. Conversely, if you keep linear pricing, consider letting the very first cheat
die be reachable on Night 1 (see G5: the $14-18 entry price vs an $18 seed is exactly the wall).

---

## 4. Playability and UX findings

### P1. Hidden Heat conflicts with stake sizing being the core skill

Payout = stake x multiplier x Heat, but Heat is never shown (SPEC §12.3) and stake sizing is named
the central skill (§9.4). A player cannot size a stake against a payout they cannot compute. In
practice, experienced players will just memorize "x1.5 after one win, x2 after two," so the hidden
number only taxes new players, the group the no-tutorial design most needs to carry. RESEARCH.md
itself lists "full information where possible: fairness comes from agency" as a core principle;
this is the one place the design contradicts its own research.

**Recommendation that preserves the no-meter aesthetic:** put the *consequence*, not the state, on
the stake control: "PUT UP $40. WIN PAYS $120." (the multiplied number simply grows when you are
hot). No meter, no percentage, no UI for Heat itself; the scene stays the mood indicator, the
stake line carries the math. Same trick works for the Reckoning barks. Keep Suspicion fully
hidden as designed (it is a *risk*, not a payout; hiding risk creates dread, hiding payouts
creates confusion).

### P2. Silent tie-rule changes will read as bugs

The Squeeze's first lever flips a *rule* (ties to banker, then push, then mark), not a
probability, and the doc says to keep it "felt, not stated" (§6.4). A player who learned on Night
1 that ties go to them will, on Night 3, watch an identical board state produce the opposite
result with no explanation. Hidden *odds* feel like fate; hidden *rule changes* feel like a bug or
a cheat by the game, and this one is fully observable. Trust, once lost, undermines the whole
"felt" design.

**Recommendation:** keep the drift, surface it diegetically at zero UI cost. The mark announces
the night's terms when ties first matter: "Ties to the bank? Not tonight, pal. Ties stand." One
bark per change, in NARRATIVE's voice. Mark-loading can stay fully hidden (it is probabilistic;
the N3-N5 night flavor lines already foreshadow it nicely).

### P3. The 1-2-3 result shout "SNAKE EYES." is wrong, and it will be noticed

Snake eyes means double 1s on two dice. Dice-literate players (a core audience for this game) will
flag it instantly, and the game even sells an item called Snake Killer that kills single 1s,
muddying it further. 1-2-3 has real street names; use one or coin one in-voice: "ONE-TWO-THREE.",
"OUT.", "THE RIVER." (on-theme with the whack), or "BUM ROLL." Cheap fix, real credibility gain.

### P4. Re-roll pacing: 50% of throws are "nothing," and every reveal is sequential

Exactly half of all rolls re-roll, so a single game averages two full one-die-at-a-time reveals
for you, plus the same for the mark on point games, and the doc adds a lingering final die. The
first nothing-roll is anticipation; the third is friction, especially at mobile session lengths
and especially during the mark's roll (P5). **Recommendation:** keep the full ceremony for the
first throw and the final settling throw; accelerate intermediate nothing-rolls (faster spill, no
linger). Tie reveal length to decision weight, the same principle as juice-tiered-to-payoff.

### P5. The mark's roll is half the game with zero player presence

After setting a point, the player watches the mark resolve the round. There is no decision, item,
or even ritual gesture on the player's side during the most tension-loaded phase of a point game.
The anti-mark item category (G2) is the mechanical fix; even without it, consider a free
flavor-interaction (holding your breath / a tap-and-hold "sweat" gesture that does nothing but is
*yours*) so the phase has a hand on it. Cee-lo at its best is two people crouched over the same
patch of concrete; right now the second half of that exchange plays itself.

### P6. The no-tutorial bet is good, but point-vs-point needs one readable crutch

Instant wins/losses teach themselves through shouts and money. The subtle case is the point
battle: a new player must infer "pair plus odd die; odd die is my number; higher beats lower;
ties... depend" purely from outcomes. The single cheapest aid: when a point is set, the odd die
stays visually hot (glow / chalk circle around it) and the shout names it ("YOUR POINT: 5."), then
the mark's deciding die gets the same treatment. That is diegetic, wordless past two words, and
removes the one genuinely confusable rule. Also note ceelo.md is written as a real-world rules
reference (banker rotation, multiplayer) and is linked from the README; fine, but make sure
nothing in-game ever links to it, or the no-rules-screen principle quietly dies.

---

## 5. Consistency and copy errors (quick fixes)

| # | Issue | Where |
|---|---|---|
| D1 | **Doc-code drift on the Squeeze**: code now has N2 = ties-to-mark + 0.22 loading and a steeper ramp; ECONOMY §5 still shows N2 = 47% ties-to-push. Re-sync, and re-state the intended "night of death" distribution. | ECONOMY §5 vs commit 8b4b6b2 |
| D2 | Stale values from the pre-steepened curve: knob table says base win "56->38%" (should be ~55->15%); §3 says avgPayout rises "to ~1.44x near Vito" while §4 correctly says 2.11x at Vito (1.44x corresponds to ~40% win, i.e. Night 3). | ECONOMY §3, §10 |
| D3 | Seed bankroll: $10 (SPEC §8.1) vs $18 (ECONOMY §6). LEVELS §7 also says bankroll resets to "$0" on a new run, vs the seed. | SPEC / ECONOMY / LEVELS |
| D4 | Heat ladder: SPEC §9.3 shows "x1.5 -> x2 -> x2.5" but ECONOMY caps a 3-game night at x2.0. The x2.5 step is unreachable; cut it or note it as the >3-game DLC case. | SPEC §9.3 vs ECONOMY §3 |
| D5 | Night-phase order: SPEC §6.1 lists Fence then Alley then Collection; LEVELS §2 diagram shows Alley then Fence then Collection; SPEC §8.2 says "between nights." The intended model (Fence anytime between games, stock refreshes nightly) should be stated once and mirrored. | SPEC §6.1/§8.2 vs LEVELS §2/§3 |
| D6 | **"Felt" appears twice in NARRATIVE** despite the locked "no tables, no felt, ever" staging rule: Vito's intro ("slides a slip of paper across the felt") and the Reckoning deciding-game bark ("Everything's on the felt"), and the Reckoning is explicitly staged on the ground (§6.5). Rewrite both lines ("across the desk", "everything's on the pavement: the money, the marker, you"). | NARRATIVE §2, §5 vs SPEC §0/§2 |
| D7 | CATALOG's `startingUnlocks` (11 items, for testing) contradicts ACHIEVEMENTS' 3-item core pool and gates (e.g. Hot Hand is a hard mastery unlock in ACHIEVEMENTS but ships in startingUnlocks; Greased Palm gated behind reaching the Reckoning but in startingUnlocks). CATALOG says this is temporary; add the reconciliation to the achievement-unlock subtask's definition of done so it cannot ship. | CATALOG vs ACHIEVEMENTS |
| D8 | "Win a single pot of $X" and other cumulative thresholds are unset (known); flagging so they ride along with the sim work (G4) rather than being hand-picked. | ACHIEVEMENTS |

---

## 6. Smaller design observations

- **Two-Face (first-death unlock)** is more interesting than "wild variance" suggests: a 1/6-only
  die enables both 1-1-1 and 6-6-6 (both auto-wins, since any triple wins), boosts point-6 and
  point-1 hands, and enables 1-2-3 when it shows 1. Its real EV could plausibly be *positive*;
  worth an exact 216-grid computation before it ships as the consolation prize.
- **Bust on a 3-game night costs tempo, not just money**: a bust consumes one of only three
  earning slots. Under the accumulating-Suspicion fix (C1/G3), that tempo cost is what makes
  pre-Collection cheating genuinely scary. Good interaction; worth surfacing in the dread juice
  (the mark eyeing the *clock* as well as your hands).
- **Cold Read** (reveal exact bust %) is a smart pressure valve for the no-meters rule: players
  who want the number can *buy* the number, diegetically. Consider making it an early, cheap,
  always-stocked unlock for exactly that reason.
- **Streak Charm** ("every 2nd consecutive win pays double") fires at most once per 3-game night
  (on game 2 or 3). Fine, but price it against that reality ($60 is steep for one proc a night).
- **Lucky Cigarette** ($35, one free Fence re-roll nightly) is wildly undercosted late (a Night-5
  re-roll is ~$95). Either price-scale it or make it the deliberate "shop-build" enabler and let
  it be a known gem; both are defensible, but choose.
- **Endless/NG+ as DLC** is the right call for scope, and the finite win is the right call for the
  fantasy. No notes; just do not let achievement design (e.g. "win a second run") quietly assume
  modes that are out of base scope. ("Win a second run" is fine; it re-runs the campaign.)

---

## 7. Prioritized recommendations

**Now (spec correctness, cheap):**
1. Resolve C1: pick the accumulating Suspicion model; rewrite SPEC §9.2 / ECONOMY §5.1 / LEVELS to
   one definition; define what a bust forfeits.
2. Re-sync ECONOMY.md to the squeeze-earlier curve (D1, D2) and reconcile seed/$ values (D3, D4).
3. Define the Reckoning's money model (C2) and the tribute formula (C3).
4. Fix the copy errors: felt x2 (D6), SNAKE EYES shout (P3).

**Next (design decisions this audit argues for):**
5. Define cup/charm slot economy and proc resolution order (C4).
6. Soften the N1/N2 wall: free-pass first Collection or seed/curve adjustment (G5).
7. Shop category quotas so cheat dice are always reachable (G6); define the 3-item-pool shop (C5).
8. Add the anti-mark item category and make Vito's loading point-beatable (G2, P5).
9. Stake-line payout preview to make Heat math playable without meters (P1); diegetic tie-rule
   barks (P2).

**Soon (the big one):**
10. Build the Monte Carlo sim now rather than later (G4). The squeeze-earlier change altered the
    most sensitive part of the curve with no way to see the consequence; every open tuning
    question in ECONOMY §11 and half the findings above (G1, G2, G5, G7, item prices, achievement
    thresholds) are answered by the same few hundred lines. It needs only the pure C# core's
    rules, which already exist and are tested.

---

## Addendum (2026-06-11): decisions adopted and changes applied

Following the designer's review of this audit, the following were decided and implemented:

1. **Difficulty model (Q1):** Cloverpit-style. The campaign is meant to be effectively unwinnable
   on a fresh account until unlocks widen the Fence pool; **Night 1 survival target ≈ 85%**.
   Applied: seed $18 → **$25**, Night-1 tribute $20 → **$15** (Monte Carlo: ~91% survival at
   cautious stakes, ~74% aggressive). ECONOMY §1 gained an explicit survival-targets goal.
2. **Suspicion (C1, G3):** the **accumulating model** was adopted and implemented
   (`SuspicionService`: accrual = half the loadout's bust-% per crooked game, clean games halve
   the accrued total, bust rolled against the accrued total on crooked wins, reset nightly).
   Docs synced (SPEC §9.2, ECONOMY §5.1). Lay Low is now a real decision.
3. **Slots (C4):** charm-dice DO occupy the 3 rolled slots; passive charms get 2 charm slots
   (placeholder). Proc resolution order documented to match the resolver (face effects in cup
   order, then one whole-hand effect by impact priority, then payout charms at settle). SPEC §9.1.
4. **Reckoning money (C2):** money stays live on Night 7: cash stakes on each Reckoning game,
   broke mid-match = whacked. "Always play all three" corrected to best-of-three (clinch at 2),
   matching the code. SPEC §6.5.
5. **Roll naturalness (designer's observation):** the resolver was hiding its internal re-rolls,
   so the screen only ever showed decisive throws (83% of which are pair + odd: "always doubles
   then a number"). Fixed: the full throw sequence (natural "nothing" re-rolls included) now
   flows from `OutcomeResolver` → `RoundReport` → choreographer/UI, with quick pacing on
   intermediate throws and full ceremony on the decisive one. Honest dice verified uniform
   (50.07% nothing-rate over 200k rolls; banker win 56.10% vs 56.25% exact).
6. **The Squeeze lever was too weak (new finding):** the implemented mark-loading model could not
   take an honest banker below ~34% win even at maximum loading, so the designed 15%-at-Vito
   curve (and the squeeze-earlier commit's intent of negative EV by Night 2) was unreachable.
   Replaced with a severity-scaled loading model and retuned the per-night loadings; the measured
   curve now lands 56 → 40 → 36 → 33 → 28 → 21 → 15% (deciding game ~11%). ECONOMY §5 synced
   with measured values (also fixed the stale 56→38% and ~1.44x-at-Vito leftovers).
7. **Copy fixes (D6, P3):** both "felt" lines rewritten in NARRATIVE; the 1-2-3 shout changed
   from "SNAKE EYES." to "ONE-TWO-THREE." in NARRATIVE and GameUI.

Still open from this audit: G1/G2 mechanics (late-game stake degeneracy, anti-mark items, build
diversity at Vito), G6 shop quotas, P1 payout preview, the C3 tribute formula, and the full
campaign Monte Carlo (G4).

---

## 8. Open questions for the designer

1. **Target campaign win rate** for (a) a competent player with a developed account, (b) a fresh
   account's first five runs? Everything tunes off this and it is currently implicit (~2-5% by my
   estimate). Is brutal-roguelike (~5%) or Balatro-ish (~15-25%) the intent?
2. **Suspicion (C1):** stateless per-game or accumulating per-night? (This audit strongly
   recommends accumulating; confirm before the rewrite.)
3. **Slots (C4):** do charm-dice occupy the 3 rolled slots? How many charm slots exist?
4. **The Reckoning (C2):** does money exist on Night 7 at all?
5. **Late-game build identity (G2):** is "only jackpot-manufacture scales into Vito" intended
   convergence, or should point builds stay viable to the end?
6. **Stake-sizing identity (G1):** are you content with bold-play dominance late (noir-appropriate
   fatalism), or do you want partial collections / surplus skim to keep sizing a live decision?
7. **Squeeze-earlier (G5):** was "modal first death on Night 2" the intent of commit 8b4b6b2, or
   was the target still "most early runs die N3-N4" (ECONOMY §10)?
8. **Heat visibility (P1):** is a multiplied payout preview on the stake control acceptable within
   the no-meters rule?
