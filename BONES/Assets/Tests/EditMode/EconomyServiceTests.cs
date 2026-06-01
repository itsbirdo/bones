using NUnit.Framework;
using Bones.Core;

namespace Bones.Tests
{
    public class EconomyServiceTests
    {
        [Test]
        public void Heat_FollowsOnePlusHalfPerWin()
        {
            Assert.AreEqual(1.0, EconomyService.Heat(0), 1e-9);
            Assert.AreEqual(1.5, EconomyService.Heat(1), 1e-9);
            Assert.AreEqual(2.0, EconomyService.Heat(2), 1e-9);
            Assert.AreEqual(1.0, EconomyService.Heat(-3), 1e-9); // clamped
        }

        [Test]
        public void Settle_PointWin_PaysStakeTimesHeat()
        {
            var point = CeeloEngine.Evaluate(3, 3, 5); // point, 1:1
            // 1 win already this night -> Heat 1.5.
            double delta = EconomyService.Settle(GameOutcome.BankerWin, 10, point, 1, PayoutTable.Default);
            Assert.AreEqual(15.0, delta, 1e-9);
        }

        [Test]
        public void Settle_JackpotsUseTheirMultipliers()
        {
            var fourFiveSix = CeeloEngine.Evaluate(4, 5, 6);
            var triple = CeeloEngine.Evaluate(6, 6, 6);
            Assert.AreEqual(20.0, EconomyService.Settle(GameOutcome.BankerWin, 10, fourFiveSix, 0, PayoutTable.Default), 1e-9);
            Assert.AreEqual(30.0, EconomyService.Settle(GameOutcome.BankerWin, 10, triple, 0, PayoutTable.Default), 1e-9);
        }

        [Test]
        public void Settle_LossAndPush()
        {
            var point = CeeloEngine.Evaluate(3, 3, 5);
            Assert.AreEqual(-10.0, EconomyService.Settle(GameOutcome.BankerLoss, 10, point, 5, PayoutTable.Default), 1e-9);
            Assert.AreEqual(0.0, EconomyService.Settle(GameOutcome.Push, 10, point, 5, PayoutTable.Default), 1e-9);
        }

        [Test]
        public void Broke_WhenBankrollBelowMinStake()
        {
            Assert.IsTrue(EconomyService.IsBroke(0));
            Assert.IsFalse(EconomyService.IsBroke(1));
            Assert.IsFalse(EconomyService.IsBroke(EconomyService.SeedBankroll));
        }

        [Test]
        public void ClampStake_RespectsMinAndBankroll()
        {
            Assert.AreEqual(1, EconomyService.ClampStake(0, 20));   // below min
            Assert.AreEqual(20, EconomyService.ClampStake(50, 20)); // above bankroll
            Assert.AreEqual(7, EconomyService.ClampStake(7, 20));   // in range
        }

        [Test]
        public void ClampStake_WithExtraCap_TakesSmallerOfBankrollAndCap()
        {
            // Tribute caps below bankroll: the tribute wins.
            Assert.AreEqual(20, EconomyService.ClampStake(50, 100, 20));
            // Bankroll caps below tribute: the bankroll wins.
            Assert.AreEqual(15, EconomyService.ClampStake(50, 15, 20));
            // Desired sits under both caps: unchanged.
            Assert.AreEqual(7, EconomyService.ClampStake(7, 100, 20));
        }

        [Test]
        public void ClampStake_WithExtraCap_NeverBelowMinStake()
        {
            // Below min is always raised to the minimum.
            Assert.AreEqual(1, EconomyService.ClampStake(0, 100, 20));
            // Even when both caps fall below the minimum, the min stake wins.
            Assert.AreEqual(1, EconomyService.ClampStake(5, 0, 0));
            Assert.AreEqual(1, EconomyService.ClampStake(5, 100, 0));
        }

        [Test]
        public void Collection_MetWhenBankrollCoversDemand()
        {
            Assert.IsTrue(EconomyService.CanMeetCollection(20, 20));
            Assert.IsTrue(EconomyService.CanMeetCollection(21, 20));
            Assert.IsFalse(EconomyService.CanMeetCollection(19, 20));
        }
    }
}
