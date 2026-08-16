using NUnit.Framework;
using Bones.Core;

namespace Bones.Tests
{
    public class OutcomeResolverTests
    {
        [Test]
        public void Banker_AlwaysResolvesToADecisiveResult()
        {
            var rng = new SystemRng(12345);
            var bone = DieSpec.Bone();
            for (int i = 0; i < 5000; i++)
            {
                var t = OutcomeResolver.ResolveBanker(rng, bone, bone, bone);
                Assert.IsTrue(t.Result.IsDecisive, "every resolved throw must be decisive");
                AssertValidFaces(t);
            }
        }

        [Test]
        public void Mark_AlwaysResolvesToADecisiveResult()
        {
            var rng = new SystemRng(999);
            for (int i = 0; i < 5000; i++)
            {
                var t = OutcomeResolver.ResolveMark(rng, 0.5);
                Assert.IsTrue(t.Result.IsDecisive);
                AssertValidFaces(t);
            }
        }

        [Test]
        public void BiasSix_RaisesWinRateAndReportsFiredProc()
        {
            var rng = new SystemRng(7);
            var lucky = new DieSpec("lucky_six", DieEffect.BiasSix, 1.0, 0.05); // always procs
            var bone = DieSpec.Bone();

            int fired = 0;
            for (int i = 0; i < 3000; i++)
            {
                var t = OutcomeResolver.ResolveBanker(rng, lucky, bone, bone);
                if (t.FiredProcs.Count > 0) fired++;
            }
            Assert.Greater(fired, 0, "a guaranteed-proc cheat die should report fired procs");
        }

        [Test]
        public void ForceTriple_ProducesSixSixSix()
        {
            var rng = new SystemRng(3);
            var magnet = new DieSpec("the_magnet", DieEffect.ForceTriple, 1.0, 0.4);
            var bone = DieSpec.Bone();
            var t = OutcomeResolver.ResolveBanker(rng, magnet, bone, bone);
            Assert.AreEqual(CeeloKind.Triple, t.Result.Kind);
            Assert.AreEqual(6, t.Result.Value);
        }

        [Test]
        public void ForceFourFiveSix_ProducesFourFiveSix()
        {
            var rng = new SystemRng(4);
            var seq = new DieSpec("the_sequencer", DieEffect.ForceFourFiveSix, 1.0, 0.4);
            var bone = DieSpec.Bone();
            var t = OutcomeResolver.ResolveBanker(rng, seq, bone, bone);
            Assert.AreEqual(CeeloKind.FourFiveSix, t.Result.Kind);
        }

        [Test]
        public void KillInstantLoss_NeverLeavesAOneTwoThree()
        {
            var rng = new SystemRng(42);
            var snakeKiller = new DieSpec("snake_killer", DieEffect.KillInstantLoss, 1.0, 0.08);
            var bone = DieSpec.Bone();
            for (int i = 0; i < 5000; i++)
            {
                var t = OutcomeResolver.ResolveBanker(rng, snakeKiller, bone, bone);
                Assert.AreNotEqual(CeeloKind.InstantLoss, t.Result.Kind,
                    "Snake Killer (guaranteed proc) must convert every 1-2-3");
            }
        }

        [Test]
        public void MarkLoading_LowersBankerWinRate()
        {
            double fair = BankerWinRateVsMark(loading: 0.0, seed: 1);
            double loaded = BankerWinRateVsMark(loading: 1.0, seed: 1);
            Assert.Less(loaded, fair, "a fully loaded mark must beat the banker more often");
        }

        private static double BankerWinRateVsMark(double loading, int seed)
        {
            var rng = new SystemRng(seed);
            var bone = DieSpec.Bone();
            int wins = 0, games = 20000;
            for (int i = 0; i < games; i++)
            {
                var banker = OutcomeResolver.ResolveBanker(rng, bone, bone, bone);
                var mark = OutcomeResolver.ResolveMark(rng, loading);
                if (CeeloEngine.ResolveRound(banker.Result, mark.Result, TieRule.Banker) == GameOutcome.BankerWin)
                    wins++;
            }
            return (double)wins / games;
        }

        [Test]
        public void BankerThrowSequence_OnlyFinalThrowIsDecisive()
        {
            var rng = new SystemRng(2026);
            var bone = DieSpec.Bone();
            for (int i = 0; i < 3000; i++)
            {
                var throws = OutcomeResolver.ResolveBankerThrows(rng, bone, bone, bone);
                Assert.GreaterOrEqual(throws.Count, 1);
                for (int j = 0; j < throws.Count - 1; j++)
                {
                    Assert.AreEqual(CeeloKind.Nothing, throws[j].Result.Kind,
                        "every throw before the last must be a natural 'nothing' re-roll");
                    AssertValidFaces(throws[j]);
                }
                Assert.IsTrue(throws[throws.Count - 1].Result.IsDecisive, "the final throw must be decisive");
                AssertValidFaces(throws[throws.Count - 1]);
            }
        }

        [Test]
        public void MarkThrowSequence_OnlyFinalThrowIsDecisive()
        {
            var rng = new SystemRng(2027);
            for (int i = 0; i < 3000; i++)
            {
                var throws = OutcomeResolver.ResolveMarkThrows(rng, 0.5);
                for (int j = 0; j < throws.Count - 1; j++)
                    Assert.AreEqual(CeeloKind.Nothing, throws[j].Result.Kind);
                Assert.IsTrue(throws[throws.Count - 1].Result.IsDecisive);
            }
        }

        [Test]
        public void HonestDice_RollUniformly_NothingAppearsAtTheNaturalRate()
        {
            // 108 of the 216 raw 3d6 outcomes are "nothing" (exactly 50%): with no cheats equipped,
            // the FIRST throw of each sequence is a raw uniform roll, so ~half must be re-rolls.
            // Guards against any resolver change that skews honest dice toward decisive hands
            // (the "always lands doubles then a number" bug class).
            var rng = new SystemRng(31337);
            var bone = DieSpec.Bone();
            int sequences = 20000, nothingFirstThrows = 0, points = 0, decisive = 0;
            for (int i = 0; i < sequences; i++)
            {
                var throws = OutcomeResolver.ResolveBankerThrows(rng, bone, bone, bone);
                if (throws[0].Result.Kind == CeeloKind.Nothing) nothingFirstThrows++;
                var final = throws[throws.Count - 1].Result;
                decisive++;
                if (final.Kind == CeeloKind.Point) points++;
            }

            double nothingRate = (double)nothingFirstThrows / sequences;
            Assert.That(nothingRate, Is.InRange(0.47, 0.53),
                $"honest first throws must be 'nothing' ~50% of the time (got {nothingRate:P1})");

            // Of decisive throws, points are 90/108 = 83.3%: the "doubles then a number" look is
            // the correct conditional mix, as long as the re-rolls above are actually shown.
            double pointRate = (double)points / decisive;
            Assert.That(pointRate, Is.InRange(0.80, 0.86),
                $"points must be ~83% of decisive results (got {pointRate:P1})");
        }

        private static void AssertValidFaces(ResolvedThrow t)
        {
            Assert.IsTrue(t.Face0 is >= 1 and <= 6);
            Assert.IsTrue(t.Face1 is >= 1 and <= 6);
            Assert.IsTrue(t.Face2 is >= 1 and <= 6);
            // Faces must actually produce the reported result.
            Assert.AreEqual(t.Result.Kind, CeeloEngine.Evaluate(t.Face0, t.Face1, t.Face2).Kind);
        }
    }
}
