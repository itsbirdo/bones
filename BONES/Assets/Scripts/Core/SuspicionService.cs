using System;

namespace Bones.Core
{
    /// <summary>
    /// Suspicion = your standing bust % for the game (sum of each equipped cheat die's bust-%,
    /// minus active favors). Rolled once when a game settles on a win. Target rate for a normal
    /// cheating build: ~1 bust per 30–40 games (≈2.5–3.3% per game). Spec §9.2.
    /// </summary>
    public static class SuspicionService
    {
        /// <summary>Standing bust chance this game, clamped to [0,1]. Lay Low forces 0.</summary>
        public static double BustChance(double summedDieSuspicion, double favorReduction, bool layingLow)
        {
            if (layingLow) return 0.0;
            double chance = summedDieSuspicion - favorReduction;
            if (chance < 0.0) return 0.0;
            if (chance > 1.0) return 1.0;
            return chance;
        }

        /// <summary>
        /// Roll for a bust. Only meaningful on a winning settle (you only get caught taking the pot).
        /// On a bust you forfeit the staked pot and Heat resets — you keep playing the night.
        /// </summary>
        public static bool RollBust(IRng rng, double bustChance) => rng.Chance(bustChance);
    }
}
