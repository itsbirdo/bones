/* BONES account-lifecycle Monte Carlo.
   Run: node web/montecarlo.js [accounts] [maxRunsPerAccount]

   Simulates PERSISTENT ACCOUNTS, not isolated runs: each account starts with the 3-item pool
   and plays runs back to back; unlocks earned in run N widen the Fence pool for run N+1,
   exactly like a real player's session. Reports night-by-night funnel stats, progression
   (catalog growth, runs-to-first-win), and the levers the numbers point at.

   The bot is a reasonable human proxy, not an optimizer:
   - shops at night start (dice by a value heuristic, hones, favors, key trinkets, re-rolls),
   - equips the best 3 owned dice for the night's squeeze level,
   - stakes to protect when covered and to chase when short (bold play on the last game),
   - lays low to cool suspicion when the night is already covered,
   - min-stakes the Reckoning (money has no upside there; see findings). */

const E = require('./engine.js');

const ACCOUNTS = parseInt(process.argv[2] || '1500', 10);
const MAX_RUNS = parseInt(process.argv[3] || '20', 10);

// Optional tribute-schedule override for balance sweeps: node montecarlo.js 800 15 15,35,70,140,280,560
if (process.argv[4]) {
  const demands = process.argv[4].split(',').map(Number);
  demands.forEach((d, i) => { if (E.NIGHTS[i]) E.NIGHTS[i].demand = d; });
  console.log('tribute schedule override:', demands.join(' / '));
}
if (process.argv[5]) { E.setStakeCapMode(process.argv[5]); console.log('stake cap mode:', process.argv[5]); }
if (process.argv[6]) { E.setSeedBankroll(parseInt(process.argv[6], 10)); console.log('seed bankroll:', process.argv[6]); }
if (process.argv[8]) { E.setNightFloat(parseInt(process.argv[8], 10)); console.log('night float:', process.argv[8]); }
if (process.argv[7]) {
  const loads = process.argv[7].split(',').map(Number);
  loads.forEach((l, i) => { if (E.NIGHTS[i]) E.NIGHTS[i].load = l; });
  console.log('loading override:', loads.join(' / '));
}

// ---------- Bot: equip heuristic ----------
// Value of a die at a given night (loading L). Rough EV-shaped scores; the point is
// sensible builds, not perfect ones. (No honing: one fixed score per die per phase.)
function dieScore(id, night, owned) {
  const L = night.load, lateGame = L >= 0.8;
  const has = x => owned.some(d => d.id === x);
  switch (id) {
    case 'snake_killer': return 6;
    case 'lucky_six': return lateGame ? 4.5 : 6.5;
    case 'shaved_edge': return lateGame ? 2.5 : 3.5;
    case 'even_steven': return 3;
    case 'high_roller': return 4;
    case 'two_face': return 1;
    case 'matchmaker': return 5 + (has('the_magnet') ? 2 : 0);
    case 'the_sequencer': return lateGame ? 10 : 8;
    case 'the_magnet': return lateGame ? 10 : 8;
    case 'the_finisher': return lateGame ? 9 : 7;
    case 'the_spoiler': return lateGame ? 6 : 5;
    case 'gilded_die': return 4;
    case 'headcracker': return 2 + ((has('the_magnet') || has('the_sequencer')) ? 2 : 0);
    case 'point_sharp': return 2;
    case 'streak_charm': return 2;
    case 'hot_hand': return 2;
    case 'rabbits_die': return 1.5;
    default: return 0;
  }
}

function equipBest(run, night) {
  const scored = run.ownedDice
    .map(d => ({ d, s: dieScore(d.id, night, run.ownedDice) }))
    .sort((a, b) => b.s - a.s);
  run.cup = [0, 1, 2].map(i => (scored[i] && scored[i].s > 0 ? { id: scored[i].d.id } : null));
}

