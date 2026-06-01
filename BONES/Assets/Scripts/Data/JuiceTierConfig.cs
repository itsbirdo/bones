using UnityEngine;

namespace Bones.Data
{
    /// <summary>The escalation tiers of the dopamine machine (manifest §3.1). Tiers must feel
    /// categorically different, not just "more". Designer-tunable per tier.</summary>
    public enum JuiceTier
    {
        Clack = 0,    // every die landing
        PointWin = 1, // ordinary win
        Hot = 2,      // 4-5-6 or mid-Heat
        Jackpot = 3,  // triple / high-Heat
        Bust = 4,     // failed Suspicion check — the anti-jackpot
    }

    [CreateAssetMenu(menuName = "BONES/Juice Tier Config", fileName = "JuiceTiers")]
    public class JuiceTierConfig : ScriptableObject
    {
        [System.Serializable]
        public struct TierParams
        {
            public JuiceTier tier;
            [Range(0f, 1f)] public float screenShake;
            public int particleBurst;
            [Range(0f, 2f)] public float timeScaleDip; // slow-mo on the lingering final die
            public Color flashColor;
            [Range(0f, 2f)] public float musicSwell;
            public bool panelFracture;
        }

        public TierParams[] tiers;

        public TierParams For(JuiceTier tier)
        {
            if (tiers != null)
                foreach (var t in tiers)
                    if (t.tier == tier) return t;
            return new TierParams { tier = tier, screenShake = 0.1f, particleBurst = 4, flashColor = Color.white };
        }
    }
}
