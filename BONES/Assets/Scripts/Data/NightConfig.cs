using UnityEngine;
using Bones.Core;

namespace Bones.Data
{
    /// <summary>
    /// One night of the run: the Collection demand plus the two Squeeze levers
    /// (tie-rule drift and opponent loading). Spec §6.3–6.4 / ECONOMY.md / LEVELS_AND_FLOW.md.
    /// </summary>
    [CreateAssetMenu(menuName = "BONES/Night Config", fileName = "Night_")]
    public class NightConfig : ScriptableObject
    {
        public int nightNumber = 1;
        [Tooltip("Cash the Collector demands at the end of this night. Miss it = whacked.")]
        public int collectionDemand = 20;

        [Header("The Squeeze")]
        public TieRule tieRule = TieRule.Banker;
        [Tooltip("0 = fair mark; higher leans the mark's dice high (loaded back at you).")]
        [Range(0f, 1f)] public float opponentLoading = 0f;

        [Header("Flow")]
        public int gamesThisNight = 3;
        [Tooltip("The final night: 3 games vs Vito, win 2 of 3, Suspicion off, no tribute (spec §6.5).")]
        public bool isReckoning = false;
    }

    /// <summary>Ordered list of nights = the campaign. Ships Nights 1–7 (Night 7 = the Reckoning).</summary>
    [CreateAssetMenu(menuName = "BONES/Campaign", fileName = "Campaign")]
    public class CampaignConfig : ScriptableObject
    {
        public NightConfig[] nights;

        public int NightCount => nights != null ? nights.Length : 0;
        public NightConfig NightAt(int index) =>
            (nights != null && index >= 0 && index < nights.Length) ? nights[index] : null;
    }
}
