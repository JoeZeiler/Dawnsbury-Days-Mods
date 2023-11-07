using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Modding;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Display.Illustrations;
using System;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using System.Linq;

namespace Dawnsbury.Mods.Weapons.StellarCannon;

public class StellarCannonLoader
{
    private static Trait Analogue;
    private static Trait Unwieldy;
    private static Trait AreaBurst10ft;
    private static Trait Area;
    private static ModdedIllustration StellarCannonIllustration;

    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        Analogue = ModManager.RegisterTrait("Automatic", new TraitProperties("Analogue", true, "does not rely on digital technology"));
        Unwieldy = ModManager.RegisterTrait("Unwieldy", new TraitProperties("Unwieldy", true, "You can’t use an unwieldy weapon more than once per round and can’t use it to Strike as part of a reaction",true));
        AreaBurst10ft = ModManager.RegisterTrait("Area10ft", new TraitProperties("Area (Burst 10 ft.)", true, "Weapons with this trait can only fire\r\nusing the Area Fire action.", true));
        Area = ModManager.RegisterTrait("Area", new TraitProperties("Area", true, "the effect happens in a targeted area", true));
        StellarCannonIllustration = new ModdedIllustration(@"StellarCannonResources\StellarCannon.png");

        ModManager.RegisterActionOnEachCreature(ApplyAreaFire);
        ModManager.RegisterNewItemIntoTheShop("StellarCannon0", CreateStellarCannon);
        ModManager.RegisterNewItemIntoTheShop("StellarCannon1", CreateTacticalStellarCannon);
        ModManager.RegisterNewItemIntoTheShop("StellarCannon2", CreateAdvancedStellarCannon);

