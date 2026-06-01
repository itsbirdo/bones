using NUnit.Framework;
using Bones.Core;

namespace Bones.Tests
{
    public class FavorServiceTests
    {
        [Test]
        public void SuspicionReduction_SumsChargedFavors()
        {
            var favors = new (double magnitude, int charges)[]
            {
                (0.02, 2),
                (0.03, 1),
            };
            Assert.AreEqual(0.05, FavorService.SuspicionReduction(favors), 1e-9);
        }

        [Test]
        public void SuspicionReduction_IgnoresZeroChargeFavors()
        {
            var favors = new (double magnitude, int charges)[]
            {
                (0.02, 0),   // spent: contributes nothing
                (0.03, 2),
            };
            Assert.AreEqual(0.03, FavorService.SuspicionReduction(favors), 1e-9);
        }

        [Test]
        public void SuspicionReduction_EmptyOrNull_IsZero()
        {
            Assert.AreEqual(0.0, FavorService.SuspicionReduction(null), 1e-9);
            Assert.AreEqual(0.0, FavorService.SuspicionReduction(new (double, int)[0]), 1e-9);
        }

        [Test]
        public void SuspicionReduction_NeverNegative()
        {
            var favors = new (double magnitude, int charges)[] { (-0.10, 2) };
            Assert.AreEqual(0.0, FavorService.SuspicionReduction(favors), 1e-9);
        }

        [Test]
        public void FavorReduction_LowersBustChanceViaSuspicionService()
        {
            // One Lookout (0.02, 2 charges) trims a 0.05 standing bust to 0.03.
            var favors = new (double magnitude, int charges)[] { (0.02, 2) };
            double reduction = FavorService.SuspicionReduction(favors);
            Assert.AreEqual(0.03, SuspicionService.BustChance(0.05, reduction, false), 1e-9);
        }

        [Test]
        public void LayingLow_StillForcesZeroBust_EvenWithFavors()
        {
            var favors = new (double magnitude, int charges)[] { (0.02, 2) };
            double reduction = FavorService.SuspicionReduction(favors);
            Assert.AreEqual(0.0, SuspicionService.BustChance(0.5, reduction, true), 1e-9);
        }
    }
}
