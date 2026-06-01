using NUnit.Framework;
using Bones.Core;

namespace Bones.Tests
{
    public class ReckoningServiceTests
    {
        [Test]
        public void MatchResolved_WhenEitherSideClinchesTwo()
        {
            Assert.IsFalse(ReckoningService.MatchResolved(0, 0));
            Assert.IsFalse(ReckoningService.MatchResolved(1, 0));
            Assert.IsFalse(ReckoningService.MatchResolved(1, 1));
            Assert.IsTrue(ReckoningService.MatchResolved(2, 0));  // player clinches early
            Assert.IsTrue(ReckoningService.MatchResolved(0, 2));  // Vito clinches early
            Assert.IsTrue(ReckoningService.MatchResolved(2, 1));
        }

        [Test]
        public void MatchResolved_AfterAllThreeGames()
        {
            // Edge guard: three games played always resolves even on odd tallies.
            Assert.IsTrue(ReckoningService.MatchResolved(2, 1));
            Assert.IsTrue(ReckoningService.MatchResolved(1, 2));
        }

        [Test]
        public void PlayerWonMatch_NeedsMajority()
        {
            Assert.IsTrue(ReckoningService.PlayerWonMatch(2, 0));
            Assert.IsTrue(ReckoningService.PlayerWonMatch(2, 1));
            Assert.IsFalse(ReckoningService.PlayerWonMatch(1, 2));
            Assert.IsFalse(ReckoningService.PlayerWonMatch(0, 2));
        }

        [Test]
        public void IsDecidingGame_OnlyAtOneOne()
        {
            Assert.IsTrue(ReckoningService.IsDecidingGame(1, 1));
            Assert.IsFalse(ReckoningService.IsDecidingGame(0, 0));
            Assert.IsFalse(ReckoningService.IsDecidingGame(1, 0));
            Assert.IsFalse(ReckoningService.IsDecidingGame(0, 1));
        }

        [Test]
        public void EffectiveLoading_BumpsOnlyTheDecidingGame()
        {
            // Non-deciding games use the night's base loading unchanged.
            Assert.AreEqual(0.80, ReckoningService.EffectiveLoading(0.80, 0, 0), 1e-9);
            Assert.AreEqual(0.80, ReckoningService.EffectiveLoading(0.80, 1, 0), 1e-9);
            // The 1-1 decider bumps by the configured amount.
            Assert.AreEqual(0.90, ReckoningService.EffectiveLoading(0.80, 1, 1), 1e-9);
        }

        [Test]
        public void EffectiveLoading_ClampsToOne()
        {
            Assert.AreEqual(1.0, ReckoningService.EffectiveLoading(0.95, 1, 1), 1e-9);
            Assert.AreEqual(1.0, ReckoningService.EffectiveLoading(1.0, 1, 1), 1e-9);
        }
    }
}
