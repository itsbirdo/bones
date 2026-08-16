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
        HeatCharm = 8,       // no face effect; amplifies Heat (e.g. Streak Charm)
        JackpotCharm = 9,    // no face effect; boosts winnings on a 4-5-6 or triple (e.g. Headcracker)

        // --- Deferred face/hand effects (catalog §11). NO-OP today: the resolver does not read
        // these yet, so a die carrying one behaves like a plain bone. Wire them up before shipping. ---
        BumpParityEven = 10, // TODO: needs per-die parity-nudge in RollDie (Even Steven: odd face -> +1 even)
        BiasOneOrSix = 11,   // TODO: needs per-die face-restriction in RollDie (Two-Face: only ever 1 or 6)
        BumpLowFaces = 12,   // TODO: needs per-die +2 nudge for low faces in RollDie (High Roller: 1-3 up by +2)
        CopyOtherDie = 13,   // TODO: needs cross-die read in ApplyWholeHand (Matchmaker: copy another die's face)

        // --- Deferred control effects (§11). Need a re-roll / nudge / set-a-die input loop that
        // does not exist yet. NO-OP today. ---
        LockThroughReroll = 14, // TODO: needs re-roll system (The Cooler: lock this die through next re-roll)
        RerollSelf = 15,        // TODO: needs re-roll system (Reroll Bone: re-roll this die once)
        NudgeSelf = 16,         // TODO: needs post-land input (The Nudge: adjust this die +/-1 after it lands)
        RerollHand = 17,        // TODO: needs re-roll system (Mulligan Cup: re-roll the whole hand once)
        ReviveInstantLoss = 18, // TODO: needs re-throw system (Second Wind: 1-2-3 -> a fresh throw)
        SetFace = 19,           // TODO: needs set-a-die input (Set-Bone: set this die to any chosen face)

        // --- Deferred charm effects (§11). Need refund / point-bump economy hooks. NO-OP today. ---
        RefundOnLoss = 20,  // TODO: needs loss-refund hook in EconomyService (Rabbit's Die)
        PointSharp = 21,    // TODO: needs point-value bump in settle (Point Sharp: point pays as one higher)

        // --- Deferred curse effects (§11). Need a downside/penalty system. NO-OP today. ---
        ForceBigStake = 22,    // TODO: needs stake-floor enforcement (Gambler's Curse)
        DoubleSwing = 23,      // TODO: needs double-win/double-loss settle (All-or-Nothing)
        SnakeEyesPact = 24,    // TODO: needs payout-mult + any-1-kills-throw (Snake Eyes Pact)
        BloodyKnuckles = 25,   // TODO: needs no-suspicion-decay + faster Heat (Bloody Knuckles)
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
        /// Resolve the banker's roll as the full throw sequence: zero or more natural "nothing"
        /// throws followed by exactly one decisive throw (the last element). Every throw is a real
        /// uniform 3d6 roll (with each die's effect gated by its proc chance), so the choreographer
        /// can show the re-rolls the spec calls for (§5.1) instead of only ever landing decisive
        /// hands. Records which cheats fired per throw.
        /// </summary>
        public static IReadOnlyList<ResolvedThrow> ResolveBankerThrows(IRng rng, DieSpec d0, DieSpec d1, DieSpec d2)
        {
            var throws = new List<ResolvedThrow>(2);
            for (int attempt = 0; attempt < MaxRerolls; attempt++)
            {
                var fired = new List<string>(3);
                int f0 = RollDie(rng, d0, fired);
                int f1 = RollDie(rng, d1, fired);
                int f2 = RollDie(rng, d2, fired);
                (f0, f1, f2) = ApplyWholeHand(rng, f0, f1, f2, d0, d1, d2, fired);

                var result = CeeloEngine.Evaluate(f0, f1, f2);
                throws.Add(new ResolvedThrow(f0, f1, f2, result, fired));
                if (result.IsDecisive)
                    return throws;
            }
            // Degenerate fallback: a guaranteed decisive point.
            var fallback = CeeloEngine.Evaluate(6, 6, 2);
            throws.Add(new ResolvedThrow(6, 6, 2, fallback, Array.Empty<string>()));
            return throws;
        }

        /// <summary>The banker's final, decisive throw (the last element of the sequence).</summary>
        public static ResolvedThrow ResolveBanker(IRng rng, DieSpec d0, DieSpec d1, DieSpec d2)
        {
            var throws = ResolveBankerThrows(rng, d0, d1, d2);
            return throws[throws.Count - 1];
        }

        /// <summary>
        /// Resolve the mark's roll as the full throw sequence (last element decisive).
        /// loadingLevel (0..1) is The Squeeze + opponent loading: the higher it is, the more the
        /// mark's dice lean high (better points, more instant wins).
        /// </summary>
        public static IReadOnlyList<ResolvedThrow> ResolveMarkThrows(IRng rng, double loadingLevel)
        {
            var none = Array.Empty<string>();
            bool loaded = loadingLevel > 0 && rng.Chance(loadingLevel);
            var throws = new List<ResolvedThrow>(2);
            for (int attempt = 0; attempt < MaxRerolls; attempt++)
            {
                int f0 = loaded ? LoadedFace(rng, loadingLevel) : rng.D6();
                int f1 = loaded ? LoadedFace(rng, loadingLevel) : rng.D6();
                int f2 = loaded ? LoadedFace(rng, loadingLevel) : rng.D6();
                var result = CeeloEngine.Evaluate(f0, f1, f2);
                throws.Add(new ResolvedThrow(f0, f1, f2, result, none));
                if (result.IsDecisive)
                    return throws;
            }
            var fallback = CeeloEngine.Evaluate(3, 3, 1);
            throws.Add(new ResolvedThrow(3, 3, 1, fallback, none));
            return throws;
        }

        /// <summary>The mark's final, decisive throw (the last element of the sequence).</summary>
        public static ResolvedThrow ResolveMark(IRng rng, double loadingLevel)
        {
            var throws = ResolveMarkThrows(rng, loadingLevel);
            return throws[throws.Count - 1];
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

        /// <summary>
        /// One face of a loaded mark's crooked roll. Severity scales with the loading level so The
        /// Squeeze can actually reach the spec's floor (ECONOMY §5: ~15% banker win at Vito):
        /// light loading shaves the die toward 4–6, heavy loading pinches it to 5–6, and
        /// Vito-grade bones come up 6 again and again. (The old 50%-high model capped the banker's
        /// honest win rate at ~34% even at full loading — far too weak for the designed curve.)
        /// </summary>
        private static int LoadedFace(IRng rng, double load)
        {
            double r = rng.NextDouble();
            if (r < load * load) return 6;          // the nastiest bones: a straight 6
            if (r < load) return rng.Range(5, 7);   // heavy shave: 5 or 6
            return rng.Range(4, 7);                  // light shave: 4–6
        }
    }
}
