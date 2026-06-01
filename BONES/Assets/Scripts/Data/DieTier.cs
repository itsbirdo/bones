using UnityEngine;

namespace Bones.Data
{
    /// <summary>Rarity/role tier. Drives silhouette + material so a die's danger reads at a glance
    /// (manifest §1.1, §3.3).</summary>
    public enum DieTier
    {
        Common,  // standard cube — bone die
        Loaded,  // worn-corner bevel — crooked cheat dice
        Trick,   // faceted/gem-cut — rare control dice
        Curse,   // cracked/sigil — high upside, real downside
        Charm,   // payout/Heat boosters (no face effect)
    }

    public static class DieTierPalette
    {
        /// <summary>Placeholder spot colors until Erin's materials arrive (noir palette).</summary>
        public static Color ColorFor(DieTier tier) => tier switch
        {
            DieTier.Common => new Color(0.92f, 0.90f, 0.82f), // bone white
            DieTier.Loaded => new Color(0.12f, 0.12f, 0.13f), // ink black
            DieTier.Trick  => new Color(0.83f, 0.66f, 0.22f), // gilded amber
            DieTier.Curse  => new Color(0.55f, 0.10f, 0.12f), // blood red
            DieTier.Charm  => new Color(0.83f, 0.66f, 0.22f), // amber
            _ => Color.gray,
        };
    }
}
