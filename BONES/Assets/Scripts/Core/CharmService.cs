using System.Collections.Generic;

namespace Bones.Core
{
    /// <summary>
    /// Pure charm math (engine-free). Charms occupy a cup slot but have no face effect; their
    /// benefit applies at settle time on a win. The caller (GameController) rolls each charm's
    /// proc with its own RNG and passes in the list of ALREADY-PROCCED charm effects, so this
    /// stays deterministic and unit-testable.
    /// </summary>
    public static class CharmService
    {
        /// <summary>The charm-adjusted multipliers for one settled, winning game.</summary>
        public readonly struct CharmResult
        {
            /// <summary>Heat multiplier to use for this game's payout (base Heat plus charm bonuses).</summary>
            public readonly double Heat;
            /// <summary>Payout multiplier applied to the winnings on top of the base settle.</summary>
            public readonly double PayoutMultiplier;

            public CharmResult(double heat, double payoutMultiplier)
            {
                Heat = heat;
                PayoutMultiplier = payoutMultiplier;
            }
        }

        public const double PayoutCharmBonus = 0.5;  // +0.5 to payout multiplier per proc
        public const double HeatCharmBonus = 0.5;     // +0.5 to Heat multiplier per proc
        public const double JackpotCharmBonus = 1.0;  // +1.0 to payout multiplier per proc (jackpots only)

        /// <summary>
        /// Apply procced charms to a winning game's economy. Starts from the base Heat and a payout
        /// multiplier of 1.0, then stacks each procced charm additively. JackpotCharm only contributes
        /// when the banker result is a 4-5-6 or a triple.
        /// </summary>
        public static CharmResult Apply(double baseHeat, CeeloResult bankerResult,
            IEnumerable<DieEffect> proccedCharms)
        {
            double heat = baseHeat;
            double payoutMult = 1.0;
            bool jackpot = bankerResult.Kind is CeeloKind.FourFiveSix or CeeloKind.Triple;

            if (proccedCharms != null)
            {
                foreach (var effect in proccedCharms)
                {
                    switch (effect)
                    {
                        case DieEffect.PayoutCharm:
                            payoutMult += PayoutCharmBonus;
                            break;
                        case DieEffect.HeatCharm:
                            heat += HeatCharmBonus;
                            break;
                        case DieEffect.JackpotCharm:
                            if (jackpot) payoutMult += JackpotCharmBonus;
                            break;
                    }
                }
            }

            return new CharmResult(heat, payoutMult);
        }
    }
}
