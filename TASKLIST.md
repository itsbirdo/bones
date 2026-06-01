# BONES — Task List

Status of the Unity build (`BONES/`) against the design docs. Updated 2026-06-01.

Legend: `[x]` done · `[ ]` to do · `[~]` partial/stubbed · `⏸` deferred

> **Sequencing decision (2026-06-01):** Art is being developed in a separate tool. **Juice and art
> work is deferred** until the visual style/graphics are locked, so we don't build polish twice.
> Until then, focus on **art-independent engineering**: gameplay systems, balancing, platform, tests.
> The placeholder dice/materials stay as-is; real assets drop into the existing swap-in slots later.

---

## ✅ Done (foundation)

- [x] Pure-C# Cee-lo engine (216 outcomes, tie rules → 56.25% / 44.68% verified) + tests
- [x] Outcome-first resolver (procs/cheats/Squeeze decide faces), RoundService, EconomyService (Heat/payout/collection), SuspicionService
- [x] Data layer: DieDefinition / ItemDefinition / NightConfig / CampaignConfig / JuiceTierConfig / GameDatabase
- [x] Run/Night/Account state + JSON save/resume (offline)
- [x] GameController flow: 3-night run, escalating Collections, broke/whacked/victory
- [x] UI Toolkit HUD + Title / Collection / Game-Over overlays
- [x] Bag (swap cup loadout) + Fence (5 random slots, paid re-roll, buy, honing 1→3)
- [x] Dice: procedural pips, two depth-separated sets, directional roll-in (yours R→L, mark L→R), one-at-a-time reveal, hidden-until-rolled, lingering final die
- [x] JuiceDirector (shake/flash/slow-mo + per-die CLACK), AudioDirector (silent hooks)
- [x] Pipeline-aware placeholder materials (no magenta), editor DataBootstrap + SceneBuilder

---

## ⏸ Feel / Juice / Audio  *(DEFERRED — pending visual style; revisit once art lands)*

- [ ] Particle systems: gold-coin burst, ink splatter, landing dust, smoke drift, Heat embers
- [ ] Comic SFX-letter pops (CLACK / HEADCRACK / BUST) — sized & spawned by stake × Heat (currently a plain fading label)
- [ ] Panel-fracture mask on win streaks
- [ ] Screen grade / palette warming as Heat climbs (amber → orange → red)
- [ ] Cheat-tell visual — mid-tumble glint / face-flip / ink-pulse when a loaded die procs (`CheatFired` event exists, no visual yet)
- [ ] Number juice — payout counter roll-up + overshoot, multiplier chips that stamp & shake
- [ ] Anticipation timing pass on the lingering final die (slow-mo, hold)
- [ ] Audio assets + wiring: dice clack variants, coin clink, cup rattle; win / jackpot / bust stingers; honing snap; proc cue; UI taps
- [ ] Adaptive music bed — smoky jazz stems that layer with Heat; vinyl crackle + rain ambience
- [ ] Tiered haptics (vibration per clack, rumble on jackpot) on device

## ⏸ Art & rendering  *(DEFERRED — being authored in an external tool)*

