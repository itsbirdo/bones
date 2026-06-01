using System;

namespace Bones.Core
{
    /// <summary>
    /// Pure money math: Heat multiplier, payout on a win, stake bounds, and collection settlement.
    /// Mirrors ECONOMY.md (seed $18, min stake $1, Heat = 1 + 0.5 × consecutive wins).
    /// </summary>
    public static class EconomyService
    {
        public const int SeedBankroll = 18;
        public const int MinStake = 1;
        public const double HeatPerWin = 0.5;

        /// <summary>Heat = 1 + 0.5 × consecutive wins this night. Resets on a loss/bust/new night.</summary>
        public static double Heat(int consecutiveWins)
        {
            if (consecutiveWins < 0) consecutiveWins = 0;
            return 1.0 + HeatPerWin * consecutiveWins;
        }

        /// <summary>
        /// Net change to the bankroll for one settled game.
        /// Win → +stake × baseMultiplier × Heat. Loss → −stake. Push → 0.
        /// (A win returns the stake plus the multiplied winnings; net gain is stake×mult×heat.)
        /// </summary>
        public static double Settle(GameOutcome outcome, int stake, CeeloResult bankerResult,
            int consecutiveWinsBeforeThisGame, PayoutTable payouts)
        {
            switch (outcome)
            {
                case GameOutcome.BankerWin:
                    double heat = Heat(consecutiveWinsBeforeThisGame);
                    return stake * payouts.MultiplierFor(bankerResult) * heat;
                case GameOutcome.BankerLoss:
                    return -stake;
                default:
                    return 0.0;
            }
        }

        /// <summary>Largest legal stake: capped by current bankroll (and never below the minimum).</summary>
        public static int MaxStake(int bankroll) => Math.Max(0, bankroll);

        /// <summary>You are broke (game over) when you cannot cover the minimum stake.</summary>
        public static bool IsBroke(int bankroll) => bankroll < MinStake;

        public static int ClampStake(int desired, int bankroll)
        {
            int max = MaxStake(bankroll);
            if (desired < MinStake) return MinStake;
            if (desired > max) return max;
            return desired;
        }

        /// <summary>
        /// Clamp a stake to the smaller of the bankroll cap and an extra cap (e.g. the night's tribute),
        /// never below the minimum. The min stake always wins: even if both caps fall below it (a near-broke
        /// player, a tiny tribute) the result is MinStake, so the stepper never produces an illegal stake.
        /// </summary>
        public static int ClampStake(int desired, int bankroll, int extraCap)
        {
            int max = Math.Min(MaxStake(bankroll), Math.Max(0, extraCap));
            if (max < MinStake) max = MinStake;
            if (desired < MinStake) return MinStake;
            if (desired > max) return max;
            return desired;
        }

        /// <summary>You survive the night's Collection if the bankroll covers the demand.</summary>
        public static bool CanMeetCollection(int bankroll, int demand) => bankroll >= demand;
    }
}