        //ModManager.AddFeat(CreateBonMotFeat(insultDirectory));
    }

    private static Action<Creature> ApplyAreaFire = (creature) =>
    {
        creature.AddQEffect(new QEffect()
        {
            StateCheck = (qfSelf) =>
            {
                creature = qfSelf.Owner;
                var areaItems = creature.HeldItems.FindAll(heldItem => heldItem is AreaItem).Cast<AreaItem>();
                if (areaItems.Count() <= 0)
                {
                    return;
                }
                foreach (var areaItem in areaItems)
                {
                    generateAreaFireActions(qfSelf.Owner,areaItem);
                }
            },

        });
    };

    private static void generateAreaFireActions(Creature itemOwner, AreaItem areaItem)
    {
        string areaType = "burst";
        int effectRange = effectRange = areaItem.AreaRange;
        Target target = Target.Burst(areaItem.WeaponProperties.RangeIncrement,effectRange);

        switch (areaItem.AreaType)
        {
            case AreaItem.AreaTypes.Burst:
                areaType = "burst";
                target = Target.Burst(areaItem.WeaponProperties.RangeIncrement, effectRange);
                break;
        }

        CombatAction areaFireAction = null;

        QEffect areaFireEffect = new QEffect(ExpirationCondition.Ephemeral)
        {
            ProvideMainAction = (qfSelf) =>
            {
                var weaponOwner = qfSelf.Owner;
                var martialTraining = weaponOwner.Proficiencies.Get(Trait.Martial);
                var simpleTraining = weaponOwner.Proficiencies.Get(Trait.Simple);
                var trainedBonus = weaponOwner.Level;

                if (areaItem.Traits.Contains(Trait.Martial))
                {
                    //+4 is too good to add for an area reflex save, so nerfing fighters here.
                    //This is closer to how starfinder does it since that uses class DC, and nothing will have higher than trained at the first 4 levels... but I wanted weapon training and ability mod to matter
                    trainedBonus = martialTraining == Proficiency.Untrained ? 0 : weaponOwner.Level + 2;
                }
                if (areaItem.Traits.Contains(Trait.Simple))
                {
                    //+4 is too good to add for an area reflex save, so nerfing fighters here.
                    //This is closer to how starfinder does it since that uses class DC, and nothing will have higher than trained at the first 4 levels... but I wanted weapon training and ability mod to matter
                    trainedBonus = simpleTraining == Proficiency.Untrained ? 0 : weaponOwner.Level + 2;
                }

                areaFireAction = new CombatAction(weaponOwner, IllustrationName.BurningJet,
                    areaItem.Name + " Area Fire[" + ((EphemeralAreaProperties)areaItem.EphemeralItemProperties).CurrentMagazine + "/" + areaItem.Capacity + "]",
                    new[] { Area, Trait.Attack },
                    "DC " + (trainedBonus + Math.Max(weaponOwner.Abilities.Strength, weaponOwner.Abilities.Constitution) + 10 + areaItem.WeaponProperties.ItemBonus) + " Basic Reflex. " +
                     "use an area fire weapon to attack in a " + effectRange * 5 + " ft. " + areaType + " for " + areaItem.WeaponProperties.Damage + " " + areaItem.WeaponProperties.DamageKind.ToString() + " damage.", target).WithActionCost(2)
                    .WithSavingThrow(new SavingThrow(Defense.Reflex, (creature) => trainedBonus + Math.Max(creature.Abilities.Strength, creature.Abilities.Constitution) + 10 + areaItem.WeaponProperties.ItemBonus))
                    .WithEffectOnSelf((self) => ((EphemeralAreaProperties)areaItem.EphemeralItemProperties).CurrentMagazine -= areaItem.Usage)
                    .WithEffectOnEachTarget(async (action, user, target, result) =>
                    {
                        await CommonSpellEffects.DealBasicDamage(action, user, target, result, areaItem.WeaponProperties.Damage, areaItem.WeaponProperties.DamageKind);
                        //only level 4 means no critical specialization effects, yay!
                    });

                return new ActionPossibility(areaFireAction);
            }
        };
        itemOwner.AddQEffect(areaFireEffect);

        if (((EphemeralAreaProperties)areaItem.EphemeralItemProperties).CurrentMagazine < areaItem.Capacity)
        {
            QEffect reloadEffect = new QEffect(ExpirationCondition.Ephemeral)
            {
                ProvideMainAction = (qfSelf) =>
                {
                    CombatAction reloadAction = new CombatAction(itemOwner, StellarCannonIllustration, "Reload " + areaItem.Name, new[] { Trait.Interact }, "interact to reload", Target.Self()).WithActionCost(areaItem.Reload)
                    .WithEffectOnSelf((self) => ((EphemeralAreaProperties)areaItem.EphemeralItemProperties).CurrentMagazine = areaItem.Capacity);

                    return new ActionPossibility(reloadAction);
                }
            };

            itemOwner.AddQEffect(reloadEffect);
        }
        
    }

    private static Item CreateStellarCannon(ItemName iName)
    {
        AreaItem stellarCannon = new AreaItem(iName, IllustrationName.FireRay, "Stellar Cannon, Commercial", 0, 4, new[] { Analogue, Unwieldy, Trait.Ranged, Trait.TwoHanded, AreaBurst10ft, Trait.Martial})
        {
            MainTrait = AreaBurst10ft,
            WeaponProperties = new WeaponProperties("1d10", DamageKind.Piercing) { ItemBonus = 0 }.WithRangeIncrement(10),
            Reload = 1,
            Capacity = 8,
            Usage = 2,
            AreaType = AreaItem.AreaTypes.Burst,
            AreaRange = 2,
            EphemeralItemProperties = new EphemeralAreaProperties() { CurrentMagazine = 8},
        };

        stellarCannon.Illustration = StellarCannonIllustration;
        return stellarCannon;
    }

    private static Item CreateTacticalStellarCannon(ItemName iName)
    {
        AreaItem stellarCannon = new AreaItem(iName, IllustrationName.FireRay, "Stellar Cannon, Tactical", 0, 39, new[] { Analogue, Unwieldy, Trait.Ranged, Trait.TwoHanded, AreaBurst10ft, Trait.Martial })
        {
            MainTrait = AreaBurst10ft,
            WeaponProperties = new WeaponProperties("1d10", DamageKind.Piercing) { ItemBonus = 1 }.WithRangeIncrement(10),
            Reload = 1,
            Capacity = 8,
            Usage = 2,
            AreaType = AreaItem.AreaTypes.Burst,
            AreaRange = 2,
            EphemeralItemProperties = new EphemeralAreaProperties() { CurrentMagazine = 8 },
        };

        stellarCannon.Illustration = StellarCannonIllustration;
        return stellarCannon;
    }

    private static Item CreateAdvancedStellarCannon(ItemName iName)
    {
        AreaItem stellarCannon = new AreaItem(iName, IllustrationName.FireRay, "Stellar Cannon, Advanced", 0,104, new[] { Analogue, Unwieldy, Trait.Ranged, Trait.TwoHanded, AreaBurst10ft, Trait.Martial })
        {
            MainTrait = AreaBurst10ft,
            WeaponProperties = new WeaponProperties("2d10", DamageKind.Piercing) { ItemBonus = 1 }.WithRangeIncrement(10),
            Reload = 1,
            Capacity = 8,
            Usage = 2,
            AreaType = AreaItem.AreaTypes.Burst,
            AreaRange = 2,
            EphemeralItemProperties = new EphemeralAreaProperties() { CurrentMagazine = 8 },
        };

        stellarCannon.Illustration = StellarCannonIllustration;
        return stellarCannon;
    }

}

public class EphemeralAreaProperties : EphemeralItemProperties
{
    public int CurrentMagazine
    {
        get; set;
    }
}

public class AreaItem : Item
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
    public int Usage
    {
        get; set;
    } = 1;
    public int Capacity
    {
        get; set;
    } = 5;
    public int Reload
    {
        get; set;
    } = 1;
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