// ---------- Bot: shopping ----------
const TRINKET_WISHLIST = ['insurance_chit', 'rabbits_foot', 'marker_shaver', 'brass_knuckles',
  'vitos_favor', 'high_rollers_clip', 'lucky_cigarette'];

function shop(run, account, aggression) {
  const night = E.currentNight(run);
  if (night.reckoning) {
    // Final stop: spend on the build, keep a small stake float.
    spendOnDice(run, account, night, 10);
    return;
  }
  // Build budget: never spend below the tribute you already hold, and keep at least 60% of
  // the roll as farming/staking fuel (40% is the max build spend in any one night).
  const demand = E.effectiveDemand(run, night);
  const factor = aggression === 'aggressive' ? 0.5 : 0.6;
  const reserve = Math.max(Math.round(run.bankroll * factor), Math.min(demand, run.bankroll));
  spendOnDice(run, account, night, reserve);

  // Favors when the cup is crooked (suspicion management).
  if (E.summedSuspicion(run.cup) > 0.02) {
    for (const o of run.fence.stock.slice()) {
      if (o.type === 'favor' && run.bankroll - o.price > reserve) E.buyOffer(run, o, account);
    }
  }
  // Key trinkets, wishlist order, only when comfortably flush.
  for (const want of TRINKET_WISHLIST) {
    const o = run.fence.stock.find(x => x.type === 'trinket' && x.id === want);
    if (o && run.bankroll - o.price > reserve * 2) E.buyOffer(run, o, account);
  }
}

function spendOnDice(run, account, night, reserve) {
  for (let pass = 0; pass < 4; pass++) {
    const offers = run.fence.stock
      .filter(o => o.type === 'die' && run.bankroll - o.price > reserve)
      .map(o => {
        const gain = dieScore(o.id, night, run.ownedDice);
        return { o, value: gain / Math.max(1, o.price) , gain };
      })
      .filter(x => x.gain > 0.5)
      .sort((a, b) => b.value - a.value);
    if (!offers[0]) {
      // Nothing worth buying: one cheap re-roll hunt early in the run.
      const cost = E.nextRerollCost(run);
      if (pass === 0 && run.fence.rerolls < 2 && cost < run.bankroll * 0.05 && run.bankroll - cost > reserve) {
        E.rerollStock(run, account);
        continue;
      }
      break;
    }
    E.buyOffer(run, offers[0].o, account);
  }
}

// ---------- Bot: stake + lay-low ----------
function decideStake(run, aggression) {
  const night = E.currentNight(run);
  if (night.reckoning) return { stake: 1, layLow: false }; // money has no upside vs Vito (see findings)

  const demand = E.effectiveDemand(run, night);
  const h = E.heat(run.night.consecWins, run.cup);
  const gamesLeft = E.GAMES_PER_NIGHT - run.night.gamesPlayed;
  const shortfall = demand - run.bankroll;

  if (shortfall <= 0) {
    // Covered: never bet the rent. The surplus above the tribute is the farming budget; with no
    // surplus to play with, SIT OUT instead of being forced to risk the tribute on a min stake.
    const surplus = run.bankroll - demand;
    if (surplus < 1) return { stake: 0, layLow: false }; // sit out, hold the rent
    const favored = night.load === 0;
    const want = favored ? surplus
      : Math.min(surplus, Math.round(run.bankroll * (aggression === 'aggressive' ? 0.25 : 0.10)));
    const stake = E.clampStake(run, Math.max(1, want));
    const layLow = !favored && run.night.accSusp > 0.05 && E.summedSuspicion(run.cup) > 0;
    return { stake: Math.min(stake, Math.max(1, surplus)), layLow };
  }
  // Short: chase. Aim for one point win (1:1 x heat) to cover, but never all-in before the
  // last throw (keep a comeback float).
  let stake = Math.ceil(shortfall / h);
  if (gamesLeft === 1) stake = Math.max(stake, run.bankroll); // last throw: bold play or die
  else stake = Math.min(Math.max(stake, Math.round(run.bankroll * 0.3)),
                        Math.round(run.bankroll * (aggression === 'aggressive' ? 0.7 : 0.55)));
  return { stake: E.clampStake(run, stake), layLow: false };
}

