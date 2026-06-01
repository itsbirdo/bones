using System.Collections.Generic;
using NUnit.Framework;
using Bones.Core;

namespace Bones.Tests
{
    public class AchievementServiceTests
    {
        private static List<string> Empty() => new List<string>();

        [Test]
        public void FirstWin_UnlocksStreakCharm()
        {
            var sig = new AchievementSignal(AchievementEvent.GamePlayed, wasWin: true, gamesWon: 1);
            var newly = AchievementService.Evaluate(sig, Empty());
            CollectionAssert.Contains(RewardIds(newly), "streak_charm");
        }

        [Test]
        public void Loss_DoesNotUnlockFirstWin()
        {
            var sig = new AchievementSignal(AchievementEvent.GamePlayed, wasWin: false, gamesWon: 0);
            var newly = AchievementService.Evaluate(sig, Empty());
            CollectionAssert.DoesNotContain(RewardIds(newly), "streak_charm");
        }

        [Test]
        public void ThirdWin_UnlocksHotHand()
        {
            var sig = new AchievementSignal(AchievementEvent.GamePlayed, wasWin: true, gamesWon: 3);
            CollectionAssert.Contains(RewardIds(AchievementService.Evaluate(sig, Empty())), "hot_hand");
        }

        [Test]
        public void First456_UnlocksHeadcracker()
        {
            var sig = new AchievementSignal(AchievementEvent.GamePlayed,
                bankerKind: CeeloKind.FourFiveSix, fourFiveSixCount: 1);
            CollectionAssert.Contains(RewardIds(AchievementService.Evaluate(sig, Empty())), "headcracker");
        }

        [Test]
        public void FirstTriple_UnlocksTheMagnet()
        {
            var sig = new AchievementSignal(AchievementEvent.GamePlayed,
                bankerKind: CeeloKind.Triple, bankerValue: 6, tripleCount: 1);
            CollectionAssert.Contains(RewardIds(AchievementService.Evaluate(sig, Empty())), "the_magnet");
        }

        [Test]
        public void SurviveFirstNight_UnlocksGreasedPalm()
        {
            var sig = new AchievementSignal(AchievementEvent.NightSurvived, highestNightReached: 1);
            CollectionAssert.Contains(RewardIds(AchievementService.Evaluate(sig, Empty())), "greased_palm");
        }

        [Test]
        public void ReachNight3_UnlocksShavedEdge()
        {
            var sig = new AchievementSignal(AchievementEvent.NightSurvived, highestNightReached: 3);
            CollectionAssert.Contains(RewardIds(AchievementService.Evaluate(sig, Empty())), "shaved_edge");
        }

        [Test]
        public void ReachNight5_UnlocksTheSequencer()
        {
            var sig = new AchievementSignal(AchievementEvent.NightSurvived, highestNightReached: 5);
            CollectionAssert.Contains(RewardIds(AchievementService.Evaluate(sig, Empty())), "the_sequencer");
        }

        [Test]
        public void FirstBust_UnlocksCoolerHead()
        {
            var sig = new AchievementSignal(AchievementEvent.GamePlayed, wasBust: true, busts: 1);
            CollectionAssert.Contains(RewardIds(AchievementService.Evaluate(sig, Empty())), "cooler_head");
        }

        [Test]
        public void AlreadyUnlocked_NotReturnedAgain()
        {
            var sig = new AchievementSignal(AchievementEvent.GamePlayed, wasWin: true, gamesWon: 1);
            var already = new List<string> { "streak_charm" };
            CollectionAssert.DoesNotContain(RewardIds(AchievementService.Evaluate(sig, already)), "streak_charm");
        }

        [Test]
        public void Evaluate_OnlyRoutesImplementedRewards()
        {
            // No matter the signal, the live path must never return a stub (unimplemented) reward.
            var stubIds = new HashSet<string>();
            foreach (var a in AchievementService.StubTable) stubIds.Add(a.RewardItemId);

            var signals = new[]
            {
                new AchievementSignal(AchievementEvent.RunEnded, deaths: 10, runsWon: 5, highestNightReached: 7),
                new AchievementSignal(AchievementEvent.GamePlayed, wasWin: true, gamesWon: 99),
                new AchievementSignal(AchievementEvent.NightSurvived, highestNightReached: 7),
            };
            foreach (var sig in signals)
                foreach (var id in RewardIds(AchievementService.Evaluate(sig, Empty())))
                    CollectionAssert.DoesNotContain(stubIds, id);
        }

        [Test]
        public void ActiveTable_AllRewardsImplemented()
        {
            foreach (var a in AchievementService.ActiveTable)
                Assert.IsTrue(a.RewardImplemented, $"Active achievement '{a.Id}' must have an implemented reward.");
        }

        [Test]
        public void StubTable_AllRewardsFlaggedUnimplemented()
        {
            foreach (var a in AchievementService.StubTable)
                Assert.IsFalse(a.RewardImplemented, $"Stub achievement '{a.Id}' should be flagged unimplemented.");
        }

        [Test]
        public void UnlockLines_HaveNoEmDashes()
        {
            foreach (var a in AchievementService.ActiveTable)
                StringAssert.DoesNotContain("—", a.UnlockLine);
            foreach (var a in AchievementService.StubTable)
                StringAssert.DoesNotContain("—", a.UnlockLine);
        }

        private static List<string> RewardIds(List<Achievement> achievements)
        {
            var ids = new List<string>();
            foreach (var a in achievements) ids.Add(a.RewardItemId);
            return ids;
        }
    }
}
