namespace Bones.Core
{
    /// <summary>
    /// Win multipliers by outcome (spec §5.1 tuning placeholders):
    /// point win 1:1 · 4-5-6 2:1 · triple 3:1 · 6-6-6 optionally higher (manifest §1 notes 5:1).
    /// All multiplied by Heat at settle time.
    /// </summary>
    public readonly struct PayoutTable
    {
        public readonly double PointWin;
        public readonly double FourFiveSix;
        public readonly double Triple;
        public readonly double SixSixSix;

        public PayoutTable(double pointWin, double fourFiveSix, double triple, double sixSixSix)
        {
            PointWin = pointWin;
            FourFiveSix = fourFiveSix;
            Triple = triple;
            SixSixSix = sixSixSix;
        }

        /// <summary>Spec defaults: 1:1 / 2:1 / 3:1, with 6-6-6 as the top jackpot at 3:1.</summary>
        public static PayoutTable Default => new PayoutTable(1.0, 2.0, 3.0, 3.0);

        /// <summary>The base multiplier (before Heat) for a winning banker result.</summary>
        public double MultiplierFor(CeeloResult result) => result.Kind switch
        {
            CeeloKind.Triple => result.Value == 6 ? SixSixSix : Triple,
            CeeloKind.FourFiveSix => FourFiveSix,
            CeeloKind.Point => PointWin,
            _ => PointWin, // a win on a contested point pays the point rate
        };
    }
}