// ---------- One full run for an account ----------
function playRun(account, aggression, stats) {
  account.runsStarted += 1;
  const run = E.newRun();
  let games = 0;

  while (games++ < 60) {
    const night = E.currentNight(run);
    const n = night.n;
    if (run.night.gamesPlayed === 0) {
      if (stats) stats.reached[n] += 1;
      E.rollStock(run, account);
      shop(run, account, aggression);
      equipBest(run, night);
    }

    let status = E.nightStatus(run);
    if (status === 'playing') {
      const { stake, layLow } = decideStake(run, aggression);
      const rep = E.playGame(run, account, stake, layLow);
      if (stats) {
        stats.games += 1;
        if (rep.busted) stats.busts += 1;
        if (layLow) stats.layLows += 1;
        if (rep.satOut) stats.sitOuts = (stats.sitOuts || 0) + 1;
      }
      status = E.nightStatus(run);
    }

    if (status === 'collection') {
      const c = E.collect(run, account);
      if (c.result === 'whacked') return endRun(account, run, n, 'whacked', stats);
      if (stats) stats.paid[n] += 1;
    } else if (status === 'broke') {
      return endRun(account, run, n, 'broke', stats);
    } else if (status === 'whacked') {
      return endRun(account, run, n, 'whacked', stats);
    } else if (status === 'freedom') {
      account.runsWon += 1;
      E.evaluateUnlocks(account);
      if (stats) { stats.freedoms += 1; stats.reckoningWins += 1; }
      return { result: 'freedom', night: 7 };
    }
  }
  return { result: 'stalled', night: 0 };
}

function endRun(account, run, night, cause, stats) {
  account.deaths += 1;
  if (cause === 'broke') account.brokeDeaths += 1; else account.whackedDeaths += 1;
  E.evaluateUnlocks(account);
  if (stats) stats.death[cause][night] += 1;
  return { result: cause, night };
}

// ---------- The experiment ----------
function freshStats() {
  const z = () => ({ 1: 0, 2: 0, 3: 0, 4: 0, 5: 0, 6: 0, 7: 0 });
  return { reached: z(), paid: z(), death: { broke: z(), whacked: z() }, freedoms: 0,
    reckoningWins: 0, games: 0, busts: 0, layLows: 0, sitOuts: 0 };
}

function simulate(aggression) {
  const buckets = [[1, 1], [2, 3], [4, 6], [7, 10], [11, MAX_RUNS]]; // run-index bands
  const byBucket = buckets.map(() => freshStats());
  const overall = freshStats(); overall.sitOuts = 0;
  const firstWinRuns = [];
  const unlocksAfterRun = {}; // runIdx -> { total, samples } (accounts that won earlier drop out)
  let totalRuns = 0, accountsWon = 0;

  for (let a = 0; a < ACCOUNTS; a++) {
    const account = E.newAccount();
    let won = false;
    for (let r = 1; r <= MAX_RUNS; r++) {
      const bi = buckets.findIndex(([lo, hi]) => r >= lo && r <= hi);
      totalRuns += 1;
      const res = playRun(account, aggression, byBucket[bi]);
      playRunInto(overall, res); // overall tallies from the same result
      const u = unlocksAfterRun[r] || (unlocksAfterRun[r] = { total: 0, samples: 0 });
      u.total += account.unlocked.length; u.samples += 1;
      if (res.result === 'freedom' && !won) {
        won = true; accountsWon += 1; firstWinRuns.push(r);
        break; // the account "beat the game"; session complete
      }
    }
  }

  // overall funnel needs its own tallies: rebuild from buckets.
  for (const key of ['reached', 'paid']) for (let n = 1; n <= 7; n++)
    overall[key][n] = byBucket.reduce((t, b) => t + b[key][n], 0);
  for (const c of ['broke', 'whacked']) for (let n = 1; n <= 7; n++)
    overall.death[c][n] = byBucket.reduce((t, b) => t + b.death[c][n], 0);
  for (const k of ['freedoms', 'games', 'busts', 'layLows', 'reckoningWins', 'sitOuts'])
    overall[k] = byBucket.reduce((t, b) => t + b[k], 0);

  return { aggression, buckets, byBucket, overall, firstWinRuns, unlocksAfterRun, totalRuns, accountsWon };
}
function playRunInto() { /* overall is rebuilt from buckets; placeholder keeps call sites tidy */ }

