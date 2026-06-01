using UnityEngine;
using Bones.Core;

namespace Bones.Data
{
    /// <summary>
    /// Designer-authored definition of a die. One mesh + many material sets = the whole catalog
    /// (manifest §1.2): a die is data, not code. Bridges to the pure-C# <see cref="DieSpec"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "BONES/Die Definition", fileName = "Die_")]
    public class DieDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id = "bone";
        public string displayName = "Bone Die";
        [TextArea] public string flavor = "A plain street die. Honest. Mostly.";
        public DieTier tier = DieTier.Common;

        [Header("Behaviour")]
        public DieEffect effect = DieEffect.None;
        [Tooltip("Proc chance per level (Lv1..Lv3). Index 0 = level 1.")]
        [Range(0f, 1f)] public float[] procChanceByLevel = { 0f };
        [Tooltip("Bust % this die adds to a game's Suspicion.")]
        [Range(0f, 1f)] public float suspicion = 0f;

        [Header("Economy")]
        public int basePrice = 0;
        public int maxLevel = 1;

        [Header("Presentation (swappable placeholders)")]
        [Tooltip("Optional authored mesh; null falls back to a rounded-cube primitive per tier.")]
        public Mesh meshOverride;
        [Tooltip("Optional authored body material; null falls back to the tier's toon material.")]
        public Material materialOverride;
        public Sprite cardIcon;

        /// <summary>Proc chance for a given level (1-based), clamped to the authored array.</summary>
        public float ProcChanceAtLevel(int level)
        {
            if (procChanceByLevel == null || procChanceByLevel.Length == 0) return 0f;
            int idx = Mathf.Clamp(level - 1, 0, procChanceByLevel.Length - 1);
            return procChanceByLevel[idx];
        }

        /// <summary>Fence price for a die at a given night (scales with the run — ECONOMY.md).</summary>
        public int PriceAtNight(int night) => Mathf.RoundToInt(basePrice * (1f + 0.5f * (night - 1)));

        public bool IsCheat => effect is DieEffect.BiasHigh or DieEffect.BiasSix or DieEffect.AvoidOne
            or DieEffect.KillInstantLoss or DieEffect.ForceFourFiveSix or DieEffect.ForceTriple;

        /// <summary>Convert to the pure-logic spec the resolver consumes, at the owned level.</summary>
        public DieSpec ToSpec(int level) => new DieSpec(id, effect, ProcChanceAtLevel(level), suspicion);
    }
}
