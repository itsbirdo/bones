using System;
using System.Collections.Generic;

namespace Bones.Model
{
    /// <summary>An owned die instance: which definition, and its honed level.</summary>
    [Serializable]
    public class OwnedDie
    {
        public string dieId;
        public int level = 1;
    }

    /// <summary>An owned item instance with remaining charges (for limited-use items).</summary>
    [Serializable]
    public class OwnedItem
    {
        public string itemId;
        public int chargesRemaining = 1;
    }

    /// <summary>
    /// Per-run state — reset on a new run. Bankroll, the cup loadout (3 die ids), owned dice/items,
    /// night index, and the debt. Plain [Serializable] for JSON persistence.
    /// </summary>
    [Serializable]
    public class RunState
    {
        public int bankroll;
        public int nightIndex;                 // 0-based index into the campaign
        public List<OwnedDie> ownedDice = new();
        public List<OwnedItem> ownedItems = new();
        public string[] cup = new string[3];   // die ids in the three cup slots (null = bone)
        public int runsThisLifetimeAtStart;    // for context/achievements

        public bool Broke => bankroll < 1;
    }

    /// <summary>
    /// Per-night state — reset at the start of each night. Heat/Suspicion build across the night's
    /// 3 games; games-played counts down the throws at the debt.
    /// </summary>
    [Serializable]
    public class NightState
    {
        public int gamesPlayed;
        public int consecutiveWins;            // drives Heat = 1 + 0.5 × this
        public bool layingLowThisGame;

        // The Reckoning (spec §6.5): best-of-three vs Vito. Reset each night; only used on the
        // Reckoning night. reckoningWins/Losses track the match score across the 3 games.
        public int reckoningWins;
        public int reckoningLosses;

        public void ResetForNewNight()
        {
            gamesPlayed = 0;
            consecutiveWins = 0;
            layingLowThisGame = false;
            reckoningWins = 0;
            reckoningLosses = 0;
        }

        public void OnReckoningGame(bool playerWon)
        {
            if (playerWon) reckoningWins++;
            else reckoningLosses++;
        }

        public void OnWin() => consecutiveWins++;
        public void OnLossOrBust() => consecutiveWins = 0;
    }

    /// <summary>Permanent, cross-run account state: unlocked item pool + lifetime stats.</summary>
    [Serializable]
    public class AccountState
    {
        public List<string> unlockedIds = new();
        public int runsStarted;
        public int runsWon;
        public int deaths;
        public int busts;
        public int biggestPot;
        public bool fenceUnlocked;             // hidden until the first game resolves

        public bool IsUnlocked(string id) => unlockedIds.Contains(id);

        public bool Unlock(string id)
        {
            if (string.IsNullOrEmpty(id) || unlockedIds.Contains(id)) return false;
            unlockedIds.Add(id);
            return true;
        }
    }

    /// <summary>The whole save payload: account (permanent) + the active run (resumable).</summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public AccountState account = new();
        public RunState activeRun;             // null when no run is in progress
        public bool hasActiveRun;
    }
}
