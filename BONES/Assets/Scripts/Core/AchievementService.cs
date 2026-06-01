using System;
using System.Collections.Generic;

namespace Bones.Core
{
    /// <summary>
    /// What just happened, plus the lifetime counters needed to judge cumulative achievements.
    /// A flat, engine-free snapshot the GameController fills from AccountState + the settled game or
    /// run event. RNG and Unity stay out so this is fully unit-testable.
    ///
    /// Counters are the running lifetime totals AFTER the current event has been applied (e.g. if this
    /// signal is "won a game", GamesWon already includes that win). Triggers compare with >= so a
    /// threshold fires the moment it is reached.
    /// </summary>
    public readonly struct AchievementSignal
    {
        // The event that triggered this evaluation pass.
        public readonly AchievementEvent Event;

        // Per-event detail (only meaningful for the matching Event; zero/false otherwise).
        public readonly bool WasWin;          // GamePlayed: the banker won and was not busted
        public readonly bool WasBust;         // GamePlayed: the win was busted
        public readonly CeeloKind BankerKind; // GamePlayed: the banker's settled Cee-lo hand
        public readonly int BankerValue;      // GamePlayed: triple/point value (for 6-6-6 etc.)
        public readonly int HighestNightReached; // RunEnded/NightSurvived: 1-based night number reached

        // Lifetime counters (post-event totals).
        public readonly int GamesWon;
        public readonly int FourFiveSixCount;
        public readonly int TripleCount;
        public readonly int Busts;
        public readonly int Deaths;
        public readonly int RunsWon;

        public AchievementSignal(
            AchievementEvent ev,
            bool wasWin = false,
            bool wasBust = false,
            CeeloKind bankerKind = CeeloKind.Nothing,
            int bankerValue = 0,
            int highestNightReached = 0,
            int gamesWon = 0,
            int fourFiveSixCount = 0,
            int tripleCount = 0,
            int busts = 0,
            int deaths = 0,
            int runsWon = 0)
        {
            Event = ev;
            WasWin = wasWin;
            WasBust = wasBust;
            BankerKind = bankerKind;
            BankerValue = bankerValue;
            HighestNightReached = highestNightReached;
            GamesWon = gamesWon;
            FourFiveSixCount = fourFiveSixCount;
            TripleCount = tripleCount;
            Busts = busts;
            Deaths = deaths;
            RunsWon = runsWon;
        }
    }

    /// <summary>The kinds of moments that can satisfy an achievement.</summary>
    public enum AchievementEvent
    {
        GamePlayed,     // a single Cee-lo game settled (win/loss/push/bust)
        NightSurvived,  // a Collection was met and the night cleared
        RunEnded,       // the run ended (victory, broke, or whacked)
    }

    /// <summary>
    /// One unlock rule: an id, the noir line shown when it fires, the item id it unlocks, whether that
    /// reward item is actually implemented today, and the predicate that decides if a signal satisfies it.
    /// </summary>
    public sealed class Achievement
    {
        public readonly string Id;
        public readonly string UnlockLine;
        public readonly string RewardItemId;
        public readonly bool RewardImplemented;
        public readonly Func<AchievementSignal, bool> Trigger;

        public Achievement(string id, string unlockLine, string rewardItemId,
            bool rewardImplemented, Func<AchievementSignal, bool> trigger)
        {
            Id = id;
            UnlockLine = unlockLine;
            RewardItemId = rewardItemId;
            RewardImplemented = rewardImplemented;
            Trigger = trigger;
        }
    }

