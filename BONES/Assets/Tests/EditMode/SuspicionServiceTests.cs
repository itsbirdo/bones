using NUnit.Framework;
using Bones.Core;

namespace Bones.Tests
{
    public class SuspicionServiceTests
    {
        [Test]
        public void BustChance_ClampsToRange()
        {
            Assert.AreEqual(0.0, SuspicionService.BustChance(-0.1, 0, false), 1e-9);
            Assert.AreEqual(0.0, SuspicionService.BustChance(0.02, 0.05, false), 1e-9); // favor exceeds suspicion
            Assert.AreEqual(0.03, SuspicionService.BustChance(0.08, 0.05, false), 1e-9);
            Assert.AreEqual(1.0, SuspicionService.BustChance(1.5, 0, false), 1e-9);
        }

        [Test]
        public void LayingLow_ForcesZeroBustChance()
        {
            Assert.AreEqual(0.0, SuspicionService.BustChance(0.5, 0, true), 1e-9);
        }

        [Test]
        public void NormalCheatingBuild_BustsRoughlyOncePer30To40Games()
        {
            // ~3% per game should land in the spec's 1-in-30-to-40 band over many games.
            var rng = new SystemRng(2024);
            double chance = 0.03;
            int busts = 0, games = 200000;
            for (int i = 0; i < games; i++)
                if (SuspicionService.RollBust(rng, chance)) busts++;

            double gamesPerBust = (double)games / busts;
            Assert.That(gamesPerBust, Is.InRange(28.0, 42.0),
                $"expected ~1 bust / 30-40 games, got 1 / {gamesPerBust:F1}");
        }
    }
}
