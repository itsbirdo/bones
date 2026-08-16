/* BONES web playtest engine. Pure game logic, no DOM, runnable in Node for simulation.
   Mirrors the design docs (GAME_DESIGN_SPEC.md, ECONOMY.md) including the 2026-08 simplification
   pass: ties ALWAYS go to the banker (the Squeeze runs on mark-loading alone), simplified
   Suspicion (Light +1% / Heavy +2% per crooked game, no stake scaling), no honing (every item
   has one fixed proc), curses + two favors deferred to a post-launch wave, The Equalizer
   removed. Seed $40 / N1 tribute $15; charm dice occupy cup slots; endgame dice (Finisher,
   Spoiler); Reckoning best-of-3 with live cash stakes. All numbers are tuning placeholders. */

const Engine = (() => {
  // ---------- RNG (injectable for deterministic sims) ----------
  let rand = Math.random;
  const setRng = fn => { rand = fn; };
  const d6 = () => 1 + Math.floor(rand() * 6);
  const chance = p => rand() < p;
  const rangeInt = (lo, hiExcl) => lo + Math.floor(rand() * (hiExcl - lo));
  const clamp01 = v => (v < 0 ? 0 : v > 1 ? 1 : v);

  // ---------- Cee-lo evaluation ----------
  // kind: 'nothing' | 'loss123' | 'point' | 'fourFiveSix' | 'triple'
  function evaluate(a, b, c) {
    const s = [a, b, c].sort((x, y) => x - y);
    if (s[0] === s[1] && s[1] === s[2]) return { kind: 'triple', value: s[0], rank: 200 + s[0] };
    if (s[0] === 4 && s[1] === 5 && s[2] === 6) return { kind: 'fourFiveSix', value: 0, rank: 100 };
    if (s[0] === 1 && s[1] === 2 && s[2] === 3) return { kind: 'loss123', value: 0, rank: 0 };
    if (s[0] === s[1]) return { kind: 'point', value: s[2], rank: s[2] };
    if (s[1] === s[2]) return { kind: 'point', value: s[0], rank: s[0] };
    return { kind: 'nothing', value: 0, rank: -1 };
  }
  const isDecisive = r => r.kind !== 'nothing';
  const isInstantWin = r => r.kind === 'fourFiveSix' || r.kind === 'triple';

  // ---------- Dice catalog ----------
  // slot: every die (cheats AND charm-dice) occupies one of the 3 cup slots (decided 2026-06).
  // proc: one fixed chance per item (honing removed 2026-08: no levels, nothing to upgrade).
  // susp: two legible tiers (2026-08). LIGHT (+1%/game) leans your own faces; HEAVY (+2%/game)
  //   rewrites the hand or touches the mark's bones. Charms are clean. Summed across the cup,
  //   accrued per crooked game.
  const SUSP_LIGHT = 0.01, SUSP_HEAVY = 0.02;
  const DICE = {
    snake_killer: { name: 'Snake Killer', cat: 'loaded', effect: 'killLoss',
      proc: 0.50, susp: SUSP_HEAVY, price: 6,
      desc: 'Eats a 1-2-3 before the marks ever see it.' },
    lucky_six: { name: 'Lucky Six', cat: 'loaded', effect: 'biasSix',
      proc: 0.42, susp: SUSP_LIGHT, price: 7,
      desc: 'Wants to land on six. Always has.' },
    shaved_edge: { name: 'Shaved Edge', cat: 'loaded', effect: 'biasHigh',
      proc: 0.50, susp: SUSP_LIGHT, price: 10,
      desc: 'A whisper off one corner. The bones just lean your way.' },
    the_sequencer: { name: 'The Sequencer', cat: 'loaded', effect: 'force456',
      proc: 0.30, susp: SUSP_HEAVY, price: 48,
      desc: 'Finishes the run for you: four, five, six.' },
    the_magnet: { name: 'The Magnet', cat: 'loaded', effect: 'forceTriple',
      proc: 0.28, susp: SUSP_HEAVY, price: 55,
      desc: 'A pair on the pavement pulls the third bone home.' },
    the_finisher: { name: 'The Finisher', cat: 'loaded', effect: 'finishPoint6',
      proc: 0.32, susp: SUSP_HEAVY, price: 50,
      desc: 'A point of six becomes a headcrack: four, five, six.' },
    the_spoiler: { name: 'The Spoiler', cat: 'loaded', effect: 'spoiler',
      proc: 0.34, susp: SUSP_HEAVY, price: 65,
      desc: 'Your thumb on HIS bones: the mark’s point drops a pip.' },
    gilded_die: { name: 'Gilded Die', cat: 'charm', effect: 'payout',
      proc: 0.40, susp: 0, price: 8,
      desc: 'If the hand wins, the pot pays half again.' },
    headcracker: { name: 'Headcracker', cat: 'charm', effect: 'jackpot',
      proc: 0.55, susp: 0, price: 9,
      desc: 'When the bones hit big, it cracks the pot wide open.' },
    streak_charm: { name: 'Streak Charm', cat: 'charm', effect: 'streak',
      proc: 1, susp: 0, price: 8,
      desc: 'Every second consecutive win pays double.' },
    hot_hand: { name: 'Hot Hand', cat: 'charm', effect: 'hotHand',
      proc: 1, susp: 0, price: 45,
      desc: 'Heat builds faster while this rides in your cup.' },
    even_steven: { name: 'Even Steven', cat: 'loaded', effect: 'bumpEven',
      proc: 0.41, susp: SUSP_LIGHT, price: 12,
      desc: 'Nudges an odd face up into an even one. Keeps things tidy.' },
    two_face: { name: 'Two-Face', cat: 'loaded', effect: 'twoFace',
      proc: 1, susp: SUSP_LIGHT, price: 18,
      desc: 'Only ever a one or a six. Nothing in between. Wild either way.' },
    high_roller: { name: 'High Roller', cat: 'loaded', effect: 'bumpLow',
      proc: 0.35, susp: SUSP_LIGHT, price: 22,
      desc: 'Catches a low roll and shoves it up two pips.' },
    matchmaker: { name: 'Matchmaker', cat: 'loaded', effect: 'copycat',
      proc: 0.27, susp: SUSP_HEAVY, price: 40,
      desc: 'Looks at the bones beside it and decides to match.' },
    second_wind: { name: 'Second Wind', cat: 'trick', effect: 'secondWind',
      proc: 1, susp: SUSP_LIGHT, price: 28,
      desc: 'A one-two-three should have buried you. It doesn’t. Once a night, throw again.' },
    rabbits_die: { name: 'Rabbit’s Die', cat: 'charm', effect: 'refund',
      proc: 0.30, susp: 0, price: 26,
      desc: 'Lose, and a few bills find their way back to your fist.' },
    point_sharp: { name: 'Point Sharp', cat: 'charm', effect: 'pointSharp',
      proc: 1, susp: 0, price: 35,
      desc: 'A point win pays a notch better than it has any right to.' },
    // ---- Deferred to the post-launch unlock wave (2026-08): the whole Curse category. ----
    gamblers_curse: { name: 'Gambler’s Curse', cat: 'curse', effect: 'forceBig', deferred: true,
      proc: 1, susp: 0, price: 8,
      desc: 'While you’re hot you can’t help yourself: the stake has to be big.' },
    all_or_nothing: { name: 'All-or-Nothing', cat: 'curse', effect: 'doubleSwing', deferred: true,
      proc: 1, susp: 0, price: 10,
      desc: 'Double the winnings, double the bleeding. No middle ground.' },
    snake_eyes_pact: { name: 'Snake Eyes Pact', cat: 'curse', effect: 'pact', deferred: true,
      proc: 1, susp: 0, price: 12,
      desc: 'A fat bonus on every win, signed in blood: any one you roll kills the throw.' },
    bloody_knuckles: { name: 'Bloody Knuckles', cat: 'curse', effect: 'bloody', deferred: true,
      proc: 1, susp: 0, price: 14,
      desc: 'Heat comes faster, and the heat on you never cools.' },
  };

  const FAVORS = {
    lookout: { name: 'Lookout', charges: 2, reduce: 0.02, price: 10,
      desc: 'A kid on the corner whistles when the heat’s near. (-2% bust, 2 uses)' },
    smooth_talker: { name: 'Smooth Talker', charges: 1, reduce: 0, price: 40,
      desc: 'Caught? You talk your way clean. Erases one bust.' },
    cold_read: { name: 'Cold Read', charges: Infinity, reduce: 0, price: 20,
      desc: 'You read the mark like a cheap paper. And you read your own odds (shows the numbers).' },
    // ---- Deferred to the post-launch unlock wave (2026-08). ----
    greased_palm: { name: 'Greased Palm', charges: 1, reduce: 0.05, price: 25, deferred: true,
      desc: 'A few bills in the right hand. Nobody saw a thing. (-5% bust, 1 use)' },
    cooler_head: { name: 'Cooler Head', charges: Infinity, reduce: 0.01, price: 30, deferred: true,
      desc: 'You keep your cool, the marks stay calmer. (-1% bust, always)' },
  };

  // Trinkets ride in your coat, not the cup: no slot cost. Limited charges unless Infinity.
  const TRINKETS = {
    pawn_ticket: { name: 'Pawn Ticket', charges: 1, price: 5,
      desc: 'Hock a die for half its base price. No questions. (sell from the Bag)' },
    high_rollers_clip: { name: 'High Roller’s Clip', charges: Infinity, price: 25,
      desc: 'The clip’s full of someone else’s money: stake up to 1.5x your roll. Lose big, you’re cleaned out.' },
    insurance_chit: { name: 'Insurance Chit', charges: Infinity, price: 30,
      desc: 'Half of one loss comes back to you. Once a night.' },
    vig_skimmer: { name: 'Vig Skimmer', charges: Infinity, price: 35,
      desc: 'You skim a sliver of the stake back even when the pot walks away.' },
    lucky_cigarette: { name: 'Lucky Cigarette', charges: Infinity, price: 35,
      desc: 'One on the house: your first Fence re-roll each night is free.' },
    loaded_coin: { name: 'Loaded Coin', charges: 1, price: 40,
      desc: 'Flat broke? Flip for it. Heads you get a $10 stake, tails you’re done.' },
    rabbits_foot: { name: 'Rabbit’s Foot', charges: Infinity, price: 55,
      desc: 'Your first stumble each night doesn’t count.' },
    brass_knuckles: { name: 'Brass Knuckles', charges: 1, price: 80,
      desc: 'Miss the Collector once and walk away breathing. He takes what you have.' },
    vitos_favor: { name: 'Vito’s Favor', charges: 1, price: 90,
      desc: 'Vito waves the vig once: a quarter off one Collection, when it saves your neck.' },
    marker_shaver: { name: 'Marker Shaver', charges: Infinity, price: 110,
      desc: 'The marker swells a little slower than it should: every tribute 10% lighter.' },
  };

  // ---------- Unlocks (account-level; ACHIEVEMENTS.md) ----------
  // Deliberately small and low-powered (decided 2026-06-12): one mild lean, one modest charm,
  // one favor. The real cheats are earned fast through the first run's natural beats.
  const STARTING_UNLOCKS = ['shaved_edge', 'gilded_die', 'lookout'];
  const ACHIEVEMENTS = [
    { id: 'first_loss', reward: 'snake_killer',
      line: 'You lost a pot and learned what the bottom looks like. The Snake Killer eats 1-2-3s.',
      test: a => a.gamesLost >= 1 },
    { id: 'five_games', reward: 'lucky_six',
      line: 'Five games on the pavement. The bones know your hands now: a Lucky Six.',
      test: a => a.gamesPlayed >= 5 },
    { id: 'first_win', reward: 'streak_charm',
      line: 'First clean win. A fence slides you a Streak Charm to keep the table warm.',
      test: a => a.gamesWon >= 1 },
    { id: 'three_wins', reward: 'hot_hand',
      line: 'Three pots down. They start calling you the Hot Hand.',
      test: a => a.gamesWon >= 3 },
    { id: 'three_games', reward: 'even_steven',
      line: 'Three games on the pavement. Even Steven likes a tidy player.',
      test: a => a.gamesPlayed >= 3 },
    { id: 'eight_games', reward: 'cold_read',
      line: 'You’re a regular now. You start reading the marks, and your own odds: Cold Read.',
      test: a => a.gamesPlayed >= 8 },
    { id: 'first_456', reward: 'headcracker',
      line: 'Four, five, six. A headcrack. Somebody quietly leaves you a Headcracker.',
      test: a => a.fourFiveSix >= 1 },
    { id: 'first_triple', reward: 'matchmaker',
      line: 'A triple on the pavement. The Matchmaker wants to make you more of them.',
      test: a => a.triples >= 1 },
    { id: 'first_666', reward: 'the_magnet',
      line: 'Devil’s bones. Six, six, six. The Magnet finds its way into your cup.',
      test: a => a.devilsBones >= 1 },
    { id: 'first_loss123', reward: 'second_wind',
      line: 'One-two-three buried you. Next time, a Second Wind digs you out.',
      test: a => a.loss123 >= 1 },
    { id: 'five_losses', reward: 'rabbits_die',
      line: 'Five pots gone. A Rabbit’s Die, for the punch-drunk.',
      test: a => a.gamesLost >= 5 },
    { id: 'first_purchase', reward: 'pawn_ticket',
      line: 'Your first buy from the Fence. He throws in a Pawn Ticket: he buys back, too.',
      test: a => a.fencePurchases >= 1 },
    { id: 'reach_night_3', reward: 'the_finisher',
      line: 'The marks’ bones are turning. The Finisher turns your best point into a headcrack.',
      test: a => a.highestNight >= 3 },
    { id: 'reach_night_5', reward: 'the_sequencer',
      line: 'The late tables. The Sequencer turns up, ready to finish a run for you.',
      test: a => a.highestNight >= 5 },
    { id: 'reach_night_4', reward: 'high_roller',
      line: 'Night four. You’ll need a stronger cheat by now: the High Roller.',
      test: a => a.highestNight >= 4 },
    { id: 'three_nights_survived', reward: 'rabbits_foot',
      line: 'Three nights survived in one run. A Rabbit’s Foot for the long haul.',
      test: a => a.highestNight >= 4 },
    { id: 'reach_night_6', reward: 'insurance_chit',
      line: 'The deep end. An Insurance Chit cushions the brutal nights.',
      test: a => a.highestNight >= 6 },
    { id: 'first_bust', reward: 'smooth_talker',
      line: 'Caught with your thumb on the bone. You learn to talk your way out: Smooth Talker.',
      test: a => a.busts >= 1 },
    { id: 'reach_reckoning', reward: 'the_spoiler',
      line: 'You’ve seen Vito’s bones up close. The Spoiler is how you answer them.',
      test: a => a.highestNight >= 7 },
    { id: 'three_reckonings', reward: 'marker_shaver',
      line: 'Three times to Vito’s table. The Marker Shaver chips at the debt itself.',
      test: a => a.reckoningsReached >= 3 },
    { id: 'first_death', reward: 'two_face',
      line: 'Nothing left to lose. Two-Face: only ever a one or a six.',
      test: a => a.deaths >= 1 },
    { id: 'first_broke', reward: 'loaded_coin',
      line: 'Cleaned out once. The Loaded Coin flips for your last stake.',
      test: a => a.brokeDeaths >= 1 },
    { id: 'first_whacked', reward: 'brass_knuckles',
      line: 'You missed a Collection once. Brass Knuckles buy you one more morning.',
      test: a => a.whackedDeaths >= 1 },
    { id: 'full_cheat_cup', reward: 'vig_skimmer',
      line: 'Three crooked bones in one cup. The Vig Skimmer suits a player like you.',
      test: a => a.fullCheatGames >= 1 },
    { id: 'shop_hunter', reward: 'lucky_cigarette',
      line: 'Three re-rolls in one night? Have a Lucky Cigarette, shop-hunter.',
      test: a => a.nightRerolls3 >= 1 },
    { id: 'one_game_clear', reward: 'point_sharp',
      line: 'A whole Collection cleared in one throw. Point Sharp, for players who hit big.',
      test: a => a.clearedNightOneGame >= 1 },
    { id: 'big_pot', reward: 'high_rollers_clip',
      line: 'A $100 pot in one game. The High Roller’s Clip says bet like you mean it.',
      test: a => a.bigPot >= 1 },
    { id: 'big_collection', reward: 'vitos_favor',
      line: 'You moved real money in one night. Vito noticed. Vito’s Favor.',
      test: a => a.bigCollections >= 1 },
  ];

  // Post-launch unlock wave (deferred 2026-08): kept out of the live list so the launch pool
  // stays legible. Ship these together when the wave lands.
  const DEFERRED_WAVE = [
    { id: 'survive_first_night', reward: 'greased_palm',
      line: 'You cleared the Collection and walked. A Greased Palm waits for next time.',
      test: a => a.highestNight >= 2 },
    { id: 'two_busts', reward: 'cooler_head',
      line: 'Busted twice. You learn to keep a Cooler Head.',
      test: a => a.busts >= 2 },
    { id: 'three_busts', reward: 'bloody_knuckles',
      line: 'Three times caught and still rolling. You stopped fearing the heat: Bloody Knuckles.',
      test: a => a.busts >= 3 },
    { id: 'three_deaths', reward: 'all_or_nothing',
      line: 'Three flameouts. Time for All-or-Nothing.',
      test: a => a.deaths >= 3 },
    { id: 'five_deaths', reward: 'snake_eyes_pact',
      line: 'Five times in the river. Sign the Snake Eyes Pact.',
      test: a => a.deaths >= 5 },
    { id: 'ten_deaths', reward: 'gamblers_curse',
      line: 'Ten deaths. You can’t help yourself anymore: the Gambler’s Curse.',
      test: a => a.deaths >= 10 },
  ];

  // ---------- Nights (the Squeeze; measured curve in ECONOMY.md §5) ----------
  // 2026-08: ties ALWAYS go to the banker (the rule the player learns on Night 1 never changes);
  // the Squeeze runs on mark-loading alone. Measured honest win rates (Monte Carlo, sim.js):
  // 56 / 40 / 36 / 33 / 28 / 21 / 15% (Vito's deciding game: ~11%). Loading past 1.0 keeps
  // scaling severity (always crooked, nastier).
  const NIGHTS = [
    // Demands Monte Carlo-tuned 2026-06-12 (~x1.7-2.0/night): the old x2.5-3.7 treadmill
    // outran what any build could earn (campaign win rate ~0.003%).
    { n: 1, demand: 15, load: 0.00 },
    { n: 2, demand: 30, load: 0.58 },
    { n: 3, demand: 55, load: 0.72 },
    { n: 4, demand: 100, load: 0.82 },
    { n: 5, demand: 170, load: 0.94 },
    { n: 6, demand: 290, load: 1.10 },
    { n: 7, demand: 0, load: 1.20, reckoning: true },
  ];

  // ---------- Economy ----------
  // Monte Carlo-tuned (2026-06-12, see web/montecarlo.js): $40 vs the $15 N1 tribute gives the
  // favored first night real war-chest-building room; $25 left runs too thin to survive Night 2.
  let SEED_BANKROLL = 40;
  const setSeedBankroll = v => { SEED_BANKROLL = v; };
  const MIN_STAKE = 1;
  const GAMES_PER_NIGHT = 3;
  const RECKONING_WINS_NEEDED = 2;
  const RECKONING_DECIDER_BUMP = 0.20; // Vito's nastiest bones for the 1-1 decider (~11% honest)
  let NIGHT_FLOAT = 10;                 // walking-around money each new night (MC-tuned; see montecarlo.js)
  const setNightFloat = v => { NIGHT_FLOAT = v; };
  // Vito's table minimum at the Reckoning: a quarter of your roll rides on every game, so the
  // money you bring matters and min-staking the finale is not a thing.
  const RECKONING_MIN_FRACTION = 0.25;

  const PAYOUT = { point: 1, fourFiveSix: 2, triple: 3 };
  const payoutMult = r =>
    r.kind === 'triple' ? PAYOUT.triple : r.kind === 'fourFiveSix' ? PAYOUT.fourFiveSix : PAYOUT.point;

  const heatStep = cup =>
    0.5 + (cupHas(cup, 'hotHand') ? 0.25 : 0) + (cupHas(cup, 'bloody') ? 0.25 : 0);
  const heat = (consecWins, cup) => 1 + heatStep(cup) * Math.max(0, consecWins);

  const priceAtNight = (base, n) => Math.round(base * (1 + 0.5 * (n - 1)));
  const rerollCost = (night, rerollsUsed) => {
    const demand = night.reckoning ? 290 : night.demand; // Reckoning shops price like Night 6 ($290)
    return Math.max(2, Math.round(0.10 * demand)) * Math.pow(1.8, rerollsUsed) | 0;
  };

  // ---------- Suspicion (simplified 2026-08; spec §9.2) ----------
  // Three rules: every crooked game adds the cup's summed tier % (Light +1 / Heavy +2) to the
  // night's accrued suspicion; a clean game halves it; a crooked WIN rolls the accrued total.
  // No stake scaling: a bust forfeits the pot, so the punishment already scales with the stake.
  const CLEAN_DECAY = 0.5;        // a clean game halves the heat on you
  const accrue = (acc, summed) => clamp01(acc + Math.max(0, summed));
  const decayClean = acc => clamp01(acc * CLEAN_DECAY);

  // ---------- Cup helpers ----------
  // cup: array of 3 entries: null (honest bone) or { id }
  const cupDice = cup => cup.map(s => (s ? { ...DICE[s.id], id: s.id } : null));
  const cupHas = (cup, effect) => cup.some(s => s && DICE[s.id].effect === effect);
  const procOf = d => d.proc;
  const summedSuspicion = cup =>
    cup.reduce((t, s) => t + (s ? DICE[s.id].susp : 0), 0);

  // ---------- Banker throw resolution (full visible sequence; last throw decisive) ----------
  function rollDie(die, fired) {
    if (die && die.effect === 'twoFace') return chance(0.5) ? 1 : 6; // only ever a one or a six
    const f = d6();
    if (die && chance(procOf(die))) {
      if (die.effect === 'biasSix') { fired.push(die.id); return 6; }
      if (die.effect === 'biasHigh') { fired.push(die.id); return rangeInt(4, 7); }
      if (die.effect === 'bumpEven' && f % 2 === 1) { fired.push(die.id); return f + 1; }
      if (die.effect === 'bumpLow' && f <= 3) { fired.push(die.id); return f + 2; }
    }
    return f;
  }

  function applyWholeHand(faces, dice, fired) {
    // The Matchmaker first: this die copies one of the others (manufactures the pair the
    // completion dice feed on).
    for (let i = 0; i < 3; i++) {
      const d = dice[i];
      if (d && d.effect === 'copycat' && chance(procOf(d))) {
        const others = [0, 1, 2].filter(j => j !== i);
        faces = faces.slice();
        faces[i] = faces[others[rangeInt(0, 2)]];
        fired.push(d.id);
        break;
      }
    }
    // Conditional completions (catalog §11) in impact order: triple-maker > 4-5-6-maker >
    // point-6 finisher > loss-killer. Each needs its setup showing in the natural roll.
    const counts = {};
    for (const f of faces) counts[f] = (counts[f] || 0) + 1;
    const pairVal = Object.keys(counts).map(Number).find(v => counts[v] === 2);

    // The Magnet: "if you have a pair, the third die matches it" -> a triple of the pair value.
    if (pairVal !== undefined) {
      for (const d of dice) {
        if (d && d.effect === 'forceTriple' && chance(procOf(d))) {
          fired.push(d.id); return [pairVal, pairVal, pairVal];
        }
      }
    }
    // The Sequencer: "if two of {4,5,6} show, it completes 4-5-6."
    const distinct456 = [4, 5, 6].filter(v => counts[v] >= 1).length;
    if (distinct456 >= 2 && !(counts[4] >= 1 && counts[5] >= 1 && counts[6] >= 1)) {
      for (const d of dice) {
        if (d && d.effect === 'force456' && chance(procOf(d))) { fired.push(d.id); return [4, 5, 6]; }
      }
    }
    const r = evaluate(...faces);
    // The Finisher: a point of six becomes the headcrack itself.
    if (r.kind === 'point' && r.value === 6) {
      for (const d of dice) {
        if (d && d.effect === 'finishPoint6' && chance(procOf(d))) { fired.push(d.id); return [4, 5, 6]; }
      }
    }
    if (r.kind === 'loss123') {
      for (const d of dice) {
        if (d && d.effect === 'killLoss' && chance(procOf(d))) {
          fired.push(d.id);
          const target = rangeInt(2, 7);
          return faces.map(f => (f === 1 ? target : f)); // the 1 bumps; usually pairs into a point
        }
      }
    }
    return faces;
  }

  function resolveBankerThrows(cup) {
    const dice = cupDice(cup);
    const throws = [];
    for (let attempt = 0; attempt < 32; attempt++) {
      const fired = [];
      let faces = [rollDie(dice[0], fired), rollDie(dice[1], fired), rollDie(dice[2], fired)];
      faces = applyWholeHand(faces, dice, fired);
      const result = evaluate(...faces);
      throws.push({ faces, result, fired });
      if (isDecisive(result)) return throws;
    }
    throws.push({ faces: [6, 6, 2], result: evaluate(6, 6, 2), fired: [] });
    return throws;
  }

  // ---------- Mark resolution (severity-scaled loading; ECONOMY §5) ----------
  function loadedFace(load) {
    // Severity scales with the loading level. Mid-run the mark beats you with high points
    // (anti-point tech like The Spoiler has a fight); at Vito-grade loading the bones run to
    // monsters, which is what pushes the base win under the ties-to-banker floor.
    const r = rand();
    const six = Math.max(0, (load - 0.75)) * 2.0;  // straight 6s only creep in past 0.75
    if (r < six) return 6;
    if (r < load) return rangeInt(5, 7);            // heavy shave: 5 or 6
    return rangeInt(4, 7);                          // light shave: 4-6
  }

  function resolveMarkThrows(load) {
    const crooked = load > 0 && chance(load);
    const throws = [];
    for (let attempt = 0; attempt < 32; attempt++) {
      const faces = [0, 0, 0].map(() => (crooked ? loadedFace(load) : d6()));
      const result = evaluate(...faces);
      throws.push({ faces, result, fired: [] });
      if (isDecisive(result)) return throws;
    }
    throws.push({ faces: [3, 3, 1], result: evaluate(3, 3, 1), fired: [] });
    return throws;
  }

  // The Spoiler: drop the mark's point by one pip (never into a triple; no-op if impossible).
  function spoilMarkThrow(t) {
    if (t.result.kind !== 'point') return null;
    const v = t.result.value;
    const pair = t.faces.find(f => t.faces.filter(x => x === f).length === 2);
    let nv = v - 1;
    if (nv === pair) nv -= 1;
    if (nv < 1) return null;
    const faces = t.faces.map(f => (f === v ? nv : f));
    return { faces, result: evaluate(...faces), fired: t.fired };
  }

  // ---------- One full round ----------
  // tieRule defaults to 'banker' (permanent rule, 2026-08); the param stays as a sim hook.
  function playRound(cup, loading, tieRule = 'banker') {
    const dice = cupDice(cup);
    const roundProcs = [];
    const tie = tieRule;

    const bankerThrows = resolveBankerThrows(cup);
    const banker = bankerThrows[bankerThrows.length - 1];

    if (banker.result.kind !== 'point') {
      const outcome = isInstantWin(banker.result) ? 'win' : 'loss';
      return { bankerThrows, markThrows: [], markRolled: false, outcome, roundProcs, tie };
    }

    let markThrows = resolveMarkThrows(loading);
    let mark = markThrows[markThrows.length - 1];

    // The Spoiler: after his bones settle, his point drops a pip.
    if (mark.result.kind === 'point') {
      for (const d of dice) {
        if (d && d.effect === 'spoiler' && chance(procOf(d))) {
          const spoiled = spoilMarkThrow(mark);
          if (spoiled) {
            markThrows = markThrows.slice(0, -1).concat([spoiled]);
            mark = spoiled;
            roundProcs.push(d.id);
          }
          break;
        }
      }
    }

    let outcome;
    if (isInstantWin(mark.result)) outcome = 'loss';
    else if (mark.result.kind === 'loss123') outcome = 'win';
    else if (banker.result.rank > mark.result.rank) outcome = 'win';
    else if (banker.result.rank < mark.result.rank) outcome = 'loss';
    else outcome = tie === 'banker' ? 'win' : tie === 'mark' ? 'loss' : 'push';

    return { bankerThrows, markThrows, markRolled: true, outcome, roundProcs, tie };
  }

  // ---------- Account (cross-run; persisted by the app) ----------
  const newAccount = () => ({
    unlocked: STARTING_UNLOCKS.slice(),
    earned: [],            // achievement ids already fired
    gamesPlayed: 0, gamesWon: 0, gamesLost: 0,
    fourFiveSix: 0, triples: 0, devilsBones: 0, loss123: 0,
    busts: 0, deaths: 0, brokeDeaths: 0, whackedDeaths: 0, runsWon: 0,
    fencePurchases: 0, nightRerolls3: 0, fullCheatGames: 0,
    clearedNightOneGame: 0, bigPot: 0, bigCollections: 0, reckoningsReached: 0,
    highestNight: 1, fenceUnlocked: false, runsStarted: 0,
    taught: {}, // The Teach (spec §12.9): beat id -> true once its line has fired
  });

  function evaluateUnlocks(account) {
    const newly = [];
    for (const a of ACHIEVEMENTS) {
      if (account.earned.includes(a.id)) continue;
      if (a.test(account)) {
        account.earned.push(a.id);
        if (!account.unlocked.includes(a.reward)) account.unlocked.push(a.reward);
        newly.push(a);
      }
    }
    return newly;
  }

  // ---------- Run state ----------
  const newRun = () => ({
    bankroll: SEED_BANKROLL,
    nightIdx: 0,
    ownedDice: [],            // [{id}]
    favors: [],               // [{id, charges}]
    trinkets: [],             // [{id, charges}] — coat pocket, no cup slot
    cup: [null, null, null],  // null = honest bone
    night: newNightState(),
    fence: { stock: [], rerolls: 0, rolledForNight: -1 },
    over: false, result: null, // 'freedom' | 'whacked' | 'broke'
  });

  const newNightState = () => ({
    gamesPlayed: 0, consecWins: 0, accSusp: 0, reckW: 0, reckL: 0,
    used: {}, // per-night one-shots: rabbits_foot, insurance_chit, second_wind
  });

  const currentNight = run => NIGHTS[run.nightIdx];

  const trinketOf = (run, id) => run.trinkets.find(t => t.id === id && t.charges > 0);
  function consumeTrinket(run, id) {
    const t = trinketOf(run, id);
    if (t && t.charges !== Infinity) t.charges -= 1;
    run.trinkets = run.trinkets.filter(x => x.charges > 0);
  }

  // What the Collector actually demands tonight (Marker Shaver lightens every tribute).
  const effectiveDemand = (run, night) =>
    Math.round((night || currentNight(run)).demand * (trinketOf(run, 'marker_shaver') ? 0.9 : 1));

  // Stake-cap rule (balance knob; the Monte Carlo showed 'tribute' strangles the favored
  // night's war-chest building): 'tribute' = min(bankroll, demand), 'double' = min(bankroll,
  // 2x demand), 'bankroll' = your whole roll. The Reckoning is always bankroll-capped.
  let stakeCapMode = 'bankroll'; // Monte Carlo verdict: tribute-capped stakes prevent the
                                 // snowball that makes late nights winnable (0.4% vs ~6% session
                                 // win rate); the bankroll cap is the fun one.
  const setStakeCapMode = m => { stakeCapMode = m; };
  function maxStake(run) {
    const night = currentNight(run);
    if (night.reckoning) return Math.max(MIN_STAKE, run.bankroll);
    const clip = trinketOf(run, 'high_rollers_clip') ? 1.5 : 1; // the clip is full of someone else's money
    let cap;
    if (stakeCapMode === 'bankroll') cap = Math.round(run.bankroll * clip);
    else if (stakeCapMode === 'double') cap = Math.min(Math.round(run.bankroll * clip), night.demand * 2);
    else cap = Math.min(Math.round(run.bankroll * clip), night.demand);
    return Math.max(MIN_STAKE, cap);
  }
  const clampStake = (run, desired) =>
    Math.max(MIN_STAKE, Math.min(desired, maxStake(run)));

  // Live odds estimate for the CURRENT cup against tonight's squeeze (dev/testing readout):
  // a quick in-browser Monte Carlo over playRound. ev = expected profit per $1 staked at heat x1.
  function estimateOdds(run, samples = 2000) {
    const night = currentNight(run);
    let load = night.load;
    if (night.reckoning && run.night.reckW === 1 && run.night.reckL === 1)
      load += RECKONING_DECIDER_BUMP;
    let wins = 0, losses = 0, multSum = 0;
    for (let i = 0; i < samples; i++) {
      const r = playRound(run.cup, load);
      if (r.outcome === 'win') {
        wins += 1;
        multSum += payoutMult(r.bankerThrows[r.bankerThrows.length - 1].result);
      } else if (r.outcome === 'loss') losses += 1;
    }
    return { win: wins / samples, loss: losses / samples, ev: multSum / samples - losses / samples };
  }

  // What the next win pays at the current Heat (the stake-line preview; point win, the floor).
  function winPays(run, stake) {
    return Math.round(stake * PAYOUT.point * heat(run.night.consecWins, run.cup));
  }

  // Standing bust % if the next game is played crooked and won (dev/readout aid).
  function nextBustPercent(run) {
    const night = currentNight(run);
    if (night.reckoning) return 0;
    const after = accrue(run.night.accSusp, summedSuspicion(run.cup));
    return Math.max(0, after - favorReduction(run)) * 100;
  }

  const favorReduction = run =>
    run.favors.reduce((t, f) => t + (f.charges > 0 ? FAVORS[f.id].reduce : 0), 0);

  function consumeFavorCharges(run) {
    for (const f of run.favors) if (f.charges > 0 && f.charges !== Infinity) f.charges -= 1;
    run.favors = run.favors.filter(f => f.charges > 0);
  }

  // ---------- Play one game (the full settle) ----------
  function playGame(run, account, stake, layLow) {
    const night = currentNight(run);
    const notes = []; // human-readable item moments for the UI log
    const reckoning = !!night.reckoning;

    // Sit out (stake 0): watch a hand instead of playing it. The game is spent, the heat on you
    // cools, nothing is risked. Not available at the Reckoning (Vito did not invite a spectator).
    if (stake === 0 && !reckoning) {
      run.night.accSusp = cupHas(run.cup, 'bloody') ? run.night.accSusp : decayClean(run.night.accSusp);
      run.night.consecWins = 0; // a streak does not survive you leaving the circle
      run.night.gamesPlayed += 1;
      account.gamesPlayed += 1;
      account.fenceUnlocked = true;
      const unlocks = evaluateUnlocks(account);
      return { satOut: true, round: null, stake: 0, delta: 0, busted: false, win: false,
        heat: 1, unlocks, reckoning, notes: ['You sit this one out. The bones move on without you.'] };
    }

    // Gambler's Curse: while you're hot you can't help yourself — the stake has to be big.
    if (cupHas(run.cup, 'forceBig') && run.night.consecWins > 0) {
      const floor = Math.ceil(maxStake(run) / 2);
      if (stake < floor) { stake = floor; notes.push('The Gambler’s Curse won’t let you bet small while you’re hot.'); }
    }
    stake = clampStake(run, stake);

    // Vito's table minimum: a quarter of your roll rides, like it or not.
    if (reckoning) {
      const tableMin = Math.max(MIN_STAKE, Math.ceil(run.bankroll * RECKONING_MIN_FRACTION));
      if (stake < tableMin) { stake = tableMin; notes.push(`Vito names the table: $${tableMin} rides.`); }
    }

    let loading = night.load;
    if (reckoning && run.night.reckW === 1 && run.night.reckL === 1)
      loading += RECKONING_DECIDER_BUMP; // Vito's nastiest bones for the decider

    let round = playRound(run.cup, loading);

    // Second Wind: once a night, a 1-2-3 becomes a fresh throw instead of a burial.
    let bankerFinal = round.bankerThrows[round.bankerThrows.length - 1];
    if (bankerFinal.result.kind === 'loss123' && !run.night.used.secondWind && cupHas(run.cup, 'secondWind')) {
      run.night.used.secondWind = true;
      account.loss123 += 1; // the 1-2-3 still happened (and still teaches)
      notes.push('Second Wind: the one-two-three doesn’t count. Throw again.');
      round = playRound(run.cup, loading);
      bankerFinal = round.bankerThrows[round.bankerThrows.length - 1];
    }

    // Snake Eyes Pact: any 1 in your settled hand kills the throw.
    if (cupHas(run.cup, 'pact') && round.outcome !== 'loss' && bankerFinal.faces.includes(1)) {
      round = { ...round, outcome: 'loss' };
      notes.push('The Pact bites: a one on the pavement kills the throw.');
    }

    const winsBefore = run.night.consecWins;
    let h = heat(winsBefore, run.cup);
    const win = round.outcome === 'win';
    const doubleSwing = cupHas(run.cup, 'doubleSwing');

    // Payout: stake x multiplier x Heat, with charm dice riding on top.
    let delta = 0;
    if (win) {
      let mult = payoutMult(bankerFinal.result);
      const dice = cupDice(run.cup);
      for (const d of dice) {
        if (!d) continue;
        if (d.effect === 'payout' && chance(procOf(d))) { mult += 0.5; round.roundProcs.push(d.id); }
        if (d.effect === 'jackpot' && mult >= 2 && chance(procOf(d))) { mult += 1.0; round.roundProcs.push(d.id); }
      }
      if (cupHas(run.cup, 'pointSharp') && bankerFinal.result.kind === 'point') mult += 0.25;
      if (cupHas(run.cup, 'pact')) mult += 1.0; // the Pact's upside
      if (cupHas(run.cup, 'streak') && (winsBefore + 1) % 2 === 0) mult *= 2; // every 2nd consecutive win
      if (doubleSwing) mult *= 2;
      delta = Math.round(stake * mult * h);
    } else if (round.outcome === 'loss') {
      delta = -stake * (doubleSwing ? 2 : 1);
      // Loss mitigation, strongest single effect applies.
      if (!run.night.used.rabbitsFoot && trinketOf(run, 'rabbits_foot')) {
        run.night.used.rabbitsFoot = true; delta = 0;
        notes.push('Rabbit’s Foot: the first stumble tonight doesn’t count.');
      } else if (!run.night.used.insurance && trinketOf(run, 'insurance_chit')) {
        run.night.used.insurance = true; delta = Math.round(delta / 2);
        notes.push('Insurance Chit: half the loss comes back.');
      } else {
        const rd = cupDice(run.cup).find(d => d && d.effect === 'refund');
        if (rd && chance(procOf(rd))) {
          delta = Math.round(delta / 2); round.roundProcs.push(rd.id);
          notes.push('Rabbit’s Die: a few bills find their way back.');
        } else if (trinketOf(run, 'vig_skimmer')) {
          delta += Math.max(1, Math.round(stake * 0.05));
          notes.push('Vig Skimmer: you palm a sliver of the stake.');
        }
      }
      delta = Math.max(delta, -run.bankroll); // a doubled loss can't take what you don't have
    }

    // Suspicion: accrues on crooked games, cools on clean ones. Off at the Reckoning.
    let busted = false;
    if (!reckoning) {
      const summed = summedSuspicion(run.cup);
      const playedClean = layLow || summed <= 0;
      if (playedClean) {
        if (!cupHas(run.cup, 'bloody')) run.night.accSusp = decayClean(run.night.accSusp);
        // Bloody Knuckles: the heat on you never cools.
      } else {
        run.night.accSusp = accrue(run.night.accSusp, summed);
      }
      if (win && !playedClean) {
        const bust = Math.max(0, run.night.accSusp - favorReduction(run));
        busted = chance(bust);
        consumeFavorCharges(run);
        if (busted) {
          const st = run.favors.find(f => f.id === 'smooth_talker' && f.charges > 0);
          if (st) {
            st.charges -= 1;
            run.favors = run.favors.filter(f => f.charges > 0);
            busted = false;
            notes.push('Smooth Talker: they grabbed your wrist and you talked it back open.');
          }
        }
      }
    }

    // Settle.
    if (busted) {
      account.busts += 1;
      run.night.consecWins = 0;
      delta = 0; // forfeit the winnings; the stake comes back
    } else if (win) {
      run.bankroll += delta;
      run.night.consecWins += 1;
      account.gamesWon += 1;
      if (delta >= effectiveDemand(run, night) && !reckoning) account.clearedNightOneGame += 1;
      if (delta >= 100) account.bigPot += 1;
    } else if (round.outcome === 'loss') {
      run.bankroll += delta;
      run.night.consecWins = 0;
      account.gamesLost += 1;
    } // push: nothing moves, Heat holds

    // Loaded Coin: flat broke, flip for one more stake.
    if (isBroke(run) && trinketOf(run, 'loaded_coin')) {
      consumeTrinket(run, 'loaded_coin');
      if (chance(0.5)) { run.bankroll = 10; notes.push('Loaded Coin: heads. Ten dollars finds your fist.'); }
      else notes.push('Loaded Coin: tails. That’s that.');
    }

    account.gamesPlayed += 1;
    const bankerKind = bankerFinal.result.kind;
    if (bankerKind === 'fourFiveSix') account.fourFiveSix += 1;
    if (bankerKind === 'triple') account.triples += 1;
    if (bankerKind === 'triple' && bankerFinal.result.value === 6) account.devilsBones += 1;
    if (bankerKind === 'loss123') account.loss123 += 1;
    if (run.cup.every(s => s && DICE[s.id].cat === 'loaded')) account.fullCheatGames += 1;

    run.night.gamesPlayed += 1;
    account.fenceUnlocked = true; // the Fence reveals itself after the first game ever resolves

    if (reckoning) {
      if (win && !busted) run.night.reckW += 1; else run.night.reckL += 1;
    }

    const unlocks = evaluateUnlocks(account);
    return { round, stake, delta, busted, win: win && !busted, heat: h, unlocks, reckoning, notes };
  }

  // ---------- Night resolution ----------
  const isBroke = run => run.bankroll < MIN_STAKE;

  // After a game: is the night (or run) over, and what happens?
  function nightStatus(run) {
    const night = currentNight(run);
    if (night.reckoning) {
      if (run.night.reckW >= RECKONING_WINS_NEEDED) return 'freedom';
      if (run.night.reckL >= RECKONING_WINS_NEEDED) return 'whacked';
      if (isBroke(run)) return 'broke';
      if (run.night.gamesPlayed >= GAMES_PER_NIGHT) return 'whacked'; // never reached 2 wins
      return 'playing';
    }
    if (isBroke(run)) return 'broke';
    if (run.night.gamesPlayed >= GAMES_PER_NIGHT) return 'collection';
    return 'playing';
  }

  // The Collection: pay or die. Returns { result: 'paid' | 'whacked', notes: [...] }.
  function collect(run, account) {
    const night = currentNight(run);
    const notes = [];
    let demand = effectiveDemand(run, night);

    // Vito's Favor: a quarter off one Collection, spent only when it saves your neck.
    if (run.bankroll < demand && trinketOf(run, 'vitos_favor')) {
      const discounted = Math.round(demand * 0.75);
      if (run.bankroll >= discounted) {
        consumeTrinket(run, 'vitos_favor');
        demand = discounted;
        notes.push('Vito’s Favor: he waves the vig, once. He’ll remember it.');
      }
    }

    if (run.bankroll >= demand) {
      run.bankroll -= demand;
      if (demand >= 250) account.bigCollections += 1; // Night 6-scale money (schedule tops at $290)
    } else if (trinketOf(run, 'brass_knuckles')) {
      // Brass Knuckles: he takes everything you have and lets you keep breathing.
      consumeTrinket(run, 'brass_knuckles');
      run.bankroll = 0;
      notes.push('Brass Knuckles: he takes what you have and leaves the rest of you intact. Once.');
    } else {
      return { result: 'whacked', notes };
    }

    run.nightIdx += 1;
    run.night = newNightState();
    run.fence.rerolls = 0;
    run.fence.stock = [];
    run.bankroll += NIGHT_FLOAT; // walking-around money: there is always one more gamble in you
    notes.push(`Walking-around money: +$${NIGHT_FLOAT}.`);
    account.highestNight = Math.max(account.highestNight, NIGHTS[run.nightIdx].n);
    if (NIGHTS[run.nightIdx].reckoning) account.reckoningsReached += 1;
    return { result: 'paid', notes };
  }

  // ---------- The Fence ----------
  // 5 offers from the unlocked pool. Category quota: at least one loaded die is always on the
  // table (cheating is mandatory; the shop must never brick a run).
  function rollStock(run, account) {
    const night = currentNight(run);
    const offers = [];

    // Deferred items never stock, even if an old save unlocked them (post-launch wave).
    const unlockedDice = account.unlocked.filter(id => DICE[id] && !DICE[id].deferred);
    const unlockedFavors = account.unlocked.filter(id => FAVORS[id] && !FAVORS[id].deferred);
    const unlockedTrinkets = account.unlocked.filter(id => TRINKETS[id]);

    const dieOffer = id => (run.ownedDice.some(d => d.id === id)
      ? null // one of each die; nothing to upgrade (honing removed 2026-08)
      : { type: 'die', id, price: priceAtNight(DICE[id].price, night.n) });
    const favorOffer = id => (run.favors.some(f => f.id === id && FAVORS[id].charges === Infinity)
      ? null // persistent favor already owned
      : { type: 'favor', id, price: priceAtNight(FAVORS[id].price, night.n) });
    const trinketOffer = id => (run.trinkets.some(t => t.id === id)
      ? null // one of each in the coat at a time
      : { type: 'trinket', id, price: priceAtNight(TRINKETS[id].price, night.n) });

    const pool = [];
    for (const id of unlockedDice) { const o = dieOffer(id); if (o) pool.push(o); }
    for (const id of unlockedFavors) { const o = favorOffer(id); if (o) pool.push(o); }
    for (const id of unlockedTrinkets) { const o = trinketOffer(id); if (o) pool.push(o); }

    // Quota slot first: a loaded die, if any exists in the pool.
    const loaded = pool.filter(o => DICE[o.id] && DICE[o.id].cat === 'loaded');
    if (loaded.length) {
      const pick = loaded[rangeInt(0, loaded.length)];
      offers.push(pick);
      pool.splice(pool.indexOf(pick), 1);
    }
    while (offers.length < 5 && pool.length) {
      const pick = pool[rangeInt(0, pool.length)];
      offers.push(pick);
      pool.splice(pool.indexOf(pick), 1);
    }
    run.fence.stock = offers;
    run.fence.rolledForNight = run.nightIdx; // fresh stock once per night; re-rolls cost money
    return offers;
  }

  function buyOffer(run, offer, account) {
    if (run.bankroll < offer.price) return false;
    run.bankroll -= offer.price;
    if (offer.type === 'die') run.ownedDice.push({ id: offer.id });
    else if (offer.type === 'favor') run.favors.push({ id: offer.id, charges: FAVORS[offer.id].charges });
    else if (offer.type === 'trinket') run.trinkets.push({ id: offer.id, charges: TRINKETS[offer.id].charges });
    if (account) account.fencePurchases += 1;
    run.fence.stock = run.fence.stock.filter(o => o !== offer);
    return true;
  }

  // Pawn Ticket: hock an owned (unequipped) die back to the Fence for half its base price.
  function sellDie(run, dieId) {
    if (!trinketOf(run, 'pawn_ticket')) return false;
    const idx = run.ownedDice.findIndex(d => d.id === dieId);
    if (idx < 0) return false;
    consumeTrinket(run, 'pawn_ticket');
    run.bankroll += Math.max(1, Math.round(DICE[dieId].price / 2));
    run.ownedDice.splice(idx, 1);
    run.cup = run.cup.map(s => (s && s.id === dieId ? null : s));
    return true;
  }

  function nextRerollCost(run) {
    if (trinketOf(run, 'lucky_cigarette') && run.fence.rerolls === 0) return 0; // first one's free
    return rerollCost(currentNight(run), run.fence.rerolls);
  }

  function rerollStock(run, account) {
    const cost = nextRerollCost(run);
    if (run.bankroll < cost) return false;
    run.bankroll -= cost;
    run.fence.rerolls += 1;
    if (run.fence.rerolls >= 3 && account) account.nightRerolls3 += 1;
    rollStock(run, account);
    return true;
  }

  return {
    setRng, evaluate, playRound, playGame, nightStatus, collect,
    newAccount, newRun, evaluateUnlocks, currentNight, clampStake, maxStake,
    winPays, nextBustPercent, summedSuspicion, heat, isBroke, effectiveDemand, setStakeCapMode, setSeedBankroll, setNightFloat, estimateOdds,
    rollStock, buyOffer, sellDie, rerollStock, rerollCost, nextRerollCost, priceAtNight,
    resolveBankerThrows, resolveMarkThrows, trinketOf,
    DICE, FAVORS, TRINKETS, NIGHTS, ACHIEVEMENTS, DEFERRED_WAVE, STARTING_UNLOCKS,
    SEED_BANKROLL, MIN_STAKE, GAMES_PER_NIGHT, PAYOUT,
  };
})();

if (typeof module !== 'undefined' && module.exports) module.exports = Engine;
