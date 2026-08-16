/* BONES web playtest UI. Bare-bones on purpose: this exists to test game flow and feel.
   All rules live in engine.js; this file only renders and sequences. Copy follows NARRATIVE.md. */

(() => {
  const E = Engine;
  const VERSION = 'v12'; // keep in step with the ?v= cache-buster in index.html
  const app = document.getElementById('app');
  const GLYPH = ['', '⚀', '⚁', '⚂', '⚃', '⚄', '⚅'];
  let pace = parseFloat(localStorage.getItem('bones_pace') || '1'); // 0.6 slow · 1 normal · 2.5 fast
  const sleep = ms => new Promise(r => setTimeout(r, ms / pace));
  const $ = (sel, root) => (root || document).querySelector(sel);

  // ---------- The Teach (spec §12.9) ----------
  // One line, in a character's voice, once per account, at the moment the rule first matters.
  // Never a popup, never blocks input. Canonical copy: NARRATIVE.md §9.
  const TEACH = {
    first_night:   'Three throws a night. At dawn the Collector takes what’s owed. Come up short and you swim.',
    first_stake:   '“Whatever you put down, I cover. Win, it comes back double. Lose, it’s mine.”',
    first_nothing: 'Nothing scores. Throw until the bones say something.',
    first_point:   '“A pair and a loose bone. The loose one’s your point. Now I roll to beat it.”',
    first_instant: 'Four-five-six, or three of a kind. Nobody rolls against that. The pot’s yours on the spot.',
    first_123:     'One-two-three. The bones bury you themselves. No counter-roll, no argument.',
    first_tie:     '“Same point. Ties go to the bank, and you’re the bank. Tonight and every night.”',
    first_heat:    'You’re hot. Hot money pays bigger. One loss and the streak’s ash.',
    first_fence:   'A fence has set up under the lamppost. He sells edges. He doesn’t ask questions.',
    first_crooked: 'Crooked bones in the cup. Play them and the alley starts watching your hands.',
    first_laylow:  'LAY LOW plays this hand straight. Straight hands cool the alley off.',
  };
  // Show a beat in the Spot's teach line. Burns once per account; if the line is occupied this
  // render the beat stays unburned and retries on a later render, so beats never queue or modal.
  function showTeach(id) {
    if (account.taught[id]) return false;
    const el = $('#teach');
    if (!el || el.textContent) return false;
    el.textContent = '‣ ' + TEACH[id];
    account.taught[id] = true;
    saveAccount();
    return true;
  }
  // For screens without the Spot layout (the night card): an inline paragraph, burned on render.
  function teachHTML(id) {
    if (account.taught[id]) return '';
    account.taught[id] = true;
    saveAccount();
    return `<p class="teach center">‣ ${TEACH[id]}</p>`;
  }

  // ---------- Account persistence ----------
  const loadAccount = () => {
    try {
      const raw = localStorage.getItem('bones_account');
      if (raw) {
        const a = Object.assign(E.newAccount(), JSON.parse(raw));
        // Veterans predating The Teach already know the game: grandfather every beat.
        if (a.gamesPlayed >= 30 && Object.keys(a.taught).length === 0)
          for (const id of Object.keys(TEACH)) a.taught[id] = true;
        return a;
      }
    } catch (e) { /* fresh account */ }
    return E.newAccount();
  };
  const saveAccount = () => localStorage.setItem('bones_account', JSON.stringify(account));

  let account = loadAccount();
  let run = null;
  let busy = false;
  let stake = 1;
  let layLow = false;
  let devMode = true; // start with dev numbers on (playtest build)
  let toasts = [];
  let runLuck = { expected: 0, actual: 0, games: 0 }; // dice luck this run: actual vs expected round wins
  let odds = null; // cached estimateOdds for the current cup/night

  const NIGHT_FLAVOR = {
    1: 'Easy money. Almost.',
    2: 'He’ll want more tomorrow. He always wants more.',
    3: 'The marks are getting wise to your bones.',
    4: 'Word’s out you cheat. So they cheat back.',
    5: 'Every table feels rigged now. That’s because it is.',
    6: 'One more night. Then Vito.',
  };

  const shoutFor = (result, asWin) => {
    if (result.kind === 'triple') return result.value === 6 ? 'DEVIL’S BONES!' : 'TRIPS!';
    if (result.kind === 'fourFiveSix') return 'HEADCRACK!';
    if (result.kind === 'loss123') return 'ONE-TWO-THREE.';
    return asWin ? 'PAID.' : `${result.value} POINT${result.value === 1 ? '' : 'S'}`;
  };

  const defOf = id => E.DICE[id] || E.FAVORS[id] || E.TRINKETS[id];
  const itemName = id => defOf(id).name;
  const itemDesc = id => defOf(id).desc;

  // ---------- Screens ----------
  function title() {
    app.innerHTML = `
      <h1>BONES</h1>
      <p class="center dim">Everybody pays the bank. Tonight, you are the bank.</p>
      <p class="center dim" style="font-size:13px">1947. You owe Vito Carbone more than you’ve got,
      and you’ve got three throws a night to make it right.</p>
      <div class="center" style="margin-top:30px"><button class="big" id="newrun">NEW RUN</button></div>
      <p class="center dim" style="font-size:12px">Lifetime: ${account.runsStarted} runs ·
        ${account.gamesWon} pots won · ${account.deaths} deaths · ${account.runsWon} markers burned<br>
        Catalog: ${account.unlocked.length} of ${[E.DICE, E.FAVORS, E.TRINKETS]
          .reduce((t, m) => t + Object.values(m).filter(x => !x.deferred).length, 0)} unlocked</p>
      <p class="center"><label><input type="checkbox" id="dev" ${devMode ? 'checked' : ''}> dev numbers</label>
      ${devMode ? `<span class="dim" style="font-size:11px"> · build ${VERSION}</span>` : ''}</p>
      <p class="center"><button id="wipe" class="danger" style="font-size:12px">DEV: WIPE SAVE (scores + unlocks)</button></p>`;
    $('#newrun').onclick = () => {
      account.runsStarted += 1;
      run = E.newRun(); stake = 5; layLow = false; toasts = [];
      runLuck = { expected: 0, actual: 0, games: 0 };
      saveAccount();
      account.seenIntro ? nightCard() : intro();
    };
    $('#dev').onchange = e => { devMode = e.target.checked; title(); };
    $('#wipe').onclick = () => {
      if (!confirm('Wipe the account? All unlocks and lifetime stats go back to a fresh start.')) return;
      localStorage.removeItem('bones_account');
      account = E.newAccount();
      run = null; toasts = []; stake = 1; layLow = false;
      title();
    };
  }

  function intro() {
    account.seenIntro = true; saveAccount();
    app.innerHTML = `
      <div class="story"><i class="dim">Back room. Cigar smoke. Rain working the window like it’s trying to get in.</i>

      <b>VITO.</b> Sit. You don’t look like a man holding my money.

      <b>VITO.</b> Relax. I’m reasonable. We’ll call it a marker.

      <i class="dim">He slides a slip of paper across the desk. The ink’s still wet.</i>

      <b>VITO.</b> Word is you’re good with the bones. So you’ll roll. Every night my Collector
      comes around. You pay what’s on the marker, or we have a different kind of conversation.

      <b>VITO.</b> Seven nights. Pay me back, with interest, and we’re square.

      <b>VITO.</b> Miss one… and the river’s cold this time of year.</div>
      <button class="big" id="go">TAKE THE MARKER</button>`;
    $('#go').onclick = nightCard;
  }

  function nightCard() {
    const night = E.currentNight(run);
    if (night.reckoning) return reckoningCard();
    app.innerHTML = `
      <h1 style="font-size:22px">NIGHT ${night.n}</h1>
      <p class="center">The marker says <b class="amber">$${E.effectiveDemand(run, night)}</b>.</p>
      <p class="center dim">${NIGHT_FLAVOR[night.n] || ''}</p>
      ${teachHTML('first_night')}
      <p class="center dim" style="font-size:13px">Bankroll: $${run.bankroll}</p>
      <div style="margin-top:auto"><button class="big" id="go">INTO THE ALLEY</button></div>`;
    $('#go').onclick = spot;
  }

  function reckoningCard() {
    app.innerHTML = `
      <h1 style="font-size:22px" class="red">THE RECKONING</h1>
      <div class="story dim">No Collector tonight. Vito wants to see you himself, back of the club,
      one bulb swinging, his men in the dark. Just you, him, and the bones. For the marker itself.

      Best of three. Win two, walk out clean. Your cash still rides on every game: go broke at his
      table and the river’s waiting either way.</div>
      <p class="center dim" style="font-size:13px">Bankroll: $${run.bankroll}</p>
      <div style="margin-top:auto"><button class="big danger" id="go">FACE HIM</button></div>`;
    $('#go').onclick = spot;
  }

  function spot() {
    const night = E.currentNight(run);
    stake = E.clampStake(run, stake);
    const reck = night.reckoning;
    odds = E.estimateOdds(run);
    app.innerHTML = `
      <div class="hud">
        <span>BANKROLL <b class="amber">$${run.bankroll}</b></span>
        <span>${reck ? 'THE MARKER ITSELF' : `THE MARKER <b class="red">$${E.effectiveDemand(run, night)}</b>`}</span>
        <span>NIGHT ${night.n}</span>
        <span>GAME ${Math.min(run.night.gamesPlayed + 1, 3)}/3</span>
        ${reck ? `<span class="red">YOU ${run.night.reckW} · VITO ${run.night.reckL}</span>` : ''}
      </div>
      <div class="dicezone">
        <div class="dicerow mark" id="markdice"></div>
        <div class="dicerow" id="dice"></div>
        <div class="shout" id="shout"></div>
        <div class="log" id="log"></div>
        <div class="teach" id="teach"></div>
        <div id="toasts"></div>
        ${devMode || run.favors.some(f => f.id === 'cold_read') ? `<div class="devbox" id="dev"></div>` : ''}
      </div>
      <div class="controls">
        <div class="stakebar">
          <span>PUT UP</span>
          <button id="sdown">−</button><b class="amber" id="stakeval">$${stake}</b><button id="sup">+</button>
          <button id="smax">MAX</button>
          <span class="dim" id="pays"></span>
        </div>
        <div class="stakebar">
          <label><input type="checkbox" id="laylow" ${layLow ? 'checked' : ''}> LAY LOW <span class="dim">(play it straight this hand)</span></label>
        </div>
        <button class="big" id="throw">FLICK TO THROW</button>
        ${reck ? '' : '<button id="sitout" style="width:100%">SIT OUT <span class="dim">(skip this game, cool the heat)</span></button>'}
        <div class="center">
          <button id="bag">THE BAG</button>
          ${account.fenceUnlocked ? '<button id="fence">THE FENCE</button>' : ''}
        </div>
      </div>`;
    const refresh = () => {
      stake = E.clampStake(run, stake);
      $('#stakeval').textContent = `$${stake}`;
      $('#pays').textContent = `WIN PAYS $${E.winPays(run, stake)}${run.night.consecWins > 0 ? ' (you’re hot)' : ''}`;
      if ($('#dev')) {
        const luck = runLuck.actual - runLuck.expected;
        $('#dev').innerHTML = `<table class="devtable">
          <tr><td>win chance (this cup, sim)</td><td>${(odds.win * 100).toFixed(1)}%</td>
              <td>EV per $1 staked</td><td>${odds.ev >= 0 ? '+' : ''}${odds.ev.toFixed(2)}</td></tr>
          <tr><td>heat</td><td>x${E.heat(run.night.consecWins, run.cup).toFixed(2)}</td>
              <td>luck this run</td><td>${luck >= 0 ? '+' : ''}${luck.toFixed(1)} wins vs expected (${runLuck.games} games)</td></tr>
          <tr><td>accrued suspicion</td><td>${(run.night.accSusp * 100).toFixed(1)}%</td>
              <td>bust if crooked win</td><td>${E.nextBustPercent(run).toFixed(1)}%</td></tr>
          <tr><td>night</td><td>load ${night.load} (ties to you, always)</td>
              <td>cup suspicion / game</td><td>+${(E.summedSuspicion(run.cup) * 100).toFixed(1)}%</td></tr>
        </table>
        <div style="margin-top:4px">dice speed:
          <button data-pace="0.6" ${pace === 0.6 ? 'disabled' : ''}>SLOW</button>
          <button data-pace="1" ${pace === 1 ? 'disabled' : ''}>NORMAL</button>
          <button data-pace="2.5" ${pace === 2.5 ? 'disabled' : ''}>FAST</button>
        </div>`;
        $('#dev').querySelectorAll('[data-pace]').forEach(el => el.onclick = () => {
          pace = parseFloat(el.dataset.pace);
          localStorage.setItem('bones_pace', String(pace));
          refresh();
        });
      }
    };
    const step = () => (stake < 10 ? 1 : stake < 50 ? 5 : 25);
    $('#sdown').onclick = () => { stake -= step(); refresh(); };
    $('#sup').onclick = () => { stake += step(); refresh(); };
    $('#smax').onclick = () => { stake = E.maxStake(run); refresh(); };
    $('#laylow').onchange = e => { layLow = e.target.checked; };
    $('#bag').onclick = () => bag();
    if ($('#fence')) $('#fence').onclick = () => fence();
    $('#throw').onclick = () => throwGame();
    if ($('#sitout')) $('#sitout').onclick = () => sitOutGame();
    refresh();

    // The Teach: spot-render beats. showTeach's occupancy check makes this a priority list —
    // one line per render, unshown beats retry next time the Spot draws.
    showTeach('first_stake');
    if (account.fenceUnlocked) showTeach('first_fence');
    if (E.summedSuspicion(run.cup) > 0) { showTeach('first_crooked'); showTeach('first_laylow'); }
  }

  async function revealThrows(throws, el, quick) {
    // Pacing: the decisive throw gets the full one-die-at-a-time ceremony with a lingering
    // final die; the in-between "nothing" re-rolls play faster but still readable.
    for (let t = 0; t < throws.length; t++) {
      const last = t === throws.length - 1;
      el.textContent = '';
      await sleep(500); // beat before each spill
      for (let i = 0; i < 3; i++) {
        el.textContent += GLYPH[throws[t].faces[i]] + ' ';
        await sleep(last ? (i === 2 ? 2400 : 1500) : 800);
      }
      if (!last) {
        $('#log').textContent = quick === 'mark' ? 'He throws nothing. Again.' : 'Nothing. Go again.';
        if (quick !== 'mark') showTeach('first_nothing');
        await sleep(1500);
      }
    }
  }

  async function sitOutGame() {
    if (busy) return;
    busy = true;
    const report = E.playGame(run, account, 0, false);
    saveAccount();
    $('#shout').textContent = '';
    $('#log').textContent = report.notes.join('\n');
    for (const u of report.unlocks) toast(u.line);
    await sleep(1600);
    busy = false;
    nextStep();
  }

  async function throwGame() {
    if (busy) return;
    busy = true;
    $('#throw').disabled = true;
    $('#shout').textContent = ''; $('#log').textContent = '';
    if ($('#teach')) $('#teach').textContent = '';
    const night = E.currentNight(run);

    const report = E.playGame(run, account, stake, layLow);
    if (odds) {
      runLuck.expected += odds.win;
      runLuck.actual += report.round.outcome === 'win' ? 1 : 0;
      runLuck.games += 1;
    }
    saveAccount();
    const banker = report.round.bankerThrows[report.round.bankerThrows.length - 1];

    await revealThrows(report.round.bankerThrows, $('#dice'), 'banker');
    $('#shout').textContent = shoutFor(banker.result, false);
    if (banker.result.kind === 'loss123') $('#shout').className = 'shout bad';

    // The Teach: name the banker result the first time each kind lands.
    const bk = banker.result.kind;
    if (bk === 'point') showTeach('first_point');
    else if (bk === 'loss123') showTeach('first_123');
    else if (bk === 'fourFiveSix' || bk === 'triple') showTeach('first_instant');

    let cmp = ''; // point-battle comparison, carried into the settle line (spec §12.8, audit P6)
    if (report.round.markRolled) {
      $('#log').textContent = `You scored ${banker.result.value} point${banker.result.value === 1 ? '' : 's'}. ${night.reckoning ? 'Vito' : 'The mark'} fades it…`;
      await sleep(700);
      await revealThrows(report.round.markThrows, $('#markdice'), 'mark');
      const mark = report.round.markThrows[report.round.markThrows.length - 1];
      $('#log').textContent = `${night.reckoning ? 'Vito' : 'The mark'} shows: ${shoutFor(mark.result, false)}`;
      if (mark.result.kind === 'point' && banker.result.kind === 'point') {
        cmp = `His ${mark.result.value} against your ${banker.result.value}. `;
        if (mark.result.value === banker.result.value) showTeach('first_tie');
      }
      await sleep(500);
    }

    // Settle line.
    const shout = $('#shout');
    if (report.busted) {
      shout.textContent = 'BUSTED!'; shout.className = 'shout bad';
      $('#log').textContent = 'He saw the bones turn. Pot’s gone. Play it cool.';
    } else if (report.win) {
      shout.textContent = shoutFor(banker.result, true); shout.className = 'shout';
      $('#log').textContent = `${cmp}+$${report.delta}  (Heat x${report.heat.toFixed(1)})`;
    } else if (report.round.outcome === 'push') {
      shout.textContent = 'PUSH.'; shout.className = 'shout';
      $('#log').textContent = 'Same point. Nobody’s money moves.';
    } else {
      shout.className = 'shout bad';
      if (banker.result.kind !== 'loss123') shout.textContent = 'TAKEN.';
      $('#log').textContent = `${cmp}−$${report.stake}. ${night.reckoning ? 'Vito gathers the dice without a word.' : 'The mark takes it.'}`;
    }
    if (report.round.roundProcs.length)
      $('#log').textContent += `\n(${[...new Set(report.round.roundProcs)].map(itemName).join(', ')} did its work.)`;
    for (const n of report.notes) $('#log').textContent += `\n${n}`;
    for (const u of report.unlocks) toast(u.line);
    if (report.win && run.night.consecWins >= 2) showTeach('first_heat');

    await sleep(3300); // hold the settle text so playtesters can read what happened
    busy = false;
    nextStep();
  }

  function toast(line) {
    toasts.push(line);
    if ($('#toasts')) $('#toasts').innerHTML = toasts.slice(-3).map(t => `<div class="toast">★ ${t}</div>`).join('');
  }

  function nextStep() {
    const status = E.nightStatus(run);
    if (status === 'playing') return spot();
    if (status === 'collection') return collection();
    if (status === 'broke') return runOver('broke');
    if (status === 'freedom') return runOver('freedom');
    if (status === 'whacked') return runOver('whacked');
  }

  function collection() {
    const night = E.currentNight(run);
    const demand = E.effectiveDemand(run, night);
    const canPay = run.bankroll >= demand || !!E.trinketOf(run, 'vitos_favor') || !!E.trinketOf(run, 'brass_knuckles');
    const line = !canPay
      ? 'He stops counting. Looks up. “That’s it?”'
      : run.bankroll - demand < 5
        ? 'He counts slow, eyes on your face the whole time. Comes up exact. He almost looks let down.'
        : 'He counts it twice, pockets it. “Vito says hello.” Gone before the door swings shut.';
    app.innerHTML = `
      <h1 style="font-size:20px">THE COLLECTION</h1>
      <p class="center dim">The Collector’s here. He doesn’t knock twice.</p>
      <p class="center">Owed: <b class="red">$${demand}</b> · You hold: <b class="amber">$${run.bankroll}</b></p>
      <p class="story dim">${line}</p>
      <div style="margin-top:auto"><button class="big ${canPay ? '' : 'danger'}" id="pay">${canPay ? 'PAY UP' : 'EMPTY YOUR POCKETS'}</button></div>`;
    $('#pay').onclick = () => {
      const c = E.collect(run, account);
      if (c.result === 'whacked') return runOver('whacked');
      for (const n of c.notes) toast(n);
      const newly = E.evaluateUnlocks(account);
      saveAccount();
      morning(newly, c.notes);
    };
  }

  function morning(newUnlocks, notes) {
    const night = E.currentNight(run);
    app.innerHTML = `
      <h1 style="font-size:18px" class="dim">MORNING AFTER</h1>
      <p class="center dim">You made it through. The marker grows while you sleep.</p>
      ${(notes || []).map(n => `<div class="toast">${n}</div>`).join('')}
      ${(newUnlocks || []).map(u => `<div class="toast">★ ${u.line}</div>`).join('')}
      <p class="center">Carried forward: <b class="amber">$${run.bankroll}</b></p>
      <p class="center dim">${night.reckoning ? 'Tonight there is no number. Tonight it’s Vito.' : `Tonight the Collector wants $${E.effectiveDemand(run, night)}.`}</p>
      <div style="margin-top:auto"><button class="big" id="go">NEXT NIGHT</button></div>`;
    $('#go').onclick = nightCard;
  }

  function runOver(kind) {
    run.over = true;
    let title2, story;
    if (kind === 'freedom') {
      account.runsWon += 1;
      title2 = 'FREEDOM';
      story = 'Vito looks at the marker a long while. Then he touches it to the cigar and lets it burn ' +
        'down to nothing. “We’re square.” Dawn comes up gray over the wet street. You walk out owing nobody.';
    } else if (kind === 'broke') {
      account.deaths += 1;
      title2 = 'CLEANED OUT';
      story = 'Empty hands, empty cup. A gambler with nothing to put up is just a man standing in the rain. ' +
        'There’s no next night.';
    } else {
      account.deaths += 1;
      title2 = 'WHACKED';
      story = 'You came up short, and short has a price. They find your coat by the water, pockets empty, ' +
        'marker still open. It outlived you.';
    }
    const newly = E.evaluateUnlocks(account);
    saveAccount();
    const night = E.currentNight(run);
    app.innerHTML = `
      <h1 style="font-size:24px" class="${kind === 'freedom' ? 'amber' : 'red'}">${title2}</h1>
      <p class="story dim">${story}</p>
      <p class="center dim" style="font-size:13px">Run summary: reached Night ${night.n} · final bankroll $${run.bankroll}</p>
      ${newly.concat([]).map(u => `<div class="toast">★ ${u.line}</div>`).join('')}
      ${toasts.length ? `<p class="dim" style="font-size:12px">Unlocked this run: ${toasts.length}</p>` : ''}
      <div style="margin-top:auto"><button class="big" id="again">ANOTHER RUN</button></div>`;
    $('#again').onclick = title;
  }

  // ---------- The Bag (equip) ----------
  function bag() {
    const overlay = document.createElement('div');
    overlay.className = 'overlay';
    const render = () => {
      const cupSlots = run.cup.map((s, i) => {
        const label = s ? E.DICE[s.id].name : 'bone die';
        return `<span class="slot ${s ? 'filled' : ''}" data-slot="${i}">${label}</span>`;
      }).join('');
      const equippedIds = run.cup.filter(Boolean).map(s => s.id);
      const inv = run.ownedDice.filter(d => !equippedIds.includes(d.id));
      const canSell = !!E.trinketOf(run, 'pawn_ticket');
      overlay.innerHTML = `<div>
        <h1 style="font-size:18px">THE BAG</h1>
        <p class="dim" style="font-size:13px">Your cup, your dice, your edge. Tap a cup slot to pull a die; tap an owned die to seat it.</p>
        <p><b>THE CUP</b> (every die rides one of three slots)</p>
        <div>${cupSlots}</div>
        <p><b>OWNED</b></p>
        <div>${inv.length ? inv.map(d =>
          `<span class="slot" data-die="${d.id}">${E.DICE[d.id].name}</span>` +
          (canSell ? `<button data-sell="${d.id}" style="font-size:11px">PAWN $${Math.max(1, Math.round(E.DICE[d.id].price / 2))}</button>` : '')
        ).join('') : '<span class="dim">nothing spare; see the Fence</span>'}</div>
        <p><b>FAVORS</b></p>
        <div>${run.favors.length ? run.favors.map(f =>
          `<div class="card">${E.FAVORS[f.id].name}${f.charges === Infinity ? '' : ` (${f.charges} left)`} <span class="dim">${E.FAVORS[f.id].desc}</span></div>`).join('') : '<span class="dim">none</span>'}</div>
        <p><b>IN YOUR COAT</b> (trinkets, no cup slot)</p>
        <div>${run.trinkets.length ? run.trinkets.map(t =>
          `<div class="card">${E.TRINKETS[t.id].name}${t.charges === Infinity ? '' : ` (${t.charges} left)`} <span class="dim">${E.TRINKETS[t.id].desc}</span></div>`).join('') : '<span class="dim">empty pockets</span>'}</div>
        <p class="dim" style="font-size:12px">Cup suspicion: +${(E.summedSuspicion(run.cup) * 100).toFixed(0)}% per crooked game (a clean game halves what you've built up)</p>
        <button class="big" id="close">BACK TO THE SPOT</button>
      </div>`;
      overlay.querySelectorAll('[data-slot]').forEach(el => el.onclick = () => {
        run.cup[+el.dataset.slot] = null; render();
      });
      overlay.querySelectorAll('[data-die]').forEach(el => el.onclick = () => {
        const empty = run.cup.findIndex(s => !s);
        const die = run.ownedDice.find(d => d.id === el.dataset.die);
        if (empty >= 0) run.cup[empty] = { id: die.id };
        render();
      });
      overlay.querySelectorAll('[data-sell]').forEach(el => el.onclick = () => {
        E.sellDie(run, el.dataset.sell); render();
      });
      $('#close', overlay).onclick = () => { overlay.remove(); spot(); };
    };
    render();
    document.body.appendChild(overlay);
  }

  // ---------- The Fence (shop) ----------
  function fence() {
    if (run.fence.rolledForNight !== run.nightIdx) E.rollStock(run, account);
    const overlay = document.createElement('div');
    overlay.className = 'overlay';
    const render = () => {
      const night = E.currentNight(run);
      const rrCost = E.nextRerollCost(run);
      overlay.innerHTML = `<div>
        <h1 style="font-size:18px">THE FENCE</h1>
        <p class="dim" style="font-size:13px">He doesn’t ask where they came from. Bankroll: <b class="amber">$${run.bankroll}</b></p>
        ${run.fence.stock.map((o, i) => `<div class="card">
            <b>${itemName(o.id)}</b>
            <span class="dim"> ${itemDesc(o.id)}</span><br>
            <button data-buy="${i}" ${run.bankroll < o.price ? 'disabled' : ''}>BUY $${o.price}</button>
          </div>`).join('') || '<p class="dim">Cleaned out. Come back tomorrow.</p>'}
        <button id="reroll" ${run.bankroll < rrCost ? 'disabled' : ''}>SHOW ME SOMETHING ELSE (${rrCost === 0 ? 'on the house' : '$' + rrCost})</button>
        <button class="big" id="close">BACK TO THE SPOT</button>
      </div>`;
      overlay.querySelectorAll('[data-buy]').forEach(el => el.onclick = () => {
        E.buyOffer(run, run.fence.stock[+el.dataset.buy], account); saveAccount(); render();
      });
      $('#reroll', overlay).onclick = () => { E.rerollStock(run, account); render(); };
      $('#close', overlay).onclick = () => { overlay.remove(); spot(); };
    };
    render();
    document.body.appendChild(overlay);
  }

  title();
})();
