using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using Bones.Core;
using Bones.Data;
using Bones.Juice;
using Bones.Presentation;

namespace Bones.UI
{
    /// <summary>
    /// Drives the whole front-end: the Spot HUD, the throw flow (choreograph → juice → settle),
    /// and the Title / Fence / Collection / Game-Over overlays. Pure UI Toolkit. Reads everything
    /// it needs from the GameController and the resolved GameReport (outcome-first).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class GameUI : MonoBehaviour
    {
        [SerializeField] private GameController game;
        [SerializeField] private DiceRollChoreographer choreographer;
        [SerializeField] private JuiceDirector juice;
        [SerializeField] private AudioDirector audioDirector;

        private VisualElement _root;
        private int _stake = 1;
        private bool _busy;

        // Cached elements
        private Label _debt, _deadline, _night, _heat, _bankroll, _stakeValue, _sfx, _result, _collectionText, _gameoverTitle, _gameoverText;
        private Button _throw, _stakeUp, _stakeDown, _bag, _fence, _newRun, _resumeRun, _collectionContinue, _fenceClose, _gameoverRestart, _bagClose, _fenceReroll;
        private Label _fenceCash;
        private Toggle _layLow;
        private VisualElement _overlayTitle, _overlayCollection, _overlayFence, _overlayGameover, _fenceStock, _overlayBag, _bagSlots, _fenceHone;

        private void Awake()
        {
            if (game == null) game = FindFirstObjectByType<GameController>();
            if (choreographer == null) choreographer = FindFirstObjectByType<DiceRollChoreographer>();
            if (juice == null) juice = FindFirstObjectByType<JuiceDirector>();
            if (audioDirector == null) audioDirector = FindFirstObjectByType<AudioDirector>();
            _root = GetComponent<UIDocument>().rootVisualElement;
            Cache();
            Wire();

            // Per-die CLACK feedback as each die lands.
            if (choreographer != null)
                choreographer.DieLanded += (slot, face) =>
                {
                    if (juice != null) juice.PlayClack();
                    if (audioDirector != null) audioDirector.Clack();
                };
        }

        private void OnEnable()
        {
            if (game != null)
            {
                game.StateChanged += RefreshHud;
                game.RunEnded += OnRunEnded;
            }
        }

        private void OnDisable()
        {
            if (game != null)
            {
                game.StateChanged -= RefreshHud;
                game.RunEnded -= OnRunEnded;
            }
        }

        private void Start() => ShowTitle();

        private void Cache()
        {
            _debt = _root.Q<Label>("debt-value");
            _deadline = _root.Q<Label>("deadline-value");
            _night = _root.Q<Label>("night-value");
            _heat = _root.Q<Label>("heat-value");
            _bankroll = _root.Q<Label>("bankroll-value");
            _stakeValue = _root.Q<Label>("stake-value");
            _sfx = _root.Q<Label>("sfx-letters");
            _result = _root.Q<Label>("result-readout");
            _collectionText = _root.Q<Label>("collection-text");
            _gameoverTitle = _root.Q<Label>("gameover-title");
            _gameoverText = _root.Q<Label>("gameover-text");

            _throw = _root.Q<Button>("throw-button");
            _stakeUp = _root.Q<Button>("stake-up");
            _stakeDown = _root.Q<Button>("stake-down");
            _bag = _root.Q<Button>("bag-button");
            _fence = _root.Q<Button>("fence-button");
            _newRun = _root.Q<Button>("new-run");
            _resumeRun = _root.Q<Button>("resume-run");
            _collectionContinue = _root.Q<Button>("collection-continue");
            _fenceClose = _root.Q<Button>("fence-close");
            _gameoverRestart = _root.Q<Button>("gameover-restart");
            _bagClose = _root.Q<Button>("bag-close");
            _layLow = _root.Q<Toggle>("lay-low");

            _overlayTitle = _root.Q<VisualElement>("overlay-title");
            _overlayCollection = _root.Q<VisualElement>("overlay-collection");
            _overlayFence = _root.Q<VisualElement>("overlay-fence");
            _overlayGameover = _root.Q<VisualElement>("overlay-gameover");
            _fenceStock = _root.Q<VisualElement>("fence-stock");
            _overlayBag = _root.Q<VisualElement>("overlay-bag");
            _bagSlots = _root.Q<VisualElement>("bag-slots");
            _fenceHone = _root.Q<VisualElement>("fence-hone");
            _fenceReroll = _root.Q<Button>("fence-reroll");
            _fenceCash = _root.Q<Label>("fence-cash");
        }

        private void Wire()
        {
            _throw.clicked += OnThrow;
            _stakeUp.clicked += () => AdjustStake(+1);
            _stakeDown.clicked += () => AdjustStake(-1);
            _fence.clicked += ShowFence;
            _fenceClose.clicked += () => Hide(_overlayFence);
            _fenceReroll.clicked += () => { if (game.TryRerollFence()) ShowFence(); };
            _collectionContinue.clicked += OnCollectionContinue;
            _newRun.clicked += OnNewRun;
            _resumeRun.clicked += OnResume;
            _gameoverRestart.clicked += OnNewRun;
            _bag.clicked += ShowBag;
            _bagClose.clicked += () => Hide(_overlayBag);

            WireFlick();
        }

        // Flick the cup up to throw (spec §12.2): swipe up on the stage. The button is the fallback.
        private float _flickStartY;
        private bool _flicking;
        private void WireFlick()
        {
            var stage = _root.Q<VisualElement>("stage");
            if (stage == null) return;
            stage.RegisterCallback<PointerDownEvent>(e => { _flicking = true; _flickStartY = e.position.y; });
            stage.RegisterCallback<PointerUpEvent>(e =>
            {
                if (!_flicking) return;
                _flicking = false;
                if (_flickStartY - e.position.y > 60f) OnThrow(); // moved up far enough
            });
        }

        // ---- Title / run lifecycle ----

        private void ShowTitle()
        {
            Show(_overlayTitle);
            Hide(_overlayCollection); Hide(_overlayFence); Hide(_overlayGameover);
            SetControlsEnabled(false); // no HUD interaction until a run starts
            // Resume only if a run is in progress.
            bool hasRun = game != null && game.TryPeekActiveRun();
            _resumeRun.style.display = hasRun ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnNewRun()
        {
            Hide(_overlayGameover); Hide(_overlayTitle);
            game.StartNewRun();
            _stake = 1;
            RefreshHud();
            SetControlsEnabled(true);
        }

        private void OnResume()
        {
            Hide(_overlayTitle);
            game.TryResumeRun();
            _stake = 1;
            RefreshHud();
            SetControlsEnabled(true);
        }

        // ---- The throw flow ----

        private void OnThrow()
        {
            if (_busy || game == null || !game.CanPlayGame()) return;
            StartCoroutine(ThrowRoutine());
        }

        private IEnumerator ThrowRoutine()
        {
            _busy = true;
            SetControlsEnabled(false);
            _sfx.text = "";
            _result.text = "";

            bool layLow = _layLow != null && _layLow.value;
            var report = game.PlayGame(_stake, layLow);

            // Reveal the banker's throw (the heartbeat).
            bool done = false;
            yield return choreographer.RevealBanker(report.Round.Banker, game.Run.cup, () => done = true);
            while (!done) yield return null;
            PopLetters(LettersFor(report.Round.Banker.Result, false));

            // If a point was set, the mark counter-rolls.
            if (report.Round.MarkRolled)
            {
                _result.text = $"Your point: {report.Round.Banker.Result.Value}.  The mark fades it…";
                yield return new WaitForSeconds(0.5f);
                done = false;
                yield return choreographer.RevealMark(report.Round.Mark, () => done = true);
                while (!done) yield return null;
            }

            // Settle: juice + readout.
            var tier = juice != null ? juice.TierFor(report.Round.Banker.Result, report.Heat, report.Busted) : JuiceTier.PointWin;
            if (report.Busted)
            {
                PopLetters("BUSTED!");
                _result.text = "Caught loading the bones. Pot forfeit. Heat's gone cold.";
            }
            else if (report.WasWin)
            {
                PopLetters(LettersFor(report.Round.Banker.Result, true));
                _result.text = $"+${(int)System.Math.Round(report.Delta)}   (Heat ×{report.Heat:0.0})";
                if (audioDirector != null) audioDirector.Coins();
            }
            else
            {
                _result.text = $"-${report.Stake}.  The mark takes it.";
            }
            if (juice != null) juice.Play(tier);
            if (audioDirector != null) { audioDirector.Stinger(tier); audioDirector.SetHeat(report.Heat); }

            RefreshHud();
            yield return new WaitForSeconds(1.1f);

            _busy = false;
            // Night over? -> Collection. Broke mid-night -> game over. Else keep playing.
            if (game.NightComplete()) ShowCollection();
            else if (game.IsBroke) game.DeclareBrokeIfNeeded();
            else SetControlsEnabled(true);
        }

        // ---- Collection ----

        private void ShowCollection()
        {
            var night = game.CurrentNight;
            bool canPay = night != null && game.Run.bankroll >= night.collectionDemand;
            _collectionText.text = night == null
                ? "The marker is settled."
                : canPay
                    ? $"The Collector wants ${night.collectionDemand}. You're holding ${game.Run.bankroll}."
                    : $"The Collector wants ${night.collectionDemand}. You've only got ${game.Run.bankroll}. This is bad.";
            Show(_overlayCollection);
        }

        private void OnCollectionContinue()
        {
            Hide(_overlayCollection);
            game.ResolveCollection(); // fires RunEnded on death/victory, else begins next night
            if (game.Run != null && game.CurrentNight != null)
            {
                RefreshHud();
                SetControlsEnabled(true);
            }
        }

        // ---- Bag (cup loadout) ----

        private void ShowBag()
        {
            if (_busy || game == null || game.Run == null) return;
            _bagSlots.Clear();
            for (int slot = 0; slot < 3; slot++)
            {
                int captured = slot;
                var def = game.Database.FindDie(game.CupSlot(slot));
                var row = new VisualElement(); row.AddToClassList("fence-item");
                var label = new Label(SlotLabel(captured, def));
                label.style.flexGrow = 1f;
                label.style.whiteSpace = WhiteSpace.Normal;
                var swap = new Button(() => CycleSlot(captured)) { text = "SWAP" };
                swap.AddToClassList("fence-buy");
                row.Add(label); row.Add(swap);
                _bagSlots.Add(row);
            }

            // Owned favors / items, read-only, with remaining charges (e.g. "Lookout  x2").
            foreach (var owned in game.Run.ownedItems)
            {
                var item = game.Database.FindItem(owned.itemId);
                if (item == null) continue;
                var row = new VisualElement(); row.AddToClassList("fence-item");
                var label = new Label($"{item.displayName}  x{owned.chargesRemaining}");
                label.style.flexGrow = 1f;
                label.style.whiteSpace = WhiteSpace.Normal;
                row.Add(label);
                _bagSlots.Add(row);
            }
            Show(_overlayBag);
        }

        private string SlotLabel(int slot, DieDefinition def)
        {
            if (def == null) return $"Slot {slot + 1}:  (empty)";
            string effect = def.effect == Bones.Core.DieEffect.None ? "no special effect" : def.effect.ToString();
            return $"Slot {slot + 1}:  {def.displayName}\n{effect} · {def.flavor}";
        }

        private void CycleSlot(int slot)
        {
            var ids = game.EquippableDieIds();
            if (ids.Count == 0) return;
            int idx = ids.IndexOf(game.CupSlot(slot));
            string next = ids[(idx + 1) % ids.Count];
            game.SetCupSlot(slot, next);
            if (choreographer != null) choreographer.ResetDice(); // rebuild visuals with the new die
            ShowBag(); // refresh the rows
        }

        // ---- Fence (tiny MVP shop) ----

        private void ShowFence()
        {
            if (_busy || game == null || game.Run == null) return;
            var db = game.Database;
            _fenceCash.text = $"Bankroll ${game.Run.bankroll}   ·   {game.CurrentNight?.collectionDemand ?? 0} due";

            // Stock: 5 random slots from the unlocked pool.
            _fenceStock.Clear();
            foreach (var offer in game.FenceStock)
            {
                var captured = offer;
                string name = offer.isItem ? db.FindItem(offer.id)?.displayName : db.FindDie(offer.id)?.displayName;
                var row = new VisualElement(); row.AddToClassList("fence-item");
                var label = new Label(offer.sold ? $"{name}  —  SOLD" : $"{name}  —  ${offer.price}");
                label.style.flexGrow = 1f;
                var buy = new Button(() => { if (game.TryBuyOffer(captured)) ShowFence(); }) { text = "BUY" };
                buy.AddToClassList("fence-buy");
                buy.SetEnabled(!offer.sold && game.Run.bankroll >= offer.price);
                row.Add(label); row.Add(buy);
                _fenceStock.Add(row);
            }

            // Re-roll.
            int rerollCost = game.FenceRerollCost();
            _fenceReroll.text = $"RE-ROLL THE STOCK  —  ${rerollCost}";
            _fenceReroll.SetEnabled(game.Run.bankroll >= rerollCost);

            // Honing: owned dice below max level.
            _fenceHone.Clear();
            foreach (var owned in game.Run.ownedDice)
            {
                if (!game.CanHone(owned.dieId)) continue;
                var capturedId = owned.dieId;
                var def = db.FindDie(owned.dieId);
                int cost = game.HoneCost(owned.dieId);
                var row = new VisualElement(); row.AddToClassList("fence-item");
                var label = new Label($"{def.displayName}  Lv.{owned.level} → {owned.level + 1}  —  ${cost}");
                label.style.flexGrow = 1f;
                var hone = new Button(() => { if (game.TryHoneDie(capturedId)) ShowFence(); }) { text = "HONE" };
                hone.AddToClassList("fence-buy");
                hone.SetEnabled(game.Run.bankroll >= cost);
                row.Add(label); row.Add(hone);
                _fenceHone.Add(row);
            }

            Show(_overlayFence);
        }

        // ---- Game over ----

        private void OnRunEnded(RunEndReason reason)
        {
            SetControlsEnabled(false);
            _gameoverTitle.text = reason == RunEndReason.Victory ? "FREEDOM" : "WHACKED";
            _gameoverText.text = reason switch
            {
                RunEndReason.Victory => "You burned the marker. Dawn's breaking. You're free.",
                RunEndReason.Broke => "Cleaned out. Can't make a stake. The street swallows you.",
                RunEndReason.Whacked => "You missed the Collection. They find you by the river.",
                _ => "",
            };
            Show(_overlayGameover);
        }

        // ---- HUD ----

        private void RefreshHud()
        {
            if (game == null || game.Run == null) return;
            var night = game.CurrentNight;
            _bankroll.text = $"${game.Run.bankroll}";
            if (night != null)
            {
                _debt.text = $"${night.collectionDemand}";
                _night.text = $"NIGHT {night.nightNumber}";
                int left = Mathf.Max(0, night.gamesThisNight - game.Night.gamesPlayed);
                _deadline.text = $"{left} throw{(left == 1 ? "" : "s")} left";
            }
            _heat.text = $"HEAT ×{EconomyService.Heat(game.Night.consecutiveWins):0.0}";
            _stake = EconomyService.ClampStake(_stake, game.Run.bankroll);
            _stakeValue.text = $"${_stake}";
        }

        private void AdjustStake(int dir)
        {
            if (game == null || game.Run == null) return;
            int step = _stake < 10 ? 1 : _stake < 50 ? 5 : 25;
            _stake = EconomyService.ClampStake(_stake + dir * step, game.Run.bankroll);
            _stakeValue.text = $"${_stake}";
        }

        private void SetControlsEnabled(bool on)
        {
            bool canPlay = on && game != null && game.CanPlayGame();
            _throw.SetEnabled(canPlay);
            _stakeUp.SetEnabled(on); _stakeDown.SetEnabled(on);
            _fence.SetEnabled(on); _bag.SetEnabled(on);
            if (_layLow != null) _layLow.SetEnabled(on);
        }

        // ---- Helpers ----

        private static string LettersFor(CeeloResult r, bool asWin) => r.Kind switch
        {
            CeeloKind.Triple => r.Value == 6 ? "SIX-SIX-SIX!" : "TRIPS!",
            CeeloKind.FourFiveSix => "HEADCRACK!",
            CeeloKind.InstantLoss => "SNAKE EYES!",
            CeeloKind.Point => asWin ? "PAID!" : $"POINT {r.Value}",
            _ => "",
        };

        private void PopLetters(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            _sfx.text = text;
            _sfx.style.opacity = 1f;
            _sfx.experimental.animation.Start(1f, 0f, 700, (e, v) => ((VisualElement)e).style.opacity = v);
        }

        private void Show(VisualElement v) { if (v != null) v.style.display = DisplayStyle.Flex; }
        private void Hide(VisualElement v) { if (v != null) v.style.display = DisplayStyle.None; }
    }
}
