using UnityEngine;

namespace Bones.Data
{
    public enum ItemKind
    {
        Charm,  // persistent payout/Heat booster (e.g. Gilded Die)
        Favor,  // consumable Suspicion/risk manager (e.g. Lookout)
        Trinket // survival/economy aid (e.g. Brass Knuckles)
    }

    public enum Durability { Persistent, LimitedUse }

    /// <summary>
    /// Non-die items: charms, favors, trinkets. MVP uses a small set (e.g. Lookout favor).
    /// Effects are interpreted by the economy/suspicion layer via <see cref="effectTag"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "BONES/Item Definition", fileName = "Item_")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id = "lookout";
        public string displayName = "Lookout";
        [TextArea] public string flavor = "A kid on the corner whistles when the heat's near.";
        public ItemKind kind = ItemKind.Favor;
        public Durability durability = Durability.LimitedUse;

        [Header("Effect")]
        [Tooltip("Interpreted by gameplay: e.g. suspicion_reduce, payout_mult, heat_amp, reprieve.")]
        public string effectTag = "suspicion_reduce";
        [Tooltip("Magnitude for the effect (e.g. 0.5 = +50% payout, or 0.03 suspicion reduction).")]
        public float magnitude = 0.03f;
        [Tooltip("Charges for limited-use items (ignored if persistent).")]
        public int charges = 1;

        [Header("Economy")]
        public int basePrice = 0;

        [Header("Presentation")]
        public Sprite cardIcon;

        public int PriceAtNight(int night) => Mathf.RoundToInt(basePrice * (1f + 0.5f * (night - 1)));
    }
}
