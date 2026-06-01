using System;
using UnityEngine;
using Bones.Core;
using Bones.Data;
using Bones.Model;
using Bones.Save;

namespace Bones
{
    /// <summary>Everything the presentation layer needs to animate one settled game.</summary>
    public struct GameReport
    {
        public RoundReport Round;
        public int Stake;
        public double Delta;        // bankroll change before bust
        public bool Busted;
        public int BankrollAfter;
        public double Heat;         // Heat applied to this game
        public bool WasWin;
    }

    public enum RunEndReason { None, Broke, Whacked, Victory }

    /// <summary>
    /// The game brain: drives the run/night/game flow, settles money and Suspicion, and persists.
    /// Outcome-first — a game is fully resolved the instant it's played; the choreographer and
    /// JuiceDirector animate to the known result. Logic lives in the pure Core services.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        [SerializeField] private GameDatabase database;
        [SerializeField] private int rngSeed = 0; // 0 = nondeterministic

        public GameDatabase Database => database;
        public RunState Run { get; private set; }
        public NightState Night { get; private set; } = new();
        public AccountState Account => _save.account;
        public NightConfig CurrentNight =>
            (database != null && database.campaign != null)
                ? database.campaign.NightAt(Run != null ? Run.nightIndex : 0)
                : null;

        /// <summary>True on the final night: best-of-three vs Vito, Suspicion off (spec §6.5).</summary>
        public bool IsReckoning => CurrentNight != null && CurrentNight.isReckoning;

        // --- Events for the UI ---
        public event Action<GameReport> GamePlayed;
        public event Action NightStarted;
        public event Action<bool> CollectionResolved;   // true = survived
        public event Action<RunEndReason> RunEnded;
        public event Action StateChanged;

        /// <summary>Fires once per newly unlocked reward item id (account-level, permanent).
        /// The unlock-notification UI is a separate subtask; until then a Debug.Log is the placeholder.</summary>
        public event Action<string> ItemUnlocked;

        private SaveData _save;
        private IRng _rng;
        private PayoutTable _payouts = PayoutTable.Default;

        private void Awake()
        {
            _save = SaveSystem.Load();
            _rng = rngSeed == 0 ? new SystemRng() : new SystemRng(rngSeed);

            // Fallback: load from Resources if the scene reference wasn't wired. Works in builds too.
            if (database == null)
                database = Resources.Load<GameDatabase>("GameDatabase");

            if (database == null)
                Debug.LogError("[BONES] No GameDatabase found. Run 'BONES ▸ Generate MVP Data' " +
                    "(it writes Assets/Resources/GameDatabase.asset), then press Play again.");
            EnsureStartingUnlocks();
        }

        private void EnsureStartingUnlocks()
        {
            if (database == null) return;
            foreach (var id in database.startingUnlocks) _save.account.Unlock(id);
            SaveSystem.Save(_save);
        }

        /// <summary>
        /// Run the pure AchievementService against a signal built from current lifetime stats, unlock any
        /// newly satisfied reward items on the account (permanent), persist, and fire ItemUnlocked. The
        /// caller is responsible for having already bumped the relevant lifetime counters on the account.
        /// </summary>
        private void EvaluateAchievements(AchievementEvent ev, GameReport? report = null)
        {
            var acc = _save.account;
            var signal = new AchievementSignal(
                ev,
                wasWin: report?.WasWin ?? false,
                wasBust: report?.Busted ?? false,
                bankerKind: report?.Round.Banker.Result.Kind ?? CeeloKind.Nothing,
                bankerValue: report?.Round.Banker.Result.Value ?? 0,
                highestNightReached: acc.highestNightReached,
                gamesWon: acc.gamesWon,
                fourFiveSixCount: acc.fourFiveSixCount,
                tripleCount: acc.tripleCount,
                busts: acc.busts,
                deaths: acc.deaths,
                runsWon: acc.runsWon);

            var newly = AchievementService.Evaluate(signal, acc.unlockedIds);
            if (newly.Count == 0) return;

            foreach (var a in newly)
            {
                if (!acc.Unlock(a.RewardItemId)) continue;
                Debug.Log($"[BONES] Unlocked '{a.RewardItemId}': {a.UnlockLine}");
                ItemUnlocked?.Invoke(a.RewardItemId);
            }
            SaveSystem.Save(_save);
        }

        // ---- Run lifecycle ----

