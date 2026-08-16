/* Node verification harness for the BONES web engine.
   Run: node web/sim.js
   Checks the Squeeze curve (ties always to the banker, loading-only), Night-1 survival,
   throw-sequence sanity, the simplified Suspicion model, and the endgame build arc
   (point-war rules the mid-game; jackpot manufacture is what fights Vito). */

const E = require('./engine.js');

const pct = v => (100 * v).toFixed(1) + '%';
let fails = 0;
const check = (ok, what) => { console.log((ok ? 'ok:   ' : 'FAIL: ') + what); if (!ok) fails++; };

// ---- 1. Throw sequences: priors are nothing, final decisive, honest rate ~50% ----
{
  const cup = [null, null, null];
  let nothingFirst = 0, total = 0, bad = false;
  const N = 100000;
  for (let i = 0; i < N; i++) {
    const throws = E.resolveBankerThrows(cup);
    if (throws[0].result.kind === 'nothing') nothingFirst++;
    for (let j = 0; j < throws.length - 1; j++)
      if (throws[j].result.kind !== 'nothing') bad = true;
    if (throws[throws.length - 1].result.kind === 'nothing') bad = true;
    total += throws.length;
  }
  check(!bad, 'sequences: priors nothing, final decisive');
  const rate = nothingFirst / N;
  check(rate > 0.48 && rate < 0.52, `honest first-throw nothing rate ~50% (got ${pct(rate)})`);
}

// ---- 2. The Squeeze curve (honest dice; ties always to the banker, loading-only) ----
{
  const targets = [[0.00, 0.5625], [0.58, 0.402], [0.72, 0.359], [0.82, 0.330],
    [0.94, 0.284], [1.10, 0.210], [1.20, 0.150], [1.40, 0.111]]; // 1.40 = Vito's decider
  const cup = [null, null, null];
  for (const [load, target] of targets) {
    let wins = 0; const N = 200000;
    for (let i = 0; i < N; i++)
      if (E.playRound(cup, load).outcome === 'win') wins++;
    const wr = wins / N;
    check(Math.abs(wr - target) < 0.012, `curve load=${load}: ${pct(wr)} (target ${pct(target)})`);
  }
}

// ---- 3. Night 1 survival (seed $40, tribute $15, cautious honest play) ----
{
  let paid = 0; const N = 50000;
  for (let r = 0; r < N; r++) {
    const account = E.newAccount();
    const run = E.newRun();
    while (E.nightStatus(run) === 'playing') {
      let stake = Math.max(1, Math.round(run.bankroll * 0.25));
      const short = 15 - run.bankroll;
      if (short > 0 && stake < short) stake = Math.min(run.bankroll, short);
      E.playGame(run, account, stake, false);
    }
    if (E.nightStatus(run) === 'collection' && run.bankroll >= 15) paid++;
  }
  const rate = paid / N;
  check(rate > 0.86, `Night 1 survival, cautious honest (>=86% target, seed $40): ${pct(rate)}`);
}

// ---- 4. Endgame builds: point-war rules the mid-game, only jackpot manufacture fights Vito ----
{
  const D = id => ({ id });
  const builds = {
    'honest (3 bones)': [null, null, null],
    'jackpot line (Sequencer+Magnet+Finisher)': [D('the_sequencer'), D('the_magnet'), D('the_finisher')],
    'point-war line (LuckySix+Spoiler+ShavedEdge)': [D('lucky_six'), D('the_spoiler'), D('shaved_edge')],
  };
  const rate = (cup, load) => {
    let wins = 0; const N = 200000;
    for (let i = 0; i < N; i++) if (E.playRound(cup, load).outcome === 'win') wins++;
    return wins / N;
  };
  const atVito = {}, atN5 = {};
  for (const [name, cup] of Object.entries(builds)) {
    atVito[name] = rate(cup, 1.20); atN5[name] = rate(cup, 0.94);
    console.log(`      win rate, ${name}: N5 ${pct(atN5[name])} · Vito ${pct(atVito[name])}`);
  }
  check(atVito['honest (3 bones)'] < 0.17, 'honest play is hopeless at Vito (~15%)');
  check(atVito['jackpot line (Sequencer+Magnet+Finisher)'] > 0.45 && atVito['jackpot line (Sequencer+Magnet+Finisher)'] < 0.58,
    'jackpot line fights at Vito (~50%)');
  check(atN5['point-war line (LuckySix+Spoiler+ShavedEdge)'] > 0.38, 'point-war line rules the mid-game (>38% at N5)');
  check(atVito['point-war line (LuckySix+Spoiler+ShavedEdge)'] > 0.19, 'point-war still beats honest at Vito');
}