    /// <summary>
    /// Pure achievement evaluation. Holds the unlock table (ACHIEVEMENTS.md) as data and, given a
    /// signal plus the set of already-unlocked item ids, returns the achievements newly satisfied.
    ///
    /// IMPORTANT: only achievements whose reward item is actually implemented are routed onto the live
    /// unlock path (see <see cref="ActiveTable"/>). Entries for stubbed/deferred items are encoded in
    /// <see cref="StubTable"/> for completeness and clearly flagged RewardImplemented = false; they are
    /// NOT evaluated by the live path until their systems exist, so non-functional dice never enter the
    /// Fence pool.
    /// </summary>
    public static class AchievementService
    {
        // ---- Active table: every reward here is an IMPLEMENTED catalog item (see CATALOG.md). ----
        // These are the achievements GameController actually evaluates. Triggers are tuning
        // placeholders drawn from ACHIEVEMENTS.md, kept generous and early so the pool visibly grows.
        public static readonly IReadOnlyList<Achievement> ActiveTable = new List<Achievement>
        {
            // Win your first game -> Streak Charm (a clean charm reward for the first win).
            new Achievement("first_win",
                "First clean win. A fence slides you a Streak Charm to keep the table warm.",
                "streak_charm", true,
                s => s.Event == AchievementEvent.GamePlayed && s.WasWin && s.GamesWon >= 1),

            // Win 3 games (the hot hand) -> Hot Hand charm.
            new Achievement("three_wins",
                "Three pots on the night. They start calling you the Hot Hand.",
                "hot_hand", true,
                s => s.Event == AchievementEvent.GamePlayed && s.WasWin && s.GamesWon >= 3),

            // First 4-5-6 (headcrack) -> Headcracker.
            new Achievement("first_456",
                "Four, five, six. A headcrack. Somebody quietly leaves you a Headcracker.",
                "headcracker", true,
                s => s.Event == AchievementEvent.GamePlayed
                     && s.BankerKind == CeeloKind.FourFiveSix && s.FourFiveSixCount >= 1),

            // First triple -> The Magnet (the triple-maker).
            new Achievement("first_triple",
                "A triple on the pavement. The Magnet finds its way into your cup.",
                "the_magnet", true,
                s => s.Event == AchievementEvent.GamePlayed
                     && s.BankerKind == CeeloKind.Triple && s.TripleCount >= 1),

            // Survive your first night -> Greased Palm (a clutch favor).
            new Achievement("survive_first_night",
                "You cleared the Collection and walked. A Greased Palm waits for next time.",
                "greased_palm", true,
                s => s.Event == AchievementEvent.NightSurvived && s.HighestNightReached >= 1),

            // Reach Night 3 -> Shaved Edge (a stronger everyday cheat by now).
            new Achievement("reach_night_3",
                "Deep enough that honest bones won't cut it. Here, a Shaved Edge.",
                "shaved_edge", true,
                s => s.HighestNightReached >= 3
                     && (s.Event == AchievementEvent.NightSurvived || s.Event == AchievementEvent.RunEnded)),

            // Reach Night 5 -> The Sequencer (a 4-5-6 manufacturer for the deep end).
            new Achievement("reach_night_5",
                "The late tables. The Sequencer turns up, ready to finish a run for you.",
                "the_sequencer", true,
                s => s.HighestNightReached >= 5
                     && (s.Event == AchievementEvent.NightSurvived || s.Event == AchievementEvent.RunEnded)),

            // Get busted (a failure unlock) -> Cooler Head (a persistent favor).
            new Achievement("first_bust",
                "Caught with your thumb on the bone. You learn to keep a Cooler Head.",
                "cooler_head", true,
                s => s.Event == AchievementEvent.GamePlayed && s.WasBust && s.Busts >= 1),
        };

        // ---- Stub table: encoded for completeness from ACHIEVEMENTS.md, but the reward item is NOT
        // implemented yet (stays registered + locked). NOT evaluated by the live path. When the named
        // systems land, flip RewardImplemented to true and move the entry into ActiveTable. ----
        public static readonly IReadOnlyList<Achievement> StubTable = new List<Achievement>
        {
            // First death -> Two-Face. Reward NOT functional yet (needs per-die face-restriction resolver).
            new Achievement("first_death",
                "Nothing left to lose. The classic first-death pity prize.",
                "two_face", false,
                s => s.Event == AchievementEvent.RunEnded && s.Deaths >= 1),

            // Beat Vito (win a run) -> Set-Bone. Reward NOT functional yet (needs set-a-die input system).
            new Achievement("beat_vito",
                "You beat Vito and bought your freedom. The Set-Bone is yours.",
                "set_bone", false,
                s => s.Event == AchievementEvent.RunEnded && s.RunsWon >= 1),

            // Die 3 times -> All-or-Nothing. Reward NOT functional yet (needs double-swing settle).
            new Achievement("die_three_times",
                "Three flameouts. Time for All-or-Nothing.",
                "all_or_nothing", false,
                s => s.Event == AchievementEvent.RunEnded && s.Deaths >= 3),
        };

        /// <summary>
        /// Evaluate the live (implemented-reward) table against a signal. Returns the achievements whose
        /// trigger just fired and whose reward item is not already unlocked. Pure: no side effects.
        /// </summary>
        public static List<Achievement> Evaluate(AchievementSignal signal, ICollection<string> unlockedIds)
        {
            var newly = new List<Achievement>();
            foreach (var a in ActiveTable)
            {
                if (!a.RewardImplemented) continue; // safety: never route an unimplemented reward
                if (unlockedIds != null && unlockedIds.Contains(a.RewardItemId)) continue;
                if (a.Trigger != null && a.Trigger(signal)) newly.Add(a);
            }
            return newly;
        }
    }
}
