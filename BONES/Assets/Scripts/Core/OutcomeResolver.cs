using System;
using System.Collections.Generic;

namespace Bones.Core
{
    /// <summary>The cheat/charm behaviour a die contributes. Face-biasing effects are applied
    /// outcome-first by the resolver; PayoutCharm/HeatCharm are handled by the economy layer.</summary>
    public enum DieEffect
    {
        None = 0,
        BiasHigh = 1,        // this die leans 4–6
        BiasSix = 2,         // this die leans toward 6 (e.g. Lucky Six)
        AvoidOne = 3,        // this die avoids landing on 1
        KillInstantLoss = 4, // whole-hand: turn a 1-2-3 into something better (e.g. Snake Killer)
        ForceFourFiveSix = 5,// whole-hand: manufacture a 4-5-6 (rare)
        ForceTriple = 6,     // whole-hand: manufacture a triple (rarest)
        PayoutCharm = 7,     // no face effect; boosts winnings (e.g. Gilded Die)
        HeatCharm = 8,       // no face effect; amplifies Heat
    }

    /// <summary>A single die in the cup, with its effect resolved to the current level's proc%.</summary>
    public readonly struct DieSpec
    {
        public readonly string Id;
        public readonly DieEffect Effect;
        public readonly double ProcChance;  // 0..1, already resolved for the die's level
        public readonly double Suspicion;    // bust-% this die adds to the game

        public DieSpec(string id, DieEffect effect, double procChance, double suspicion)
        {
            Id = id;
            Effect = effect;
            ProcChance = procChance;
            Suspicion = suspicion;
        }

        public static DieSpec Bone(string id = "bone") => new DieSpec(id, DieEffect.None, 0, 0);
        public bool BiasesFaces => Effect is DieEffect.BiasHigh or DieEffect.BiasSix or DieEffect.AvoidOne;
        public bool IsWholeHand => Effect is DieEffect.KillInstantLoss or DieEffect.ForceFourFiveSix or DieEffect.ForceTriple;
    }

    /// <summary>The decided result of one throw: the exact faces the choreographer must land on,
    /// the categorized result, and which dice's cheats visibly fired (drives the tell + Suspicion).</summary>
    public readonly struct ResolvedThrow
    {
        public readonly int Face0, Face1, Face2;
        public readonly CeeloResult Result;
        public readonly IReadOnlyList<string> FiredProcs;

        public ResolvedThrow(int f0, int f1, int f2, CeeloResult result, IReadOnlyList<string> fired)
        {
            Face0 = f0; Face1 = f1; Face2 = f2;
            Result = result;
            FiredProcs = fired;
        }
    }

    /// <summary>
    /// Outcome-first dice (manifest §1.4): the logic rolls and decides each face after applying
    /// procs, cheats, and The Squeeze. The roll animation is choreography to this known result.
    /// </summary>
    public static class OutcomeResolver
    {
        private const int MaxRerolls = 32; // re-roll "nothing" until decisive; safety cap

        /// <summary>
        /// Resolve the banker's throw with the player's 3-die cup. Re-rolls non-decisive throws,
        /// applying each die's effect (gated by its proc chance). Records which cheats fired.
        /// </summary>
        public static ResolvedThrow ResolveBanker(IRng rng, DieSpec d0, DieSpec d1, DieSpec d2)
        {
            var fired = new List<string>(3);
            for (int attempt = 0; attempt < MaxRerolls; attempt++)
            {
                fired.Clear();
                int f0 = RollDie(rng, d0, fired);
                int f1 = RollDie(rng, d1, fired);
                int f2 = RollDie(rng, d2, fired);
                (f0, f1, f2) = ApplyWholeHand(rng, f0, f1, f2, d0, d1, d2, fired);

                var result = CeeloEngine.Evaluate(f0, f1, f2);
                if (result.IsDecisive)
                    return new ResolvedThrow(f0, f1, f2, result, fired);
            }
            // Degenerate fallback: a guaranteed decisive point.
            var fallback = CeeloEngine.Evaluate(6, 6, 2);
            return new ResolvedThrow(6, 6, 2, fallback, fired);
        }

