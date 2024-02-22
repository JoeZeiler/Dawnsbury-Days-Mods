using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core;
using Dawnsbury.Display.Illustrations;

namespace Dawnsbury.Mods.Weapons.StarfinderWeapons
{
    /// <summary>
    /// stores ephemeral properties for area weapons
    /// </summary>
    public class EphemeralAreaProperties : EphemeralItemProperties
    {
        public int CurrentMagazine
        {
            get; set;
        }
    }

    /// <summary>
    /// stores properties for guns
    /// </summary>
    public class GunItem : Item
    {
        public GunItem(IllustrationName illustration, string name, params Trait[] traits) : base(illustration, name, traits)
        {

        }
        public GunItem(ItemName itemName, IllustrationName illustration, string name, int level, int price, params Trait[] traits) : base(itemName, (Illustration)illustration, name, level, price, traits)
        {
        }

        public string BaseGunItemName
        {
            get; set;
        }

        public int Usage
        {
            get; set;
        } = 1;

        public int CommercialUsage
        {
            get; set;
        } = 1;
        public int TacticalUsage
        {
            get; set;
        } = 1;
        public int AdvancedUsage
        {
            get; set;
        } = 2;

        public int Capacity
        {
            get; set;
        } = 5;

        public int CommercialCapacity
        {
            get; set;
        } = 5;
        public int TacticalCapacity
        {
            get; set;
        } = 10;
        public int AdvancedCapacity
        {
            get; set;
        } = 10;

        public int CommercialRange
        {
            get; set;
        } = 6;
        public int TacticalRange
        {
            get; set;
        } = 8;
        public int AdvancedRange
        {
            get; set;
        } = 8;

        public int Reload
        {
            get; set;
        } = 1;
    }

    /// <summary>
    /// stores properties for area items
    /// </summary>
    public class AreaItem : GunItem
    {
        public enum AreaTypes
        {
            Burst,
            Cone,
            Line
        }
        public AreaItem(IllustrationName illustration, string name, params Trait[] traits) : base(illustration, name, traits)
        {

        }

        public AreaItem(ItemName itemName, IllustrationName illustration, string name, int level, int price, params Trait[] traits) : base(itemName, illustration, name, level, price, traits)
        {
        }
        public AreaTypes AreaType
        {
            get; set;
        } = AreaTypes.Burst;
        public int AreaRange
        {
            get; set;
        } = 1;
        public int Tracking
        {
            get; set;
        } = 0;
    }
}
