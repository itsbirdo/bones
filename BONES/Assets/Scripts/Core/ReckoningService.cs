namespace Bones.Core
{
    /// <summary>
    /// Pure logic for the Reckoning finale (spec §6.5): a best-of-three vs Vito.
    /// No money or RNG here, just the match-state decisions: who has clinched the
    /// match, and the deciding-game loading bump. The controller owns the rest.
    /// </summary>
    public static class ReckoningService
    {
        /// <summary>Games in the Reckoning match. You must win a majority of these.</summary>
        public const int GamesToPlay = 3;

        /// <summary>Wins needed to take the marker (majority of 3).</summary>
        public const int WinsToClinch = 2;

        /// <summary>Extra opponent loading on the deciding game (1-1), clamped to 1.0. Spec §6.5.</summary>
        public const double DecidingGameLoadingBump = 0.10;

        /// <summary>The match is over once either side reaches a majority or all games are played.</summary>
        public static bool MatchResolved(int playerWins, int vitoWins) =>
            playerWins >= WinsToClinch
            || vitoWins >= WinsToClinch
            || (playerWins + vitoWins) >= GamesToPlay;

        /// <summary>True once the player has clinched the marker (a majority of the 3 games).</summary>
        public static bool PlayerWonMatch(int playerWins, int vitoWins) => playerWins >= WinsToClinch;

        /// <summary>
        /// The deciding game is the one played when the match is level at 1-1 (the third throw).
        /// Vito saves his nastiest bones for it (spec §6.5).
        /// </summary>
        public static bool IsDecidingGame(int playerWins, int vitoWins) =>
            playerWins == 1 && vitoWins == 1;

        /// <summary>
        /// Opponent loading for the upcoming Reckoning game: the night's base loading, bumped on the
        /// 1-1 deciding game, clamped to [0, 1].
        /// </summary>
        public static double EffectiveLoading(double baseLoading, int playerWins, int vitoWins)
        {
            double loading = baseLoading;
            if (IsDecidingGame(playerWins, vitoWins)) loading += DecidingGameLoadingBump;
            if (loading < 0.0) loading = 0.0;
            if (loading > 1.0) loading = 1.0;
            return loading;
        }
    }
}
