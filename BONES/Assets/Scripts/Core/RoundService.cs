namespace Bones.Core
{
    /// <summary>The full record of one Cee-lo round: both throws (the mark's is only valid when the
    /// banker set a point) and the settled outcome. Feeds both the choreographer and the economy.</summary>
    public readonly struct RoundReport
    {
        public readonly ResolvedThrow Banker;
        public readonly bool MarkRolled;
        public readonly ResolvedThrow Mark;
        public readonly GameOutcome Outcome;

        public RoundReport(ResolvedThrow banker, bool markRolled, ResolvedThrow mark, GameOutcome outcome)
        {
            Banker = banker;
            MarkRolled = markRolled;
            Mark = mark;
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
            var banker = OutcomeResolver.ResolveBanker(rng, d0, d1, d2);

            // The mark only rolls if the banker set a point.
            if (banker.Result.Kind != CeeloKind.Point)
            {
                var outright = CeeloEngine.ResolveRound(banker.Result, default, tie);
                return new RoundReport(banker, markRolled: false, default, outright);
            }

            var mark = OutcomeResolver.ResolveMark(rng, markLoading);
            var outcome = CeeloEngine.ResolveRound(banker.Result, mark.Result, tie);
            return new RoundReport(banker, markRolled: true, mark, outcome);
        }
    }
}
