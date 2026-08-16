/* Headless UI smoke test for the BONES web playtest.
   Run: node web/ui-smoke.js   (or: cd web && npm run ui)

   Loads engine.js + the REAL app.js against a minimal DOM shim, then plays like a player:
   clicks through the intro, throws, shops at the Fence, seats dice from the Bag, pays
   Collections, dies, starts new runs. Verifies:
   - the whole screen flow renders without throwing (title → intro → nights → collection →
     morning → run over → new run, plus Bag and Fence overlays),
   - every beat of The Teach (spec §12.9) fires exactly once on a fresh account,
   - the always-on point-comparison settle line appears ("his 4 against your 5"),
   - a veteran account (30+ games, pre-Teach save) is grandfathered and never sees a line. */

const vm = require('vm');
const fs = require('fs');
const path = require('path');

// ---------- Minimal DOM (exactly what app.js uses) ----------
const VOID = new Set(['input', 'br', 'hr', 'img', 'meta']);
const WRITES = []; // every string written via textContent, for transient-copy assertions

class El {
  constructor(tag, attrs = {}) {
    this.tagName = tag;
    this.attrs = attrs;
    this.children = [];      // El | string
    this.parent = null;
    this.onclick = null;
    this.onchange = null;
    this.disabled = 'disabled' in attrs;
    this.checked = 'checked' in attrs;
  }
  get dataset() {
    const d = {};
    for (const k of Object.keys(this.attrs))
      if (k.startsWith('data-')) d[k.slice(5)] = this.attrs[k];
    return d;
  }
  get className() { return this.attrs.class || ''; }
  set className(v) { this.attrs.class = v; }
  get textContent() {
    let out = '';
    for (const c of this.children) out += typeof c === 'string' ? c : c.textContent;
    return out;
  }
  set textContent(v) { this.children = [String(v)]; WRITES.push(String(v)); }
  set innerHTML(html) { this.children = []; parseInto(this, String(html)); }
  appendChild(el) { el.parent = this; this.children.push(el); return el; }
  remove() {
    if (this.parent) this.parent.children = this.parent.children.filter(c => c !== this);
    this.parent = null;
  }
  *walk() {
    for (const c of this.children) {
      if (typeof c === 'string') continue;
      yield c;
      yield* c.walk();
    }
  }
  querySelector(sel) { for (const el of this.walk()) if (matches(el, sel)) return el; return null; }
  querySelectorAll(sel) { const out = []; for (const el of this.walk()) if (matches(el, sel)) out.push(el); return out; }
}

function matches(el, sel) {
  if (sel.startsWith('#')) return el.attrs.id === sel.slice(1);
  if (sel.startsWith('[') && sel.endsWith(']')) return sel.slice(1, -1) in el.attrs;
  if (sel.startsWith('.')) return (el.attrs.class || '').split(/\s+/).includes(sel.slice(1));
  return el.tagName === sel;
}

function parseInto(parent, html) {
  const re = /<\/?[a-zA-Z][^>]*>|[^<]+/g;
  const stack = [parent];
  let m;
  while ((m = re.exec(html))) {
    const tok = m[0];
    if (tok[0] !== '<') { stack[stack.length - 1].children.push(tok); continue; }
    if (tok[1] === '/') {
      const tag = tok.slice(2, -1).trim().toLowerCase();
      for (let i = stack.length - 1; i > 0; i--)
        if (stack[i].tagName === tag) { stack.length = i; break; }
      continue;
    }
    const t = /^<([a-zA-Z][a-zA-Z0-9]*)([\s\S]*?)\/?>$/.exec(tok);
    const tag = t[1].toLowerCase();
    const attrs = {};
    const ar = /([a-zA-Z-]+)(?:="([^"]*)")?/g;
    let a;
    while ((a = ar.exec(t[2]))) attrs[a[1]] = a[2] !== undefined ? a[2] : '';
    const el = new El(tag, attrs);
    el.parent = stack[stack.length - 1];
    stack[stack.length - 1].children.push(el);
    if (!VOID.has(tag) && !tok.endsWith('/>')) stack.push(el);
  }
}

function makeDocument() {
  const doc = new El('document');
  const body = doc.appendChild(new El('body'));
  const appDiv = body.appendChild(new El('div', { id: 'app' }));
  doc.body = body;
  doc.getElementById = id => doc.querySelector('#' + id);
  doc.createElement = tag => new El(tag);
  return { doc, appDiv };
}

// ---------- Boot the real app in a VM context ----------
function boot(preStorage) {
  const store = new Map(Object.entries(preStorage || {}));
  store.set('bones_pace', '1000000000'); // divide every sleep to ~0ms
  const localStorage = {
    getItem: k => (store.has(k) ? store.get(k) : null),
    setItem: (k, v) => store.set(k, String(v)),
    removeItem: k => store.delete(k),
  };
  const { doc } = makeDocument();
  const ctx = {
    console, setTimeout, clearTimeout, Math, JSON, Date,
    document: doc, localStorage, confirm: () => true,
  };
  vm.createContext(ctx);
  vm.runInContext(fs.readFileSync(path.join(__dirname, 'engine.js'), 'utf8'), ctx, { filename: 'engine.js' });
  vm.runInContext(fs.readFileSync(path.join(__dirname, 'app.js'), 'utf8'), ctx, { filename: 'app.js' });
  return { doc, store };
}