        public void StartNewRun()
        {
            if (database == null || database.campaign == null || database.campaign.NightCount == 0)
            {
                Debug.LogError("[BONES] GameDatabase or its Campaign is not wired. " +
                    "Run 'BONES ▸ Generate MVP Data', then 'BONES ▸ Build Playable Scene' (or assign " +
                    "GameDatabase.asset to the GameController, with its Campaign field set).");
                return;
            }

            Run = new RunState
            {
                bankroll = EconomyService.SeedBankroll,
                nightIndex = 0,
                runsThisLifetimeAtStart = _save.account.runsStarted,
            };
            // Start with three bone dice in the cup.
            Run.cup[0] = Run.cup[1] = Run.cup[2] = database.boneDie != null ? database.boneDie.id : "bone";

            _save.account.runsStarted++;
            _save.activeRun = Run;
            _save.hasActiveRun = true;
            SaveSystem.Save(_save);

            BeginNight();
        }

        public bool TryResumeRun()
        {
            if (_save.hasActiveRun && _save.activeRun != null)
            {
                Run = _save.activeRun;
                BeginNight();
                return true;
            }
            return false;
        }

        /// <summary>Is there a resumable run in the save (without starting it)? For the title screen.</summary>
        public bool TryPeekActiveRun() => _save != null && _save.hasActiveRun && _save.activeRun != null;

        /// <summary>Buy a die at the Fence: deduct cash, own it, and slot it over a bone die if possible.</summary>
        public bool TryBuyDie(DieDefinition die, int price)
        {
            if (die == null || Run == null || Run.bankroll < price) return false;
            Run.bankroll -= price;
            AddDieToOwned(die.id);
            SaveSystem.Save(_save);
            StateChanged?.Invoke();
            return true;
        }

        private void AddDieToOwned(string dieId)
        {
            bool owned = false;
            foreach (var d in Run.ownedDice) if (d.dieId == dieId) { owned = true; break; }
            if (!owned) Run.ownedDice.Add(new OwnedDie { dieId = dieId, level = 1 });

            // Auto-equip into the first bone slot.
            string boneId = database.boneDie != null ? database.boneDie.id : "bone";
            for (int i = 0; i < 3; i++)
                if (Run.cup[i] == boneId) { Run.cup[i] = dieId; break; }
        }

        private void AddItemToOwned(ItemDefinition item)
        {
            foreach (var it in Run.ownedItems)
                if (it.itemId == item.id) { it.chargesRemaining += Math.Max(1, item.charges); return; }
            Run.ownedItems.Add(new OwnedItem { itemId = item.id, chargesRemaining = Math.Max(1, item.charges) });
        }

        // ---- The Fence (roguelike shop) ----

        /// <summary>One slot of the Fence's nightly stock.</summary>
        [Serializable]
        public class FenceOffer
        {
            public string id;
            public bool isItem;
            public int price;
            public bool sold;
        }

        private readonly System.Collections.Generic.List<FenceOffer> _fenceStock = new();
        private int _fenceRerolls;

        public System.Collections.Generic.IReadOnlyList<FenceOffer> FenceStock => _fenceStock;

        private void GenerateFenceStock()
        {
            _fenceRerolls = 0;
            PopulateStock();
        }

        private void PopulateStock()
        {
            _fenceStock.Clear();
            if (database == null) return;

            // Pool = unlocked dice + unlocked items (distinct), drawn without repeats up to 5.
            var pool = new System.Collections.Generic.List<(string id, bool isItem)>();
            foreach (var d in database.allDice)
                if (d != null && _save.account.IsUnlocked(d.id)) pool.Add((d.id, false));
            foreach (var it in database.allItems)
                if (it != null && _save.account.IsUnlocked(it.id)) pool.Add((it.id, true));
            if (pool.Count == 0) return;

            int night = CurrentNight != null ? CurrentNight.nightNumber : 1;
            int slots = Math.Min(5, pool.Count);
            for (int s = 0; s < slots; s++)
            {
                int pick = _rng.Range(0, pool.Count);
                var entry = pool[pick];
                pool.RemoveAt(pick); // no repeats this stock
                int price = entry.isItem
                    ? database.FindItem(entry.id).PriceAtNight(night)
                    : database.FindDie(entry.id).PriceAtNight(night);
                _fenceStock.Add(new FenceOffer { id = entry.id, isItem = entry.isItem, price = price, sold = false });
            }
        }

        /// <summary>Re-roll cost climbs each re-roll this night (~10% of tribute × 1.8^n); resets nightly.</summary>
        public int FenceRerollCost()
        {
            int demand = CurrentNight != null ? CurrentNight.collectionDemand : 20;
            double cost = demand * 0.10 * Math.Pow(1.8, _fenceRerolls);
            return Math.Max(1, (int)Math.Round(cost));
        }