// ---------- Reporting ----------
const pct = (a, b) => (b > 0 ? (100 * a / b).toFixed(1) + '%' : '-');
const median = arr => { const s = arr.slice().sort((a, b) => a - b); return s.length ? s[Math.floor(s.length / 2)] : null; };

function report(sim) {
  const { overall: o } = sim;
  console.log(`\n================ POLICY: ${sim.aggression.toUpperCase()} ================`);
  console.log(`${ACCOUNTS} accounts, ${sim.totalRuns} total runs (cap ${MAX_RUNS} runs/account, stop on first win)`);
  console.log(`${o.games} games · bust rate ${pct(o.busts, o.games)} · lay-low rate ${pct(o.layLows, o.games)} · sit-out rate ${pct(o.sitOuts || 0, o.games)}`);

  console.log('\nNight-by-night funnel (all runs pooled):');
  console.log('night  reached   survived  surv%    died-broke  died-whacked');
  for (let n = 1; n <= 6; n++) {
    const reach = o.reached[n], paid = o.paid[n];
    console.log(
      `  N${n}   ${String(reach).padStart(7)}   ${String(paid).padStart(8)}  ${pct(paid, reach).padStart(6)}` +
      `   ${String(o.death.broke[n]).padStart(9)}   ${String(o.death.whacked[n]).padStart(11)}`);
  }
  const reach7 = o.reached[7];
  console.log(`  N7   ${String(reach7).padStart(7)}   ${String(o.freedoms).padStart(8)}  ${pct(o.freedoms, reach7).padStart(6)}` +
    `   ${String(o.death.broke[7]).padStart(9)}   ${String(o.death.whacked[7]).padStart(11)}   (the Reckoning)`);

  console.log('\nProgression across the session (conditional night survival by run number):');
  console.log('runs        N1     N2     N3     N4     N5     N6   Reck   runs→win');
  sim.buckets.forEach(([lo, hi], i) => {
    const b = sim.byBucket[i];
    const cells = [];
    for (let n = 1; n <= 6; n++) cells.push(pct(b.paid[n], b.reached[n]).padStart(6));
    cells.push(pct(b.freedoms, b.reached[7]).padStart(6));
    const wins = b.freedoms;
    console.log(`  ${String(lo).padStart(2)}-${String(hi).padEnd(3)}  ${cells.join(' ')}   ${wins} wins`);
  });

  const fw = sim.firstWinRuns;
  console.log(`\nAccounts beating the game within ${MAX_RUNS} runs: ${pct(sim.accountsWon, ACCOUNTS)}` +
    (fw.length ? ` · median runs to first win: ${median(fw)} · fastest: ${Math.min(...fw)}` : ''));
  const avgUnlocks = r => { const u = sim.unlocksAfterRun[r]; return u && u.samples ? (u.total / u.samples).toFixed(1) : '-'; };
  const liveCount = [E.DICE, E.FAVORS, E.TRINKETS]
    .reduce((t, m) => t + Object.values(m).filter(x => !x.deferred).length, 0);
  console.log(`Catalog growth (avg items unlocked of ${liveCount}): after run 1: ${avgUnlocks(1)} · run 3: ${avgUnlocks(3)} · run 5: ${avgUnlocks(5)} · run 10: ${avgUnlocks(10)}`);
}

for (const aggression of ['cautious', 'aggressive']) report(simulate(aggression));
