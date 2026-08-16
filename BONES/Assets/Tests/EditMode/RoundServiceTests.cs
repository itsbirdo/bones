using NUnit.Framework;
using Bones.Core;

namespace Bones.Tests
{
    public class RoundServiceTests
    {
        [Test]
        public void MarkOnlyRollsWhenBankerSetsAPoint()
        {
            var rng = new SystemRng(123);
            var bone = DieSpec.Bone();
            int markRolledOnPoint = 0, markRolledOnNonPoint = 0;

            for (int i = 0; i < 4000; i++)
            {
                var report = RoundService.PlayRound(rng, bone, bone, bone, 0.0, TieRule.Banker);
                bool bankerPoint = report.Banker.Result.Kind == CeeloKind.Point;
                if (bankerPoint && report.MarkRolled) markRolledOnPoint++;
                if (!bankerPoint && report.MarkRolled) markRolledOnNonPoint++;
            }

            Assert.Greater(markRolledOnPoint, 0, "the mark should roll when the banker sets a point");
            Assert.AreEqual(0, markRolledOnNonPoint, "the mark must never roll on an instant win/loss");
        }

        [Test]
        public void Report_CarriesFullThrowSequences_FinalThrowMatchesDecisive()
        {
            var rng = new SystemRng(77);
            var bone = DieSpec.Bone();
            for (int i = 0; i < 2000; i++)
            {
                var report = RoundService.PlayRound(rng, bone, bone, bone, 0.3, TieRule.Push);

                var lastBanker = report.BankerThrows[report.BankerThrows.Count - 1];
                Assert.AreEqual(report.Banker.Result.Kind, lastBanker.Result.Kind);
                Assert.AreEqual(report.Banker.Face0, lastBanker.Face0);
                for (int j = 0; j < report.BankerThrows.Count - 1; j++)
                    Assert.AreEqual(CeeloKind.Nothing, report.BankerThrows[j].Result.Kind);

                if (report.MarkRolled)
                {
                    var lastMark = report.MarkThrows[report.MarkThrows.Count - 1];
                    Assert.AreEqual(report.Mark.Result.Kind, lastMark.Result.Kind);
                    for (int j = 0; j < report.MarkThrows.Count - 1; j++)
                        Assert.AreEqual(CeeloKind.Nothing, report.MarkThrows[j].Result.Kind);
                }
                else
                {
                    Assert.AreEqual(0, report.MarkThrows.Count, "no mark throws on an outright banker result");
                }
            }
        }

        [Test]
        public void GuaranteedTriple_AlwaysWinsOutright()
        {
            var rng = new SystemRng(5);
            var magnet = new DieSpec("magnet", DieEffect.ForceTriple, 1.0, 0.4);
            var bone = DieSpec.Bone();
            for (int i = 0; i < 1000; i++)
            {
                var report = RoundService.PlayRound(rng, magnet, bone, bone, 1.0, TieRule.Mark);
                Assert.IsFalse(report.MarkRolled);
                Assert.AreEqual(GameOutcome.BankerWin, report.Outcome);
            }
        }
    }
}
