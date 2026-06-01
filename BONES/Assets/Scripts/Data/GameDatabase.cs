using System.Collections.Generic;
using UnityEngine;

namespace Bones.Data
{
    /// <summary>
    /// The single content catalog: all dice/items, the campaign, juice tuning, and the starting
    /// core pool. One authored asset wires the whole game together (assign in the bootstrap scene).
    /// </summary>
    [CreateAssetMenu(menuName = "BONES/Game Database", fileName = "GameDatabase")]
    public class GameDatabase : ScriptableObject
    {
        [Header("Catalog")]
        public DieDefinition boneDie;            // free default that fills empty cup slots
        public List<DieDefinition> allDice = new();
        public List<ItemDefinition> allItems = new();

        [Header("Campaign & tuning")]
        public CampaignConfig campaign;
        public JuiceTierConfig juice;

        [Header("Starting pool (ACHIEVEMENTS.md first-time core)")]
        [Tooltip("Item ids unlocked for a brand-new account: snake_killer, lucky_six, gilded_die.")]
        public List<string> startingUnlocks = new() { "snake_killer", "lucky_six", "gilded_die" };

        [Header("Dev aids")]
        [Tooltip("Development aid: when ON, the HUD shows the Heat and Suspicion meters as raw numbers. " +
            "Should be OFF for release (the meters become 'felt only' per the main spec; the atmospheric " +
            "conveyance is deferred with art).")]
        public bool showMetersInDev = true;

        public DieDefinition FindDie(string id)
        {
            if (boneDie != null && boneDie.id == id) return boneDie;
            foreach (var d in allDice) if (d != null && d.id == id) return d;
            return null;
        }

        public ItemDefinition FindItem(string id)
        {
            foreach (var i in allItems) if (i != null && i.id == id) return i;
            return null;
        }
    }
}