// ---------- Driver ----------
const tick = (ms = 1) => new Promise(r => setTimeout(r, ms));
const account = store => JSON.parse(store.get('bones_account') || '{}');

let fails = 0;
const check = (ok, what) => { console.log((ok ? 'ok:   ' : 'FAIL: ') + what); if (!ok) fails++; };
const errors = [];
process.on('unhandledRejection', e => errors.push(e));
process.on('uncaughtException', e => errors.push(e));

async function settle(doc, tries = 4000) {
  // Wait for a stable, actionable screen.
  for (let i = 0; i < tries; i++) {
    const throwBtn = doc.querySelector('#throw');
    if (throwBtn && !throwBtn.disabled) return 'spot';
    if (doc.querySelector('#pay')) return 'collection';
    if (doc.querySelector('#again')) return 'over';
    if (doc.querySelector('#newrun')) return 'title';
    if (doc.querySelector('#go')) return 'go';
    await tick(2);
  }
  throw new Error('UI never settled');
}

async function shopAndEquip(doc) {
  const fence = doc.querySelector('#fence');
  if (fence) {
    fence.onclick();
    const buy = doc.querySelectorAll('[data-buy]').find(b => !b.disabled);
    if (buy) buy.onclick();
    doc.querySelector('#close').onclick();
    await tick(2);
  }
  const bag = doc.querySelector('#bag');
  if (bag) {
    bag.onclick();
    const die = doc.querySelector('[data-die]');
    if (die) die.onclick();
    doc.querySelector('#close').onclick();
    await tick(2);
  }
}

async function scenarioFresh() {
  console.log('--- scenario A: fresh account plays until every Teach beat fires ---');
  const { doc, store } = boot();
  const teachSeen = new Set();
  let comparisonSeen = false;
  let runs = 0, steps = 0;
  const BEATS = ['first_night', 'first_stake', 'first_nothing', 'first_point', 'first_instant',
    'first_123', 'first_tie', 'first_heat', 'first_fence', 'first_crooked', 'first_laylow'];
  const sample = () => {
    const t = doc.querySelector('#teach');
    if (t && t.textContent) teachSeen.add('shown');
    if (!comparisonSeen && WRITES.some(w => /His \d against your \d/.test(w))) comparisonSeen = true;
  };

  doc.querySelector('#newrun').onclick(); runs++;
  let firstNightChecked = false, firstStakeChecked = false;
  while (steps++ < 6000) {
    const state = await settle(doc);
    sample();
    const acct = account(store);
    const taught = acct.taught || {};
    if (!firstNightChecked && taught.first_night) {
      firstNightChecked = true;
      check(doc.querySelector('.teach') !== null, 'first_night teach line rendered on the night card');
    }
    if (state === 'spot' && !firstStakeChecked) {
      firstStakeChecked = true;
      const t = doc.querySelector('#teach');
      check(t && /Whatever you put down/.test(t.textContent), 'first_stake teach line shows on the first Spot');
    }
    if (BEATS.every(b => taught[b]) && comparisonSeen) break;
    if (state === 'spot') { await shopAndEquip(doc); doc.querySelector('#throw').onclick(); }
    else if (state === 'collection') doc.querySelector('#pay').onclick();
    else if (state === 'go') doc.querySelector('#go').onclick();
    else if (state === 'over') { doc.querySelector('#again').onclick(); await tick(2); doc.querySelector('#newrun').onclick(); runs++; }
    else if (state === 'title') { doc.querySelector('#newrun').onclick(); runs++; }
    await tick(2);
    if (runs > 60) break;
  }
  const taught = account(store).taught || {};
  for (const b of BEATS) check(!!taught[b], `beat ${b} fired (within ${runs} runs)`);
  check(comparisonSeen, 'point-comparison settle line appeared ("his X against your Y")');
  check(errors.length === 0, `no UI errors during ${steps} steps / ${runs} runs` +
    (errors.length ? ` (first: ${errors[0] && errors[0].stack ? errors[0].stack.split('\n')[0] : errors[0]})` : ''));
}

async function scenarioVeteran() {
  console.log('--- scenario B: veteran account (30+ games, pre-Teach save) is grandfathered ---');
  const vet = { gamesPlayed: 60, gamesWon: 25, deaths: 8, seenIntro: true };
  const { doc, store } = boot({ bones_account: JSON.stringify(vet) });
  doc.querySelector('#newrun').onclick();
  let sawTeach = false;
  for (let steps = 0; steps < 400; steps++) {
    const state = await settle(doc);
    for (const el of doc.querySelectorAll('.teach'))
      if (el.textContent.trim()) sawTeach = true;
    if (state === 'spot') doc.querySelector('#throw').onclick();
    else if (state === 'collection') doc.querySelector('#pay').onclick();
    else if (state === 'go') doc.querySelector('#go').onclick();
    else break; // run over: one run is enough
    await tick(2);
  }
  const taught = account(store).taught || {};
  check(Object.keys(taught).length >= 11, 'veteran save pre-marked fully taught');
  check(!sawTeach, 'veteran never sees a Teach line');
}

(async () => {
  await scenarioFresh();
  await scenarioVeteran();
  console.log(fails === 0 ? '\nALL UI CHECKS PASSED' : `\n${fails} UI CHECKS FAILED`);
  process.exit(fails === 0 ? 0 : 1);
})();
