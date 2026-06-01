using System.IO;
using UnityEditor;
using UnityEngine;
using Bones.Core;
using Bones.Data;

namespace Bones.Editor
{
    /// <summary>
    /// One-click generator for the MVP content set so you never hand-author ScriptableObjects.
    /// Menu: BONES ▸ Generate MVP Data. Creates dice, an item, Nights 1–3, juice tiers, the
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

            // --- Item (favor) ---
            var lookout = MakeItem("lookout", "Lookout", ItemKind.Favor, Durability.LimitedUse,
                "A kid on the corner whistles when the heat's near.", "suspicion_reduce", 0.02f, 2, 10);

            // --- Juice tiers ---
            var juice = MakeJuice();

            // --- Nights 1–3 (Squeeze: tie drifts banker -> push -> mark; mark starts loading) ---
            var n1 = MakeNight("Night_1", 1, 20, TieRule.Banker, 0.00f);
            var n2 = MakeNight("Night_2", 2, 55, TieRule.Push, 0.00f);
            var n3 = MakeNight("Night_3", 3, 140, TieRule.Mark, 0.12f);

            var campaign = ScriptableObject.CreateInstance<CampaignConfig>();
            campaign.nights = new[] { n1, n2, n3 };
            CreateAsset(campaign, "Assets/Data/Campaign.asset");

            // --- The wired database ---
            var db = ScriptableObject.CreateInstance<GameDatabase>();
            db.boneDie = bone;
            db.allDice.AddRange(new[] { snakeKiller, luckySix, gildedDie });
            db.allItems.Add(lookout);
            db.campaign = campaign;
            db.juice = juice;
            db.startingUnlocks = new System.Collections.Generic.List<string> { "snake_killer", "lucky_six", "gilded_die" };
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
                "• 4 dice (bone, snake_killer, lucky_six, gilded_die)\n" +
                "• Lookout favor\n" +
                "• Nights 1–3 + Campaign\n" +
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

        private static NightConfig MakeNight(string asset, int number, int demand, TieRule tie, float loading)
        {
            var n = ScriptableObject.CreateInstance<NightConfig>();
            n.nightNumber = number; n.collectionDemand = demand; n.tieRule = tie;
            n.opponentLoading = loading; n.gamesThisNight = 3;
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
