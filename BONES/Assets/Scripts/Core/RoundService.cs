using System;
using System.Collections.Generic;

namespace Bones.Core
{
    /// <summary>The full record of one Cee-lo round: every throw both sides made (natural "nothing"
    /// re-rolls included, so the presentation can show them), the decisive throws, and the settled
    /// outcome. The mark's throws are empty when the banker resolved outright.</summary>
    public readonly struct RoundReport
    {
        /// <summary>The banker's decisive (final) throw.</summary>
        public readonly ResolvedThrow Banker;
        /// <summary>The banker's full throw sequence; the last element is decisive.</summary>
        public readonly IReadOnlyList<ResolvedThrow> BankerThrows;
        public readonly bool MarkRolled;
        /// <summary>The mark's decisive (final) throw. Only valid when MarkRolled.</summary>
        public readonly ResolvedThrow Mark;
        /// <summary>The mark's full throw sequence; empty when the banker resolved outright.</summary>
        public readonly IReadOnlyList<ResolvedThrow> MarkThrows;
        public readonly GameOutcome Outcome;

        public RoundReport(IReadOnlyList<ResolvedThrow> bankerThrows, bool markRolled,
            IReadOnlyList<ResolvedThrow> markThrows, GameOutcome outcome)
        {
            BankerThrows = bankerThrows;
            Banker = bankerThrows[bankerThrows.Count - 1];
            MarkRolled = markRolled;
            MarkThrows = markThrows;
            Mark = markRolled ? markThrows[markThrows.Count - 1] : default;
            Outcome = outcome;
        }
    }

    /// <summary>
    /// Plays one complete round (outcome-first): banker rolls; if a point is set, the mark rolls;
    /// the round is resolved under the night's tie rule. Pure and deterministic given the RNG.
    /// </summary>
    public static class RoundService
    {
        public static RoundReport PlayRound(IRng rng, DieSpec d0, DieSpec d1, DieSpec d2,
            double markLoading, TieRule tie)
        {
            var bankerThrows = OutcomeResolver.ResolveBankerThrows(rng, d0, d1, d2);
            var banker = bankerThrows[bankerThrows.Count - 1];

            // The mark only rolls if the banker set a point.
            if (banker.Result.Kind != CeeloKind.Point)
            {
                var outright = CeeloEngine.ResolveRound(banker.Result, default, tie);
                return new RoundReport(bankerThrows, markRolled: false, Array.Empty<ResolvedThrow>(), outright);
            }

            var markThrows = OutcomeResolver.ResolveMarkThrows(rng, markLoading);
            var mark = markThrows[markThrows.Count - 1];
            var outcome = CeeloEngine.ResolveRound(banker.Result, mark.Result, tie);
            return new RoundReport(bankerThrows, markRolled: true, markThrows, outcome);
        }
    }
}