        public bool TryRerollFence()
        {
            int cost = FenceRerollCost();
            if (Run == null || Run.bankroll < cost) return false;
            Run.bankroll -= cost;
            _fenceRerolls++;
            PopulateStock();
            SaveSystem.Save(_save);
            StateChanged?.Invoke();
            return true;
        }

        public bool TryBuyOffer(FenceOffer offer)
        {
            if (offer == null || offer.sold || Run == null || Run.bankroll < offer.price) return false;
            if (offer.isItem)
            {
                var item = database.FindItem(offer.id);
                if (item == null) return false;
                Run.bankroll -= offer.price;
                AddItemToOwned(item);
            }
            else
            {
                var die = database.FindDie(offer.id);
                if (die == null) return false;
                Run.bankroll -= offer.price;
                AddDieToOwned(offer.id);
            }
            offer.sold = true;
            SaveSystem.Save(_save);
            StateChanged?.Invoke();
            return true;
        }

        // ---- Honing (level a die 1 -> maxLevel) ----

        public bool CanHone(string dieId)
        {
            var def = database.FindDie(dieId);
            var owned = FindOwnedDie(dieId);
            return def != null && owned != null && owned.level < def.maxLevel;
        }

        public int HoneCost(string dieId)
        {
            var def = database.FindDie(dieId);
            var owned = FindOwnedDie(dieId);
            if (def == null || owned == null) return int.MaxValue;
            int night = CurrentNight != null ? CurrentNight.nightNumber : 1;
            return Math.Max(1, Mathf.RoundToInt(def.PriceAtNight(night) * 0.75f * owned.level));
        }

        public bool TryHoneDie(string dieId)
        {
            if (!CanHone(dieId)) return false;
            int cost = HoneCost(dieId);
            if (Run.bankroll < cost) return false;
            Run.bankroll -= cost;
            FindOwnedDie(dieId).level++;
            SaveSystem.Save(_save);
            StateChanged?.Invoke();
            return true;
        }

        private OwnedDie FindOwnedDie(string dieId)
        {
            foreach (var d in Run.ownedDice) if (d.dieId == dieId) return d;
            return null;
        }

        /// <summary>Dice the player can equip: the free Bone die plus everything owned (distinct).</summary>
        public System.Collections.Generic.List<string> EquippableDieIds()
        {
            var list = new System.Collections.Generic.List<string>();
            string boneId = database != null && database.boneDie != null ? database.boneDie.id : "bone";
            list.Add(boneId);
            foreach (var owned in Run.ownedDice)
                if (!list.Contains(owned.dieId)) list.Add(owned.dieId);
            return list;
        }

        public string CupSlot(int slot) => (Run != null && slot >= 0 && slot < 3) ? Run.cup[slot] : null;

        /// <summary>Swap a die into a cup slot (the Bag). Persists and notifies the UI.</summary>
        public void SetCupSlot(int slot, string dieId)
        {
            if (Run == null || slot < 0 || slot >= 3) return;
            Run.cup[slot] = dieId;
            SaveSystem.Save(_save);
            StateChanged?.Invoke();
        }

        public void BeginNight()
        {
            Night.ResetForNewNight();
            GenerateFenceStock();
            NightStarted?.Invoke();
            StateChanged?.Invoke();
        }

        // ---- Playing a game ----

        public bool CanPlayGame()
        {
            var night = CurrentNight;
            if (night == null || Run == null) return false;
            // The Reckoning: no broke/collection path tonight (spec §6.5). You play out the match;
            // it ends the instant a side clinches 2 of 3.
            if (night.isReckoning)
                return !ReckoningService.MatchResolved(Night.reckoningWins, Night.reckoningLosses)
                    && Night.gamesPlayed < night.gamesThisNight;
            return Night.gamesPlayed < night.gamesThisNight && !EconomyService.IsBroke(Run.bankroll);
        }

        public int MaxStake() => EconomyService.MaxStake(Run.bankroll);

