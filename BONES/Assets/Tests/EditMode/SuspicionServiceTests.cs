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
        public void Suspicion_AccruesAcrossCrookedGames_AndCoolsOnCleanOnes()
        {
            // A ~3%-loadout build across a 3-game crooked night: 1.5% -> 3.0% -> 4.5% (avg ~3%/game,
            // the spec's 1-in-30-40 calibration), with the night's last win the riskiest.
            double s = 0.0;
            s = SuspicionService.Accrue(s, 0.03);
            Assert.AreEqual(0.015, s, 1e-9);
            s = SuspicionService.Accrue(s, 0.03);
            Assert.AreEqual(0.030, s, 1e-9);
            s = SuspicionService.Accrue(s, 0.03);
            Assert.AreEqual(0.045, s, 1e-9);

            // A clean game (Lay Low / honest cup) halves the heat on you.
            Assert.AreEqual(0.0225, SuspicionService.DecayOnClean(s), 1e-9);

            // Clamps: accrual never exceeds 1, never dips below 0.
            Assert.AreEqual(1.0, SuspicionService.Accrue(0.9, 0.5), 1e-9);
            Assert.AreEqual(0.0, SuspicionService.Accrue(0.0, -0.5), 1e-9);
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
