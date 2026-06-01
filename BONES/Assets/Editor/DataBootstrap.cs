using System.IO;
using UnityEditor;
using UnityEngine;
using Bones.Core;
using Bones.Data;

namespace Bones.Editor
{
    /// <summary>
    /// One-click generator for the MVP content set so you never hand-author ScriptableObjects.
    /// Menu: BONES ▸ Generate MVP Data. Creates dice, an item, Nights 1–7 (Night 7 = the Reckoning), juice tiers, the
    /// campaign, and the wired GameDatabase under Assets/Data.
    /// </summary>
    public static class DataBootstrap
    {
        private const string DiceDir = "Assets/Data/Dice";
        private const string ItemsDir = "Assets/Data/Items";
        private const string NightsDir = "Assets/Data/Nights";

        [MenuItem("BONES/Generate MVP Data")]
        public static void Generate()
        {
            EnsureDir(DiceDir); EnsureDir(ItemsDir); EnsureDir(NightsDir);

            // --- Dice (starting core pool + bone default) ---
            var bone = MakeDie("bone", "Bone Die", DieTier.Common, DieEffect.None,
                "A plain street die. Honest. Mostly.", new[] { 0f }, 0f, 0, 1);

            var snakeKiller = MakeDie("snake_killer", "Snake Killer", DieTier.Loaded, DieEffect.KillInstantLoss,
                "Eats a 1-2-3 before the marks ever see it.", new[] { 0.35f, 0.50f, 0.65f }, 0.012f, 6, 3);

            var luckySix = MakeDie("lucky_six", "Lucky Six", DieTier.Loaded, DieEffect.BiasSix,
                "Wants to land on six. Always has.", new[] { 0.30f, 0.42f, 0.55f }, 0.015f, 7, 3);

            var gildedDie = MakeDie("gilded_die", "Gilded Die", DieTier.Charm, DieEffect.PayoutCharm,
                "Pays a little extra when the night goes your way.", new[] { 0.30f, 0.40f, 0.50f }, 0f, 8, 3);

            var streakCharm = MakeDie("streak_charm", "Streak Charm", DieTier.Charm, DieEffect.HeatCharm,
                "Keeps the table warm so a hot hand runs hotter.", new[] { 0.30f, 0.40f, 0.50f }, 0f, 8, 3);

            var headcracker = MakeDie("headcracker", "Headcracker", DieTier.Charm, DieEffect.JackpotCharm,
                "When the bones hit big (4-5-6 or a triple), it cracks the pot wide open.", new[] { 0.40f, 0.55f, 0.70f }, 0f, 9, 3);

            // ============================================================================
            // Catalog §11 build-out. Items below are split into two groups:
            //   IMPLEMENTED  - map cleanly to an existing effect and go into startingUnlocks.
            //   DEFERRED     - carry a no-op stub effect/tag (// TODO) until their system exists;
            //                  registered in the catalog but kept OUT of startingUnlocks.
            // Do not change the 6 dice above or the Lookout favor; only add.
            // ============================================================================

            // --- Loaded dice: IMPLEMENTED (clean maps to existing face/hand effects) ---
            var shavedEdge = MakeDie("shaved_edge", "Shaved Edge", DieTier.Loaded, DieEffect.BiasHigh,
                "A whisper off one corner. The bones just lean your way.", new[] { 0.40f, 0.50f, 0.60f }, 0.005f, 10, 3);

            var theSequencer = MakeDie("the_sequencer", "The Sequencer", DieTier.Loaded, DieEffect.ForceFourFiveSix,
                "Two of the right faces showing, and it finishes the run for you: four, five, six.", new[] { 0.16f, 0.25f, 0.34f }, 0.018f, 48, 3);

            var theMagnet = MakeDie("the_magnet", "The Magnet", DieTier.Loaded, DieEffect.ForceTriple,
                "A pair on the pavement pulls the third bone home. Triples, if it bites.", new[] { 0.15f, 0.23f, 0.32f }, 0.020f, 55, 3);

            // --- Loaded dice: DEFERRED (no clean map; stub effects, await resolver work) ---
            var evenSteven = MakeDie("even_steven", "Even Steven", DieTier.Loaded, DieEffect.BumpParityEven,
                "Nudges an odd face up into an even one. Keeps things tidy.", new[] { 0.30f, 0.41f, 0.52f }, 0.0075f, 12, 3);

            var twoFace = MakeDie("two_face", "Two-Face", DieTier.Loaded, DieEffect.BiasOneOrSix,
                "Only ever a one or a six. Nothing in between. Wild either way.", new[] { 1f }, 0.008f, 18, 1);

            var highRoller = MakeDie("high_roller", "High Roller", DieTier.Loaded, DieEffect.BumpLowFaces,
                "Catches a low roll and shoves it up two pips.", new[] { 0.24f, 0.35f, 0.46f }, 0.015f, 22, 3);

            var matchmaker = MakeDie("matchmaker", "Matchmaker", DieTier.Loaded, DieEffect.CopyOtherDie,
                "Looks at the bones beside it and decides to match.", new[] { 0.18f, 0.27f, 0.36f }, 0.015f, 40, 3);

            // --- Control/trick dice: DEFERRED (need a re-roll / nudge / set-a-die loop) ---
            var theCooler = MakeDie("the_cooler", "The Cooler", DieTier.Trick, DieEffect.LockThroughReroll,
                "Hold this one steady while the rest go again.", new[] { 0.20f, 0.29f, 0.38f }, 0.002f, 30, 3);

            var rerollBone = MakeDie("reroll_bone", "Reroll Bone", DieTier.Trick, DieEffect.RerollSelf,
                "Didn't like how it landed? Throw it once more.", new[] { 0.18f, 0.27f, 0.36f }, 0.003f, 35, 3);

            var theNudge = MakeDie("the_nudge", "The Nudge", DieTier.Trick, DieEffect.NudgeSelf,
                "A thumb on the bone after it settles. One pip, up or down.", new[] { 0.14f, 0.22f, 0.30f }, 0.006f, 42, 3);

            var mulliganCup = MakeDie("mulligan_cup", "Mulligan Cup", DieTier.Trick, DieEffect.RerollHand,
                "Scoop the whole throw back into the cup and spill it fresh. Once.", new[] { 1f }, 0.005f, 30, 1);

            var secondWind = MakeDie("second_wind", "Second Wind", DieTier.Trick, DieEffect.ReviveInstantLoss,
                "A one-two-three should have buried you. It doesn't. Throw again.", new[] { 1f }, 0.004f, 28, 1);

            var setBone = MakeDie("set_bone", "Set-Bone", DieTier.Trick, DieEffect.SetFace,
                "Set the face yourself. Pure heresy, and worth every cent.", new[] { 1f }, 0.020f, 120, 1);

            // --- Charms: IMPLEMENTED (clean map to HeatCharm) ---
            var hotHand = MakeDie("hot_hand", "Hot Hand", DieTier.Charm, DieEffect.HeatCharm,
                "The streak runs hotter and cools slower while this is on you.", new[] { 1f }, 0f, 45, 1);

            // --- Charms: DEFERRED (need refund / point-bump economy hooks) ---
            var rabbitsDie = MakeDie("rabbits_die", "Rabbit's Die", DieTier.Charm, DieEffect.RefundOnLoss,
                "Lose, and a few bills find their way back to your fist.", new[] { 0.20f, 0.30f, 0.40f }, 0f, 26, 3);

            var pointSharp = MakeDie("point_sharp", "Point Sharp", DieTier.Charm, DieEffect.PointSharp,
                "Your point reads a notch higher than it has any right to.", new[] { 1f }, 0f, 35, 1);

            // --- Curse dice: DEFERRED (need a downside/penalty system) ---
            var gamblersCurse = MakeDie("gamblers_curse", "Gambler's Curse", DieTier.Curse, DieEffect.ForceBigStake,
                "While you're hot you can't help yourself: the stake has to be big.", new[] { 1f }, 0f, 8, 1);

            var allOrNothing = MakeDie("all_or_nothing", "All-or-Nothing", DieTier.Curse, DieEffect.DoubleSwing,
                "Double the winnings, double the bleeding. No middle ground.", new[] { 1f }, 0f, 10, 1);

            var snakeEyesPact = MakeDie("snake_eyes_pact", "Snake Eyes Pact", DieTier.Curse, DieEffect.SnakeEyesPact,
                "A fat multiplier, signed in blood: any one you roll kills the throw.", new[] { 1f }, 0f, 12, 1);

            var bloodyKnuckles = MakeDie("bloody_knuckles", "Bloody Knuckles", DieTier.Curse, DieEffect.BloodyKnuckles,
                "Heat comes faster, and the heat on you never cools.", new[] { 1f }, 0f, 14, 1);

            // --- Favor (the existing MVP item; do NOT change) ---
            var lookout = MakeItem("lookout", "Lookout", ItemKind.Favor, Durability.LimitedUse,
                "A kid on the corner whistles when the heat's near.", "suspicion_reduce", 0.02f, 2, 10);

            // --- Favors: IMPLEMENTED (clean map to suspicion_reduce) ---
            var greasedPalm = MakeItem("greased_palm", "Greased Palm", ItemKind.Favor, Durability.LimitedUse,
                "A few bills in the right hand and nobody saw a thing. For one game.", "suspicion_reduce", 0.05f, 1, 25);

            var coolerHead = MakeItem("cooler_head", "Cooler Head", ItemKind.Favor, Durability.Persistent,
                "You keep your cool, so the marks stay a touch calmer too.", "suspicion_reduce", 0.01f, 1, 30);

            // --- Favors: DEFERRED ---
            var coldRead = MakeItem("cold_read", "Cold Read", ItemKind.Favor, Durability.Persistent,
                "You read the mark like a cheap paper. And you read your own odds.", "reveal_odds", 0f, 1, 20); // TODO: needs UI readout of bust% + mark tell (no mechanical effect)

            var smoothTalker = MakeItem("smooth_talker", "Smooth Talker", ItemKind.Favor, Durability.LimitedUse,
                "Caught? You talk your way clean. Once a night.", "bust_negate", 1f, 1, 40); // TODO: needs bust-negation hook in SuspicionService/GameController

            // --- Trinkets: DEFERRED (need economy / debt / reprieve systems) ---
            var pawnTicket = MakeItem("pawn_ticket", "Pawn Ticket", ItemKind.Trinket, Durability.LimitedUse,
                "Hock a die for whatever cash it'll fetch. No questions.", "sell_die", 1f, 1, 5); // TODO: needs die sell-back economy

            var highRollersClip = MakeItem("high_rollers_clip", "High Roller's Clip", ItemKind.Trinket, Durability.Persistent,
                "A fat money clip says you can stake fatter.", "max_stake_up", 0.5f, 1, 25); // TODO: needs max-stake cap system

            var insuranceChit = MakeItem("insurance_chit", "Insurance Chit", ItemKind.Trinket, Durability.LimitedUse,
                "Half a loss comes back to you. Once a night.", "loss_recover", 0.5f, 1, 30); // TODO: needs loss-recovery hook in EconomyService

            var vigSkimmer = MakeItem("vig_skimmer", "Vig Skimmer", ItemKind.Trinket, Durability.Persistent,
                "You skim a sliver of the pot even when it walks away from you.", "loss_skim", 0.05f, 1, 35); // TODO: needs partial-pot-on-loss hook

            var luckyCigarette = MakeItem("lucky_cigarette", "Lucky Cigarette", ItemKind.Trinket, Durability.Persistent,
                "One on the house: a free look at the Fence's stock each night.", "free_reroll", 1f, 1, 35); // TODO: needs Fence re-roll system

            var loadedCoin = MakeItem("loaded_coin", "Loaded Coin", ItemKind.Trinket, Durability.LimitedUse,
                "Flat broke? Flip for it. Heads you stake again, tails you're done.", "broke_bailout", 0.5f, 1, 40); // TODO: needs broke-bailout 50/50 hook

            var rabbitsFoot = MakeItem("rabbits_foot", "Rabbit's Foot", ItemKind.Trinket, Durability.Persistent,
                "Your first stumble each night doesn't count.", "forgive_first_loss", 1f, 1, 55); // TODO: needs first-loss-forgive hook

            var brassKnuckles = MakeItem("brass_knuckles", "Brass Knuckles", ItemKind.Trinket, Durability.LimitedUse,
                "Miss the Collector once and walk away breathing. Once.", "reprieve", 1f, 1, 80); // TODO: needs missed-Collection reprieve system

            var vitosFavor = MakeItem("vitos_favor", "Vito's Favor", ItemKind.Trinket, Durability.LimitedUse,
                "Vito waves the vig for a night. He'll remember it.", "skip_interest", 1f, 1, 90); // TODO: needs debt-interest skip system

            var markerShaver = MakeItem("marker_shaver", "Marker Shaver", ItemKind.Trinket, Durability.Persistent,
                "The marker swells a little slower than it should. All run.", "debt_slow", 0.1f, 1, 110); // TODO: needs debt-growth scalar system

            // --- Juice tiers ---
            var juice = MakeJuice();

            // --- Nights 1–7 (the Squeeze: tie drifts banker -> push -> mark; mark loads harder each
            //     late night; Night 7 is the Reckoning vs Vito — no tribute, Suspicion off). Spec §6.3–6.5,
            //     ECONOMY.md §5. All numbers are tuning placeholders matching the docs. ---
            var n1 = MakeNight("Night_1", 1, 20, TieRule.Banker, 0.00f);
            var n2 = MakeNight("Night_2", 2, 55, TieRule.Push, 0.00f);
            var n3 = MakeNight("Night_3", 3, 140, TieRule.Mark, 0.12f);
            var n4 = MakeNight("Night_4", 4, 375, TieRule.Mark, 0.30f);
            var n5 = MakeNight("Night_5", 5, 950, TieRule.Mark, 0.50f);
            var n6 = MakeNight("Night_6", 6, 2400, TieRule.Mark, 0.65f);
            // The Reckoning: no ordinary collection (demand 0); Vito heavily loaded; flagged isReckoning.
            var n7 = MakeNight("Night_7", 7, 0, TieRule.Mark, 0.80f, true);

            var campaign = ScriptableObject.CreateInstance<CampaignConfig>();
            campaign.nights = new[] { n1, n2, n3, n4, n5, n6, n7 };
            CreateAsset(campaign, "Assets/Data/Campaign.asset");

            // --- The wired database ---
            var db = ScriptableObject.CreateInstance<GameDatabase>();
            db.boneDie = bone;
            db.allDice.AddRange(new[]
            {
                // Original MVP dice (unchanged)
                snakeKiller, luckySix, gildedDie, streakCharm, headcracker,
                // Loaded: implemented + deferred
                shavedEdge, theSequencer, theMagnet,
                evenSteven, twoFace, highRoller, matchmaker,
                // Control/trick (all deferred)
                theCooler, rerollBone, theNudge, mulliganCup, secondWind, setBone,
                // Charms: implemented + deferred
                hotHand, rabbitsDie, pointSharp,
                // Curses (all deferred)
                gamblersCurse, allOrNothing, snakeEyesPact, bloodyKnuckles,
            });
            db.allItems.AddRange(new[]
            {
                // Favors: existing + implemented + deferred
                lookout, greasedPalm, coolerHead, coldRead, smoothTalker,
                // Trinkets (all deferred)
                pawnTicket, highRollersClip, insuranceChit, vigSkimmer, luckyCigarette,
                loadedCoin, rabbitsFoot, brassKnuckles, vitosFavor, markerShaver,
            });
            db.campaign = campaign;
            db.juice = juice;
            db.showMetersInDev = true; // dev aid: Heat/Suspicion meters visible; flip OFF for release

            // Brand-new-account starting core ONLY (ACHIEVEMENTS.md "Starting core pool"): two
            // intuitive cheat dice plus one charm. Everything else implemented is earned through play
            // via Bones.Core.AchievementService (see AchievementService.ActiveTable):
            //   streak_charm  <- win your first game
            //   hot_hand      <- win 3 games
            //   headcracker   <- first 4-5-6
            //   the_magnet    <- first triple
            //   greased_palm  <- survive your first night
            //   shaved_edge   <- reach Night 3
            //   the_sequencer <- reach Night 5
            //   cooler_head   <- get busted
            // Deferred (stubbed) items stay out of both lists until their systems exist.
            db.startingUnlocks = new System.Collections.Generic.List<string>
            {
                "snake_killer", "lucky_six", "gilded_die",
            };
            EnsureDir("Assets/Resources");
            CreateAsset(db, "Assets/Resources/GameDatabase.asset"); // in Resources so GameController can auto-load it
            // Remove any stale copy from the old location.
            if (AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/Data/GameDatabase.asset") != null)
                AssetDatabase.DeleteAsset("Assets/Data/GameDatabase.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = db;
            Debug.Log("[BONES] MVP data generated under Assets/Data. Select GameDatabase.asset.");
            EditorUtility.DisplayDialog("BONES",
                "MVP data generated under Assets/Data:\n" +
                "• 26 dice total (1 bone + 25 catalog dice: loaded, control, charm, curse)\n" +
                "• 15 items total (5 favors + 10 trinkets)\n" +
                "• 3 starting unlocks (Snake Killer, Lucky Six, Gilded Die); other implemented\n" +
                "  items are earned via achievements; stubbed items await their systems (see CATALOG.md)\n" +
                "• Nights 1–7 (Night 7 = the Reckoning) + Campaign\n" +
                "• JuiceTiers + GameDatabase (wired)\n\n" +
                "Next: BONES ▸ Build Playable Scene.", "OK");
        }

        private static DieDefinition MakeDie(string id, string name, DieTier tier, DieEffect effect,
            string flavor, float[] proc, float suspicion, int price, int maxLevel)
        {
            var d = ScriptableObject.CreateInstance<DieDefinition>();
            d.id = id; d.displayName = name; d.tier = tier; d.effect = effect; d.flavor = flavor;
            d.procChanceByLevel = proc; d.suspicion = suspicion; d.basePrice = price; d.maxLevel = maxLevel;
            CreateAsset(d, $"{DiceDir}/Die_{id}.asset");
            return d;
        }

        private static ItemDefinition MakeItem(string id, string name, ItemKind kind, Durability dur,
            string flavor, string tag, float mag, int charges, int price)
        {
            var i = ScriptableObject.CreateInstance<ItemDefinition>();
            i.id = id; i.displayName = name; i.kind = kind; i.durability = dur; i.flavor = flavor;
            i.effectTag = tag; i.magnitude = mag; i.charges = charges; i.basePrice = price;
            CreateAsset(i, $"{ItemsDir}/Item_{id}.asset");
            return i;
        }

        private static NightConfig MakeNight(string asset, int number, int demand, TieRule tie, float loading,
            bool isReckoning = false)
        {
            var n = ScriptableObject.CreateInstance<NightConfig>();
            n.nightNumber = number; n.collectionDemand = demand; n.tieRule = tie;
            n.opponentLoading = loading; n.gamesThisNight = 3; n.isReckoning = isReckoning;
            CreateAsset(n, $"{NightsDir}/{asset}.asset");
            return n;
        }

        private static JuiceTierConfig MakeJuice()
        {
            var j = ScriptableObject.CreateInstance<JuiceTierConfig>();
            j.tiers = new[]
            {
                Tier(JuiceTier.Clack,    0.03f, 2,  0.0f, new Color(1,1,1,0.0f), 0.0f, false),
                Tier(JuiceTier.PointWin, 0.10f, 12, 0.0f, new Color(0.83f,0.66f,0.22f,0.2f), 0.3f, false),
                Tier(JuiceTier.Hot,      0.25f, 40, 0.3f, new Color(1f,0.55f,0.1f,0.4f), 0.7f, false),
                Tier(JuiceTier.Jackpot,  0.55f, 120,0.6f, new Color(1f,0.85f,0.3f,0.7f), 1.2f, true),
                Tier(JuiceTier.Bust,     0.40f, 0,  0.5f, new Color(0.6f,0.05f,0.07f,0.7f), 0.0f, true),
            };
            CreateAsset(j, "Assets/Data/JuiceTiers.asset");
            return j;
        }

        private static JuiceTierConfig.TierParams Tier(JuiceTier t, float shake, int parts, float dip,
            Color flash, float swell, bool fracture) => new JuiceTierConfig.TierParams
        {
            tier = t, screenShake = shake, particleBurst = parts, timeScaleDip = dip,
            flashColor = flash, musicSwell = swell, panelFracture = fracture
        };

        private static void EnsureDir(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        private static void CreateAsset(Object obj, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(obj, path);
        }
    }
}