// ---- 5. Suspicion (simplified): flat accrual regardless of stake; clean games halve ----
{
  const account = E.newAccount();
  const cup = [{ id: 'snake_killer' }, { id: 'lucky_six' }, null]; // heavy + light = 3% per crooked game
  const run = E.newRun(); run.cup = cup;
  E.playGame(run, account, 15, false); // full-tribute stake
  const runSmall = E.newRun(); runSmall.cup = cup;
  E.playGame(runSmall, account, 1, false); // minimum stake
  check(Math.abs(run.night.accSusp - runSmall.night.accSusp) < 1e-9,
    `accrual is stake-blind (${(100 * run.night.accSusp).toFixed(1)}% both ways)`);
  check(Math.abs(run.night.accSusp - 0.03) < 1e-9, 'heavy + light cup accrues 3% per crooked game');
  const before = run.night.accSusp;
  E.playGame(run, account, 5, true); // Lay Low: clean game halves the accrued heat
  check(Math.abs(run.night.accSusp - before / 2) < 1e-9, 'a clean game halves accrued suspicion');
}

// ---- 6. Full-campaign smoke: simulate complete runs with a simple cheater policy ----
{
  let deaths = { 1:0,2:0,3:0,4:0,5:0,6:0,7:0 }, freedoms = 0; const N = 20000;
  const account = E.newAccount();
  account.unlocked = Object.keys(E.DICE).concat(Object.keys(E.FAVORS), Object.keys(E.TRINKETS)); // mature account
  for (let r = 0; r < N; r++) {
    const run = E.newRun();
    let guard = 0;
    while (!run.over && guard++ < 200) {
      const night = E.currentNight(run);
      // Shop: buy the single priciest non-curse die we can afford, keeping a tribute buffer.
      if (run.night.gamesPlayed === 0 && !night.reckoning) {
        E.rollStock(run, account);
        const buffer = night.demand * 0.8;
        const affordable = run.fence.stock
          .filter(o => E.DICE[o.id] && E.DICE[o.id].cat !== 'curse' && run.bankroll - o.price > buffer)
          .sort((a, b) => b.price - a.price);
        if (affordable[0]) E.buyOffer(run, affordable[0]);
        // Equip the three priciest owned dice.
        const sorted = run.ownedDice.slice().sort((a, b) => E.DICE[b.id].price - E.DICE[a.id].price);
        run.cup = [sorted[0] || null, sorted[1] || null, sorted[2] || null];
      }
      const status0 = E.nightStatus(run);
      if (status0 === 'playing') {
        // Bold-ish: stake what's needed to clear, capped.
        const need = night.reckoning ? run.bankroll : Math.max(1, night.demand - run.bankroll + 1);
        const stake = E.clampStake(run, Math.max(Math.round(run.bankroll * 0.5), need));
        E.playGame(run, account, stake, false);
      }
      const status = E.nightStatus(run);
      if (status === 'collection') {
        if (E.collect(run, account).result === 'whacked') { run.over = true; deaths[night.n]++; }
      } else if (status === 'broke') { run.over = true; deaths[night.n]++; }
      else if (status === 'whacked') { run.over = true; deaths[7]++; }
      else if (status === 'freedom') { run.over = true; freedoms++; }
    }
  }
  console.log('      death-by-night distribution:', Object.entries(deaths)
    .map(([n, c]) => `N${n}:${pct(c / N)}`).join(' '), `freedom:${pct(freedoms / N)}`);
  check(freedoms / N < 0.30, 'the campaign is still hard even built');
}

// ---- 7. The Reckoning flow: a built, bankrolled player wins the match at a real rate ----
{
  const a = E.newAccount();
  let freedoms = 0, broke = 0; const N = 5000;
  for (let i = 0; i < N; i++) {
    const run = E.newRun();
    run.nightIdx = 6; // The Reckoning
    run.bankroll = 200;
    run.cup = [{ id: 'the_sequencer' }, { id: 'the_magnet' }, { id: 'the_finisher' }];
    let guard = 0;
    while (guard++ < 10) {
      const st = E.nightStatus(run);
      if (st === 'freedom') { freedoms++; break; }
      if (st === 'whacked' || st === 'broke') { broke++; break; }
      E.playGame(run, a, run.bankroll, false); // all-in
    }
  }
  const rate = freedoms / N;
  check(rate > 0.15 && rate < 0.40,
    `Reckoning all-in with the jackpot build: freedom ${pct(rate)} (~25%: all-in means two straight)`);
}

console.log(fails === 0 ? '\nALL CHECKS PASSED' : `\n${fails} CHECKS FAILED`);
process.exit(fails === 0 ? 0 : 1);