        /// <summary>Play one Cee-lo round at the given stake. Returns the fully-resolved report.</summary>
        public GameReport PlayGame(int desiredStake, bool layLow)
        {
            var night = CurrentNight;
            int stake = EconomyService.ClampStake(desiredStake, Run.bankroll);

            var (d0, d1, d2) = CupSpecs();
            double loading = night != null ? night.opponentLoading : 0f;
            TieRule tie = night != null ? night.tieRule : TieRule.Banker;

            // The Reckoning: Vito saves his nastiest bones for the 1-1 deciding game (spec §6.5).
            bool reckoning = night != null && night.isReckoning;
            if (reckoning)
                loading = ReckoningService.EffectiveLoading(loading, Night.reckoningWins, Night.reckoningLosses);

            var round = RoundService.PlayRound(_rng, d0, d1, d2, loading, tie);
            int winsBefore = Night.consecutiveWins;
            double heat = EconomyService.Heat(winsBefore);

            double delta = EconomyService.Settle(round.Outcome, stake, round.Banker.Result, winsBefore, _payouts);
            bool win = round.Outcome == GameOutcome.BankerWin;
            if (win)
            {
                // Roll each cup charm's proc here (RNG stays in the controller), then let the pure
                // CharmService decide the charm-adjusted Heat and payout multiplier for this win.
                var charm = CharmService.Apply(heat, round.Banker.Result, ProccedCharms());
                // Re-base the win off the charm-adjusted Heat (Settle used the base Heat), then scale
                // by the payout multiplier. PayoutCharm-only behaviour stays identical to before.
                if (heat > 0) delta *= charm.Heat / heat;
                delta *= charm.PayoutMultiplier;
                heat = charm.Heat; // the report shows the Heat actually applied to this game
            }

            bool busted = false;
            // Suspicion is OFF during the Reckoning: both sides cheat openly, no bust checks (spec §6.5).
            // Cheats still work; we just never roll the bust and never spend Favor charges.
            if (win && !reckoning)
            {
                double bust = SuspicionService.BustChance(SummedSuspicion(), FavorReduction(), layLow);
                busted = SuspicionService.RollBust(_rng, bust);
                // A bust check actually occurred (winning settle, not laying low): spend favor charges.
                if (!layLow) ConsumeSuspicionFavors();
            }

            // Apply to bankroll.
            if (busted)
            {
                // Forfeit the staked pot (you keep your stake, lose the winnings) and Heat resets.
                _save.account.busts++;
                Night.OnLossOrBust();
            }
            else if (win)
            {
                Run.bankroll += (int)Math.Round(delta);
                Night.OnWin();
                _save.account.gamesWon++;
                if (Run.bankroll > _save.account.biggestPot) _save.account.biggestPot = Run.bankroll;
            }
            else if (round.Outcome == GameOutcome.BankerLoss)
            {
                Run.bankroll += (int)Math.Round(delta); // delta is negative
                Night.OnLossOrBust();
            }
            // Push: no change, Heat preserved.

            // The Reckoning is scored as a match: a clean banker win is your game, anything else is Vito's
            // (a push or loss does not advance you toward the 2 wins you need). Money still banks above.
            if (reckoning)
                Night.OnReckoningGame(win && !busted);

            // Track the banker's headline hands for achievement triggers (counts on any outcome:
            // rolling a 4-5-6 or a triple is the feat, win or not).
            var bankerKind = round.Banker.Result.Kind;
            if (bankerKind == CeeloKind.FourFiveSix) _save.account.fourFiveSixCount++;
            else if (bankerKind == CeeloKind.Triple) _save.account.tripleCount++;

            Night.gamesPlayed++;
            _save.account.fenceUnlocked = true; // unlocks after the first resolved game
            SaveSystem.Save(_save);

            var report = new GameReport
            {
                Round = round,
                Stake = stake,
                Delta = busted ? 0 : delta,
                Busted = busted,
                BankrollAfter = Run.bankroll,
                Heat = heat,
                WasWin = win && !busted,
            };

            EvaluateAchievements(AchievementEvent.GamePlayed, report);

            GamePlayed?.Invoke(report);
            StateChanged?.Invoke();
            return report;
        }

        public bool IsBroke => Run != null && EconomyService.IsBroke(Run.bankroll);

        /// <summary>Going broke mid-night (can't make a stake) ends the run immediately.</summary>
        public void DeclareBrokeIfNeeded()
        {
            if (IsBroke) EndRun(RunEndReason.Broke);
        }

        public bool NightComplete()
        {
            var night = CurrentNight;
            if (night == null) return false;
            // The Reckoning completes early the moment a side clinches the best-of-three.
            if (night.isReckoning && ReckoningService.MatchResolved(Night.reckoningWins, Night.reckoningLosses))
                return true;
            return Night.gamesPlayed >= night.gamesThisNight;
        }

        // ---- Collection / night transition ----

        /// <summary>
        /// The Reckoning finale (spec §6.5): no collection. Win the best-of-three vs Vito for
        /// Freedom; otherwise it is a death (Whacked). Called once the match has resolved.
        /// </summary>
        public void ResolveReckoning()
        {
            EndRun(ReckoningService.PlayerWonMatch(Night.reckoningWins, Night.reckoningLosses)
                ? RunEndReason.Victory
                : RunEndReason.Whacked);
        }

