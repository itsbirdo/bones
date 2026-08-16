using System;

namespace Bones.Core
{
    /// <summary>
    /// Suspicion ACCUMULATES across the night's games (spec §9.2): the same marks watch you all
    /// night. Each crooked game adds a share of the loadout's summed bust-% to the night's accrued
    /// suspicion; a clean game (Lay Low, or no cheats in the cup) lets it cool. The bust is rolled
    /// against the ACCRUED total when a crooked game settles on a win, so the night has a real
    /// push-your-luck arc: cheat early and the third, biggest game is the dangerous one, or lay
    /// low mid-night to cool off before the kill shot. Resets at the start of each night.
    /// Target rate for a normal cheating build stays ~1 bust per 30–40 games (≈3%/game average).
    /// </summary>
    public static class SuspicionService
    {
        /// <summary>
        /// Calibration: a crooked game adds HALF its loadout's summed bust-% to the night's accrual,
        /// so a steady ~3%-loadout build averages ~3%/game across a 3-game night
        /// (1.5% → 3.0% → 4.5%), preserving the spec's ~1-bust-per-30–40-games target while making
        /// the night's last crooked win the riskiest.
        /// </summary>
        public const double AccrualRate = 0.5;

        /// <summary>A clean game halves the heat on you (they saw nothing this hand).</summary>
        public const double CleanGameDecay = 0.5;

        /// <summary>Accrued suspicion after playing a crooked game with this loadout.</summary>
        public static double Accrue(double accrued, double summedDieSuspicion)
            => Clamp01(accrued + AccrualRate * Math.Max(0.0, summedDieSuspicion));

        /// <summary>Accrued suspicion after a clean game (Lay Low, or an honest cup).</summary>
        public static double DecayOnClean(double accrued) => Clamp01(accrued * CleanGameDecay);

        /// <summary>
        /// Bust chance for a winning settle: the night's accrued suspicion minus active favors,
        /// clamped to [0,1]. Lay Low forces 0 (you played this hand straight; nothing to catch).
        /// </summary>
        public static double BustChance(double accruedSuspicion, double favorReduction, bool layingLow)
        {
            if (layingLow) return 0.0;
            return Clamp01(accruedSuspicion - favorReduction);
        }

        /// <summary>
        /// Roll for a bust. Only meaningful on a winning settle of a crooked game (you only get
        /// caught taking the pot). On a bust you forfeit the staked pot and Heat resets — you keep
        /// playing the night.
        /// </summary>
        public static bool RollBust(IRng rng, double bustChance) => rng.Chance(bustChance);

        private static double Clamp01(double v) => v < 0.0 ? 0.0 : (v > 1.0 ? 1.0 : v);
    }
}