        /// <summary>
        /// Resolve the mark's throw. loadingLevel (0..1) is The Squeeze + opponent loading: the
        /// higher it is, the more the mark's dice lean high (better points, more instant wins).
        /// </summary>
        public static ResolvedThrow ResolveMark(IRng rng, double loadingLevel)
        {
            var none = Array.Empty<string>();
            bool loaded = loadingLevel > 0 && rng.Chance(loadingLevel);
            for (int attempt = 0; attempt < MaxRerolls; attempt++)
            {
                int f0 = loaded ? BiasedHigh(rng) : rng.D6();
                int f1 = loaded ? BiasedHigh(rng) : rng.D6();
                int f2 = loaded ? BiasedHigh(rng) : rng.D6();
                var result = CeeloEngine.Evaluate(f0, f1, f2);
                if (result.IsDecisive)
                    return new ResolvedThrow(f0, f1, f2, result, none);
            }
            var fallback = CeeloEngine.Evaluate(3, 3, 1);
            return new ResolvedThrow(3, 3, 1, fallback, none);
        }

        private static int RollDie(IRng rng, DieSpec die, List<string> fired)
        {
            if (die.BiasesFaces && rng.Chance(die.ProcChance))
            {
                fired.Add(die.Id);
                return die.Effect switch
                {
                    DieEffect.BiasSix => 6,
                    DieEffect.BiasHigh => rng.Range(4, 7),
                    DieEffect.AvoidOne => rng.Range(2, 7),
                    _ => rng.D6(),
                };
            }
            return rng.D6();
        }

        private static (int, int, int) ApplyWholeHand(IRng rng, int f0, int f1, int f2,
            DieSpec d0, DieSpec d1, DieSpec d2, List<string> fired)
        {
            // Highest-impact effect wins if multiple proc: ForceTriple > Force456 > KillInstantLoss.
            if (TryProc(rng, DieEffect.ForceTriple, d0, d1, d2, fired, out _))
                return (6, 6, 6);
            if (TryProc(rng, DieEffect.ForceFourFiveSix, d0, d1, d2, fired, out _))
                return (4, 5, 6);
            if (TryProc(rng, DieEffect.KillInstantLoss, d0, d1, d2, fired, out _))
            {
                if (CeeloEngine.Evaluate(f0, f1, f2).Kind == CeeloKind.InstantLoss)
                    return BumpLowest(rng, f0, f1, f2); // 1-2-3 -> nudge the low die into a point
            }
            return (f0, f1, f2);
        }

        private static bool TryProc(IRng rng, DieEffect effect, DieSpec d0, DieSpec d1, DieSpec d2,
            List<string> fired, out string id)
        {
            if (TryProcOne(rng, effect, d0, fired, out id)) return true;
            if (TryProcOne(rng, effect, d1, fired, out id)) return true;
            if (TryProcOne(rng, effect, d2, fired, out id)) return true;
            id = null;
            return false;
        }

        private static bool TryProcOne(IRng rng, DieEffect effect, DieSpec d, List<string> fired, out string id)
        {
            if (d.Effect == effect && rng.Chance(d.ProcChance))
            {
                fired.Add(d.Id);
                id = d.Id;
                return true;
            }
            id = null;
            return false;
        }

        private static (int, int, int) BumpLowest(IRng rng, int f0, int f1, int f2)
        {
            // 1-2-3 sorted; raise the '1' to match another die, making a pair (a point).
            int target = rng.Range(2, 7);
            if (f0 == 1) return (target == 1 ? 2 : target, f1, f2);
            if (f1 == 1) return (f0, target == 1 ? 2 : target, f2);
            return (f0, f1, target == 1 ? 2 : target);
        }

        /// <summary>A d6 leaning high: ~half the time a guaranteed 4–6, else a fair roll.</summary>
        private static int BiasedHigh(IRng rng) => rng.Chance(0.5) ? rng.Range(4, 7) : rng.D6();
    }
}