        public void ResolveCollection()
        {
            var night = CurrentNight;
            if (night == null) { EndRun(RunEndReason.Victory); return; }

            // No tribute on the Reckoning night: resolve the marker showdown instead.
            if (night.isReckoning) { ResolveReckoning(); return; }

            if (!EconomyService.CanMeetCollection(Run.bankroll, night.collectionDemand))
            {
                CollectionResolved?.Invoke(false);
                EndRun(RunEndReason.Whacked);
                return;
            }

            Run.bankroll -= night.collectionDemand;
            CollectionResolved?.Invoke(true);

            // You cleared this night: record the deepest night reached, then check survival achievements.
            if (night.nightNumber > _save.account.highestNightReached)
                _save.account.highestNightReached = night.nightNumber;
            EvaluateAchievements(AchievementEvent.NightSurvived);

            Run.nightIndex++;
            SaveSystem.Save(_save);

            if (Run.nightIndex >= database.campaign.NightCount)
            {
                EndRun(RunEndReason.Victory);
                return;
            }

            if (EconomyService.IsBroke(Run.bankroll))
            {
                EndRun(RunEndReason.Broke);
                return;
            }

            BeginNight();
        }

        private void EndRun(RunEndReason reason)
        {
            if (reason == RunEndReason.Victory) _save.account.runsWon++;
            else _save.account.deaths++;

            _save.hasActiveRun = false;
            _save.activeRun = null;
            SaveSystem.Save(_save);

            // End-of-run achievements (depth reached this run, lifetime death/win counts).
            EvaluateAchievements(AchievementEvent.RunEnded);

            RunEnded?.Invoke(reason);
        }

        // ---- Cup / charms ----

        private (DieSpec, DieSpec, DieSpec) CupSpecs()
        {
            return (SpecForSlot(0), SpecForSlot(1), SpecForSlot(2));
        }

        private DieSpec SpecForSlot(int slot)
        {
            string id = Run.cup[slot];
            var def = database.FindDie(id) ?? database.boneDie;
            int level = LevelOf(id);
            return def != null ? def.ToSpec(level) : DieSpec.Bone();
        }

        private int LevelOf(string id)
        {
            foreach (var owned in Run.ownedDice)
                if (owned.dieId == id) return owned.level;
            return 1;
        }

        private double SummedSuspicion()
        {
            double sum = 0;
            for (int i = 0; i < 3; i++)
            {
                var def = database.FindDie(Run.cup[i]);
                if (def != null) sum += def.suspicion;
            }
            return sum;
        }

        /// <summary>Adapt owned suspicion_reduce favors to plain data for the pure FavorService.</summary>
        private System.Collections.Generic.IEnumerable<(double magnitude, int charges)> SuspicionFavors()
        {
            foreach (var owned in Run.ownedItems)
            {
                var def = database.FindItem(owned.itemId);
                if (def != null && def.effectTag == "suspicion_reduce")
                    yield return (def.magnitude, owned.chargesRemaining);
            }
        }

        /// <summary>Total bust-% reduction from active (charged) suspicion favors. Spec §9.2.</summary>
        private double FavorReduction() => FavorService.SuspicionReduction(SuspicionFavors());

        /// <summary>Spend one charge from each contributing suspicion_reduce favor (floor 0).</summary>
        private void ConsumeSuspicionFavors()
        {
            foreach (var owned in Run.ownedItems)
            {
                if (owned.chargesRemaining <= 0) continue;
                var def = database.FindItem(owned.itemId);
                if (def != null && def.effectTag == "suspicion_reduce")
                    owned.chargesRemaining = Math.Max(0, owned.chargesRemaining - 1);
            }
        }

        /// <summary>
        /// Roll each cup charm's proc and return the effects that fired. Charms (PayoutCharm,
        /// HeatCharm, JackpotCharm) carry no face effect; CharmService turns the fired list into
        /// the adjusted Heat and payout multiplier. RNG lives here so CharmService stays pure.
        /// </summary>
        private System.Collections.Generic.List<DieEffect> ProccedCharms()
        {
            var fired = new System.Collections.Generic.List<DieEffect>(3);
            for (int i = 0; i < 3; i++)
            {
                var def = database.FindDie(Run.cup[i]);
                if (def == null) continue;
                bool isCharm = def.effect is DieEffect.PayoutCharm or DieEffect.HeatCharm or DieEffect.JackpotCharm;
                if (isCharm && _rng.Chance(def.ProcChanceAtLevel(LevelOf(def.id))))
                    fired.Add(def.effect);
            }
            return fired;
        }
    }
}
