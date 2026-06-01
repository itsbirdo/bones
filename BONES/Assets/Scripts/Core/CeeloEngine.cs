using System;

namespace Bones.Core
{
    /// <summary>The category of a single Cee-lo throw (3d6). See ceelo.md / spec §5.1.</summary>
    public enum CeeloKind
    {
        /// <summary>No decision — re-roll (e.g. 2-3-4, 1-4-5).</summary>
        Nothing = 0,
        /// <summary>1-2-3 — automatic loss (worst roll).</summary>
        InstantLoss = 1,
        /// <summary>Pair + odd die; the odd die is the point (1..6).</summary>
        Point = 2,
        /// <summary>4-5-6 — automatic win.</summary>
        FourFiveSix = 3,
        /// <summary>Three of a kind (1-1-1..6-6-6); beats 4-5-6, higher triple beats lower.</summary>
        Triple = 4,
    }

    /// <summary>Who wins a settled game from the banker's seat.</summary>
    public enum GameOutcome
    {
        BankerWin,
        BankerLoss,
        Push,
    }

    /// <summary>How same-point ties resolve. The primary lever of The Squeeze (spec §6.4).</summary>
    public enum TieRule
    {
        Banker, // Night 1 — house edge
        Push,   // Night 2 — no money moves
        Mark,   // Nights 3+ — edge inverts
    }

    /// <summary>The evaluated result of one throw, with a total-order rank for comparison.</summary>
    public readonly struct CeeloResult
    {
        public readonly CeeloKind Kind;
        /// <summary>Point value (1..6) when Kind==Point; triple value (1..6) when Kind==Triple; else 0.</summary>
        public readonly int Value;

        public CeeloResult(CeeloKind kind, int value)
        {
            Kind = kind;
            Value = value;
        }

        public bool IsDecisive => Kind != CeeloKind.Nothing;
        public bool IsInstantWin => Kind == CeeloKind.FourFiveSix || Kind == CeeloKind.Triple;

        /// <summary>
        /// Total order over decisive results (higher beats lower):
        /// Triple(v) [201..206] > 4-5-6 [100] > Point(v) [1..6] > 1-2-3 [0].
        /// Nothing is non-decisive and ranks -1.
        /// </summary>
        public int Rank => Kind switch
        {
            CeeloKind.Triple => 200 + Value,
            CeeloKind.FourFiveSix => 100,
            CeeloKind.Point => Value,
            CeeloKind.InstantLoss => 0,
            _ => -1,
        };

        public override string ToString() => Kind switch
        {
            CeeloKind.Triple => $"Triple {Value}-{Value}-{Value}",
            CeeloKind.FourFiveSix => "4-5-6",
            CeeloKind.Point => $"Point {Value}",
            CeeloKind.InstantLoss => "1-2-3",
            _ => "nothing",
        };
    }

    /// <summary>
    /// Pure, deterministic Cee-lo rules. No Unity dependencies — fully unit-testable.
    /// Reproduces the spec's banker win rates: 56.25% (ties→banker), 44.68% (ties→push/mark).
    /// </summary>
    public static class CeeloEngine
    {
        /// <summary>Categorize three dice (each 1..6) into a Cee-lo result.</summary>
        public static CeeloResult Evaluate(int d1, int d2, int d3)
        {
            // Sort ascending without allocation.
            int lo = d1, mid = d2, hi = d3;
            if (lo > mid) (lo, mid) = (mid, lo);
            if (mid > hi) (mid, hi) = (hi, mid);
            if (lo > mid) (lo, mid) = (mid, lo);

            // Triple.
            if (lo == mid && mid == hi)
                return new CeeloResult(CeeloKind.Triple, lo);

            // 4-5-6.
            if (lo == 4 && mid == 5 && hi == 6)
                return new CeeloResult(CeeloKind.FourFiveSix, 0);

            // 1-2-3.
            if (lo == 1 && mid == 2 && hi == 3)
                return new CeeloResult(CeeloKind.InstantLoss, 0);

            // Point: a pair plus an odd die.
            if (lo == mid && mid != hi)
                return new CeeloResult(CeeloKind.Point, hi);   // pair is lo/mid, odd is hi
            if (mid == hi && lo != mid)
                return new CeeloResult(CeeloKind.Point, lo);    // pair is mid/hi, odd is lo

            // Anything else: no decision.
            return new CeeloResult(CeeloKind.Nothing, 0);
        }

        public static CeeloResult Evaluate(ReadOnlySpan<int> dice)
        {
            if (dice.Length != 3) throw new ArgumentException("Cee-lo uses exactly 3 dice.", nameof(dice));
            return Evaluate(dice[0], dice[1], dice[2]);
        }

        /// <summary>
        /// Resolve a full round from the banker's seat. The banker rolls first; the mark only
        /// rolls if the banker set a point. Both inputs must be decisive (caller re-rolls Nothing).
        /// </summary>
        public static GameOutcome ResolveRound(CeeloResult banker, CeeloResult mark, TieRule tie)
        {
            if (!banker.IsDecisive)
                throw new ArgumentException("Banker result must be decisive.", nameof(banker));

            // Banker resolves outright on an instant win or loss; the mark never rolls.
            if (banker.IsInstantWin) return GameOutcome.BankerWin;
            if (banker.Kind == CeeloKind.InstantLoss) return GameOutcome.BankerLoss;

            // Banker set a point — compare against the mark's decisive roll.
            if (!mark.IsDecisive)
                throw new ArgumentException("Mark result must be decisive when the banker set a point.", nameof(mark));

            int cmp = banker.Rank.CompareTo(mark.Rank);
            if (cmp > 0) return GameOutcome.BankerWin;
            if (cmp < 0) return GameOutcome.BankerLoss;

            // Same point — apply the tie rule.
            return tie switch
            {
                TieRule.Banker => GameOutcome.BankerWin,
                TieRule.Mark => GameOutcome.BankerLoss,
                _ => GameOutcome.Push,
            };
        }
    }
}