- [ ] Activate URP (create + assign pipeline asset & Universal Renderer)
- [ ] Toon / inked outline shader for the dice (manifest's "single most important shader")
- [ ] Authored dice meshes — rounded-cube + 4–5 rarity silhouettes (common/loaded/trick/curse) → `DieDefinition.meshOverride`
- [ ] Inked pip face textures + crooked "tell" face variant
- [ ] 2D world: pavement plate, brick-wall backstop, chalk lines, gutter, puddles (+ location dressings)
- [ ] Proper blob contact-shadow sprite (replace the primitive quad)
- [ ] Portraits — Vito, the Collector, rotating marks (+ win/loss/cheat reaction swaps)
- [ ] Item / dice card icons (Balatro-style collectibles)
- [ ] Atmosphere shaders — film grain / halftone overlay, vignette, ink-bleed transitions, rain/smoke layers
- [ ] Flow-screen art + poster/marquee typography (title, Vito intro, run summary, game over)
- [ ] Fonts — condensed noir display + clean numeric (TextMeshPro assets)

## 🎲 Gameplay systems (MVP completion + breadth)

- [~] Favors wired into gameplay — Lookout etc. actually reduce bust % and consume charges (buyable now, but inert in the bust math)
- [ ] Charms beyond the Gilded payout charm (Heat amp, streak charm, headcracker…)
- [ ] Full dice / charm / favor / trinket catalog with effects (spec §11)
- [ ] Achievement + unlock system (only the starting 3 are unlocked; encode the ACHIEVEMENTS.md table)
- [ ] Unlock discovery UI (brief noir story beats, browsable in the Bag)
- [~] The Reckoning — Night 7 finale vs Vito (best-of-3, win 2 of 3, Suspicion off, Vito loaded, 3rd game hardest); currently a stub flag
- [ ] Extend campaign to the full 7 nights with the tuned collection schedule + Squeeze curve
- [ ] Decide & implement Heat/Suspicion presentation: "felt, never shown" (spec) vs visible meters (manifest) — config flag exists conceptually
- [ ] Contextual actions: re-roll item prompt, Lay Low surfacing only when relevant
- [ ] Stake cap = night's tribute (spec) in addition to bankroll cap; stake UX polish
- [ ] Run summary / stats screen; Vito intro narrative beat
- [ ] Limited-use item timing UI (spend-now-or-save consumables)

## ⚖️ Balancing & tuning

- [ ] Monte Carlo sim to validate: Squeeze curve, EV per night, bust rate (~1 per 30–40 games), collection schedule, Fence prices, re-roll cost, honing value
- [ ] Seed bankroll / min-stake feel; bust severity escalation (lose pot → lose die/night at high Suspicion)
- [ ] Tune dice proc %s and Suspicion contributions per spec bands

## 📱 Mobile / platform

- [ ] URP mobile renderer + quality settings tuned for mid-range phones
- [ ] Player settings: portrait lock, IL2CPP, ARM64, app icons, splash, bundle id, versioning
- [ ] Touch tuning — flick sensitivity; optional device-motion shake (flavor only)
- [ ] Safe-area handling (notches / rounded corners)
- [ ] On-device performance pass — draw calls, sprite atlases, frame pacing
- [ ] iOS build via Xcode (signing/provisioning); Android APK/AAB (keystore)
- [ ] Save hardening — versioning/migration, corruption recovery

## 🧹 Polish / tech debt

- [ ] Camera framing tuning so both dice sets sit cleanly between HUD and controls (no overlap)
- [ ] Opponent dice distinct tint; size/depth tuning
- [ ] Settings menu — audio volume, haptics toggle, reset save
- [ ] Pause / app-backgrounding handling
- [ ] More tests — Fence/honing/flow PlayMode tests; favor & charm math
- [ ] Accessibility — text scaling, colorblind-safe treatment of the red/amber spot colors

---

## Suggested order — art-independent first (per the sequencing decision)

**Now (no art needed):**
1. **Finish systems** — wire favors into the bust math, charms beyond Gilded, full dice/charm/favor/trinket catalog with effects
2. **The Reckoning + full 7-night campaign** — Vito best-of-3 finale, tuned collection schedule & Squeeze curve
3. **Achievements + unlock system** — encode the ACHIEVEMENTS.md table; pool grows as you play
4. **Balance pass** — Monte Carlo sim to validate Squeeze/EV/bust-rate/prices; tune from data
5. **Platform + robustness** — player settings, safe-area, save versioning, more PlayMode tests
6. Decide Heat/Suspicion presentation (felt vs meters); contextual actions; run-summary screen

**Later (once the visual style is locked):**
7. **Juice** — particles, comic SFX, number juice, cheat-tell, screen grade
8. **Audio** — clacks, stingers, adaptive jazz, haptics
9. **URP + toon dice shader**, authored meshes/textures, world art, portraits, atmosphere
10. **On-device polish** — perf pass, device builds

External art drops into the existing swap-in slots (`DieDefinition.meshOverride`/`materialOverride`,
`AudioDirector` clip slots, portrait/card sprite fields) throughout — no re-architecting required.
