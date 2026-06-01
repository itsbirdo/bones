using System.Collections.Generic;
using NUnit.Framework;
using Bones.Core;

namespace Bones.Tests
{
    public class CharmServiceTests
    {
        private static CeeloResult Point() => CeeloEngine.Evaluate(3, 3, 5);
        private static CeeloResult FourFiveSix() => CeeloEngine.Evaluate(4, 5, 6);
        private static CeeloResult Triple() => CeeloEngine.Evaluate(6, 6, 6);

        [Test]
        public void NoCharms_LeavesHeatAndPayoutUntouched()
        {
            var r = CharmService.Apply(1.5, Point(), new List<DieEffect>());
            Assert.AreEqual(1.5, r.Heat, 1e-9);
            Assert.AreEqual(1.0, r.PayoutMultiplier, 1e-9);
        }

        [Test]
        public void NullCharms_IsSafe()
        {
            var r = CharmService.Apply(1.0, Point(), null);
            Assert.AreEqual(1.0, r.Heat, 1e-9);
            Assert.AreEqual(1.0, r.PayoutMultiplier, 1e-9);
        }

        [Test]
        public void PayoutCharm_AddsToPayoutOnAnyWin()
        {
            var onPoint = CharmService.Apply(1.0, Point(), new[] { DieEffect.PayoutCharm });
            Assert.AreEqual(1.5, onPoint.PayoutMultiplier, 1e-9);
            Assert.AreEqual(1.0, onPoint.Heat, 1e-9);

            var onJackpot = CharmService.Apply(1.0, FourFiveSix(), new[] { DieEffect.PayoutCharm });
            Assert.AreEqual(1.5, onJackpot.PayoutMultiplier, 1e-9);
        }

        [Test]
        public void HeatCharm_RaisesHeatNotPayout()
        {
            var r = CharmService.Apply(1.5, Point(), new[] { DieEffect.HeatCharm });
            Assert.AreEqual(2.0, r.Heat, 1e-9);
            Assert.AreEqual(1.0, r.PayoutMultiplier, 1e-9);
        }

        [Test]
        public void JackpotCharm_BoostsOnFourFiveSixAndTriple()
        {
            var on456 = CharmService.Apply(1.0, FourFiveSix(), new[] { DieEffect.JackpotCharm });
            Assert.AreEqual(2.0, on456.PayoutMultiplier, 1e-9);

            var onTriple = CharmService.Apply(1.0, Triple(), new[] { DieEffect.JackpotCharm });
            Assert.AreEqual(2.0, onTriple.PayoutMultiplier, 1e-9);
        }

        [Test]
        public void JackpotCharm_DoesNothingOnAPointWin()
        {
            var r = CharmService.Apply(1.0, Point(), new[] { DieEffect.JackpotCharm });
            Assert.AreEqual(1.0, r.PayoutMultiplier, 1e-9);
            Assert.AreEqual(1.0, r.Heat, 1e-9);
        }

        [Test]
        public void Charms_StackAdditively()
        {
            // Two payout charms + two heat charms on a jackpot, plus a jackpot charm.
            var charms = new[]
            {
                DieEffect.PayoutCharm, DieEffect.PayoutCharm,
                DieEffect.HeatCharm, DieEffect.HeatCharm,
                DieEffect.JackpotCharm,
            };
            var r = CharmService.Apply(1.0, Triple(), charms);
            // payout: 1.0 + 0.5 + 0.5 (payout) + 1.0 (jackpot) = 3.0
            Assert.AreEqual(3.0, r.PayoutMultiplier, 1e-9);
            // heat: 1.0 + 0.5 + 0.5 = 2.0
            Assert.AreEqual(2.0, r.Heat, 1e-9);
        }

        [Test]
        public void StackedJackpotCharms_OnlyCountOnJackpot()
        {
            var charms = new[] { DieEffect.JackpotCharm, DieEffect.JackpotCharm };
            Assert.AreEqual(3.0, CharmService.Apply(1.0, FourFiveSix(), charms).PayoutMultiplier, 1e-9);
            Assert.AreEqual(1.0, CharmService.Apply(1.0, Point(), charms).PayoutMultiplier, 1e-9);
        }
    }
}
