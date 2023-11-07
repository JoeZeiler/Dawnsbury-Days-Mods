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
using Dawnsbury.Core.Creatures.Parts;
using System.Collections.Generic;
using Dawnsbury.Core.CharacterBuilder;

namespace Dawnsbury.Mods.Weapons.StellarCannon;

/// <summary>
/// adds the stellar cannon and its variants from the starfinder 2E field test with some small modifications
/// </summary>
public class StarfinderWeaponsLoader
{
    private static Trait Analogue;
    private static Trait Unwieldy;
    private static Trait AreaBurst10ft;
    private static Trait Area;
    private static Trait AreaCone;
    private static Trait AreaLine;
    private static Trait Concussive;
    private static ModdedIllustration StellarCannonIllustration;
    private static ModdedIllustration ScattergunIllustration;
    private static ModdedIllustration FlamePistolIllustration;

    /// <summary>
    /// loads the appropriate mods
    /// </summary>
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        Analogue = ModManager.RegisterTrait("Automatic", new TraitProperties("Analogue", true, "does not rely on digital technology"));
        Unwieldy = ModManager.RegisterTrait("Unwieldy", new TraitProperties("Unwieldy", true, "You can’t use an unwieldy weapon more than once per round and can’t use it to Strike as part of a reaction",true));
        AreaBurst10ft = ModManager.RegisterTrait("AreaBurst10ft", new TraitProperties("Area (Burst 10 ft.)", true, "Weapons with this trait can only fire using the Area Fire action.", true));
        AreaCone = ModManager.RegisterTrait("AreaCone", new TraitProperties("Area (Cone)", true, "Weapons with this trait can only fire using the Area Fire action.", true));
        AreaLine = ModManager.RegisterTrait("AreaLine", new TraitProperties("Area (Line)", true, "Weapons with this trait can only fire using the Area Fire action.", true));
        Concussive = ModManager.RegisterTrait("Concussive", new TraitProperties("Concussive", true, "Use the weaker of the target’s resistance or immunity\r\nto piercing or to bludgeoning."));
        Area = ModManager.RegisterTrait("Area", new TraitProperties("Area", true, "the effect happens in a targeted area", true));
        StellarCannonIllustration = new ModdedIllustration(@"StarfinderWeaponsResources\StellarCannon.png");
        ScattergunIllustration = new ModdedIllustration(@"StarfinderWeaponsResources\Scattergun.png");
        FlamePistolIllustration = new ModdedIllustration(@"StarfinderWeaponsResources\FlamePistol.png");

        ModManager.RegisterActionOnEachCreature(ApplyAreaFire);
        ModManager.RegisterActionOnEachCreature(GenerateConcussiveEffect);
        ModManager.RegisterNewItemIntoTheShop("StellarCannon0", CreateStellarCannon);
        ModManager.RegisterNewItemIntoTheShop("StellarCannon1", CreateTacticalStellarCannon);
        ModManager.RegisterNewItemIntoTheShop("StellarCannon2", CreateAdvancedStellarCannon);
        ModManager.RegisterNewItemIntoTheShop("Scattergun0", CreateScattergun);
        ModManager.RegisterNewItemIntoTheShop("Scattergun1", CreateTacticalScattergun);
        ModManager.RegisterNewItemIntoTheShop("Scattergun2", CreateAdvancedScattergun);
        ModManager.RegisterNewItemIntoTheShop("FlamePistol0", CreateFlamePistol);
        ModManager.RegisterNewItemIntoTheShop("FlamePistol1", CreateTacticalFlamePistol);
        ModManager.RegisterNewItemIntoTheShop("FlamePistol2", CreateAdvancedFlamePistol);
    }

    /// <summary>
    /// creates the area fire and reload actions for equipped area weapons
    /// </summary>
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
                    GenerateAreaFireActions(qfSelf.Owner,areaItem);
                }
            },

        });
    };

    /// <summary>
    /// adds the concussive trait effects
    /// </summary>
    private static Action<Creature> GenerateConcussiveEffect = (creature) =>
    {
        creature.AddQEffect(new QEffect()
        {
            YourStrikeGainsDamageType = (qfSelf, cAction) =>
            {
                if(cAction.Item?.HasTrait(Concussive) != true)
                {
                    return null;
                }
                if(cAction.Item.WeaponProperties.DamageKind == DamageKind.Piercing)
                {
                    return DamageKind.Bludgeoning;
                }
                if(cAction.Item.WeaponProperties.DamageKind == DamageKind.Bludgeoning)
                {
                    return DamageKind.Piercing;
                }
                return null;
            }
        });
    };

    /// <summary>
    /// creates the actions to give the creature for the given area weapon
    /// </summary>
    /// <param name="itemOwner">the wielder of the weapon</param>
    /// <param name="areaItem">the item the action correlates to</param>
    private static void GenerateAreaFireActions(Creature itemOwner, AreaItem areaItem)
    {
        string areaType = "burst";
        int effectRange = areaItem.AreaRange;
        Target target = Target.Burst(areaItem.WeaponProperties.RangeIncrement,effectRange);

        switch (areaItem.AreaType)
        {
            case AreaItem.AreaTypes.Burst:
                areaType = "burst";
                target = Target.Burst(areaItem.WeaponProperties.RangeIncrement, effectRange);
                break;
            case AreaItem.AreaTypes.Cone:
                areaType = "cone";
                target = Target.Cone(areaItem.WeaponProperties.RangeIncrement);
                break;
            case AreaItem.AreaTypes.Line:
                areaType = "line";
                target = Target.Line(areaItem.WeaponProperties.RangeIncrement);
                break;
        }

        CombatAction areaFireAction = null;

        QEffect areaFireEffect = new QEffect(ExpirationCondition.Ephemeral)
        {
            PreventTakingAction = (action) =>
            {
                if(action.Item is AreaItem)
                {
                    AreaItem aItem = (AreaItem)action.Item;
                    if(((EphemeralAreaProperties)aItem.EphemeralItemProperties).CurrentMagazine < aItem.Usage)
                    {
                        return "Not Enough Ammo Loaded.";
                    }
                }
                return null;
            },
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
                     "use an area fire weapon to attack in a " + effectRange * 5 + " ft. " + areaType + " for " + areaItem.WeaponProperties.Damage + " " + areaItem.WeaponProperties.DamageKind.ToString() + " damage.", target)
                    { Item = areaItem}
                    .WithActionCost(2)
                    .WithSavingThrow(new SavingThrow(Defense.Reflex, (creature) =>
                    {
                        return trainedBonus + Math.Max(creature.Abilities.Strength, creature.Abilities.Constitution) + 10 + areaItem.WeaponProperties.ItemBonus;
                    }
                    ))
                    .WithEffectOnSelf((self) => ((EphemeralAreaProperties)areaItem.EphemeralItemProperties).CurrentMagazine -= areaItem.Usage)
                    .WithEffectOnEachTarget(async (action, user, target, result) =>
                    {
                        DamageKind kind = areaItem.WeaponProperties.DamageKind;
                        if (areaItem.Traits.Contains(Concussive))
                        {
                            kind = getBetterDamageKindAgainst(target, DamageKind.Piercing, DamageKind.Bludgeoning);
                        }
                        if (areaItem.Traits.Contains(Trait.VersatileP))
                        {
                            kind = getBetterDamageKindAgainst(target, kind, DamageKind.Piercing);
                        }
                        if(areaItem.Traits.Contains(Trait.VersatileS))
                        {
                            kind = getBetterDamageKindAgainst(target, kind, DamageKind.Slashing);
                        }

                        await CommonSpellEffects.DealBasicDamage(action, user, target, result, areaItem.WeaponProperties.Damage, kind);
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
                    CombatAction reloadAction = new CombatAction(itemOwner, areaItem.Illustration, "Reload " + areaItem.Name, new[] { Trait.Interact }, "interact to reload", Target.Self()).WithActionCost(areaItem.Reload)
                    .WithEffectOnSelf((self) => ((EphemeralAreaProperties)areaItem.EphemeralItemProperties).CurrentMagazine = areaItem.Capacity);

                    return new ActionPossibility(reloadAction);
                },
            };

            itemOwner.AddQEffect(reloadEffect);
        }
    }

    /// <summary>
    /// finds the better damage type to use against the target
    /// </summary>
    /// <param name="target">the target to check against</param>
    /// <param name="kindA">the first damage kind</param>
    /// <param name="kindB">the second damage kind</param>
    /// <returns>the better damage type. If it is the same for both damage kinds, kindA is returned.</returns>
    private static DamageKind getBetterDamageKindAgainst(Creature target, DamageKind kindA, DamageKind kindB)
    {
        if(target.WeaknessAndResistance.Immunities.Contains(kindB))
        {
            return kindA;
        }
        if (target.WeaknessAndResistance.Immunities.Contains(kindA))
        {
            return kindB;
        }

        Resistance resistanceA = target.WeaknessAndResistance.Resistances.FirstOrDefault(res => res.DamageKind == kindA,new Resistance(kindA,0,false));
        Resistance resistanceB = target.WeaknessAndResistance.Resistances.FirstOrDefault(res => res.DamageKind == kindB, new Resistance(kindB, 0, false));

        int resistValueA = resistanceA.Value * (resistanceA.IsWeakness ? -1 : 1);
        int resistValueB = resistanceB.Value * (resistanceB.IsWeakness ? -1 : 1);

        return resistValueB < resistValueA ? kindB : kindA;
    }

    /// <summary>
    /// creates the base stellar cannon
    /// </summary>
    /// <param name="iName">input from the game</param>
    /// <returns>the stellar cannon item</returns>
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

    /// <summary>
    /// creates the tactical stellar cannnon
    /// </summary>
    /// <param name="iName">input from the game</param>
    /// <returns>the stellar cannon item</returns>
    private static Item CreateTacticalStellarCannon(ItemName iName)
    {
        AreaItem stellarCannon = new AreaItem(iName, IllustrationName.FireRay, "Stellar Cannon, Tactical", 0, 39, new[] { Analogue, Unwieldy, Trait.Ranged, Trait.TwoHanded, AreaBurst10ft, Trait.Martial })
        {
            MainTrait = AreaBurst10ft,
            WeaponProperties = new WeaponProperties("1d10", DamageKind.Piercing) { ItemBonus = 1 }.WithRangeIncrement(10),
            Reload = 1,
            Capacity = 12,
            Usage = 2,
            AreaType = AreaItem.AreaTypes.Burst,
            AreaRange = 2,
            EphemeralItemProperties = new EphemeralAreaProperties() { CurrentMagazine = 12 },
        };

        stellarCannon.Illustration = StellarCannonIllustration;
        return stellarCannon;
    }

    /// <summary>
    /// creates the advanced stellar cannnon
    /// </summary>
    /// <param name="iName">input from the game</param>
    /// <returns>the stellar cannon item</returns>
    private static Item CreateAdvancedStellarCannon(ItemName iName)
    {
        AreaItem stellarCannon = new AreaItem(iName, IllustrationName.FireRay, "Stellar Cannon, Advanced", 0,104, new[] { Analogue, Unwieldy, Trait.Ranged, Trait.TwoHanded, AreaBurst10ft, Trait.Martial })
        {
            MainTrait = AreaBurst10ft,
            WeaponProperties = new WeaponProperties("2d10", DamageKind.Piercing) { ItemBonus = 1 }.WithRangeIncrement(10),
            Reload = 1,
            Capacity = 16,
            Usage = 4,
            AreaType = AreaItem.AreaTypes.Burst,
            AreaRange = 2,
            EphemeralItemProperties = new EphemeralAreaProperties() { CurrentMagazine = 16 },
        };

        stellarCannon.Illustration = StellarCannonIllustration;
        return stellarCannon;
    }

    /// <summary>
    /// creates the base scattergun
    /// </summary>
    /// <param name="iName">input from the game</param>
    /// <returns>the scattergun item</returns>
    private static Item CreateScattergun(ItemName iName)
    {
        AreaItem scattergun = new AreaItem(iName, IllustrationName.BurningHands, "Scattergun, Commercial", 0, 4, new[] { Analogue, Unwieldy, Trait.Ranged, Trait.TwoHanded, AreaCone, Trait.Simple, Concussive })
        {
            MainTrait = AreaCone,
            WeaponProperties = new WeaponProperties("1d6", DamageKind.Piercing) { ItemBonus = 0 }.WithRangeIncrement(3),
            Reload = 1,
            Capacity = 4,
            Usage = 1,
            AreaType = AreaItem.AreaTypes.Cone,
            AreaRange = 3,
            EphemeralItemProperties = new EphemeralAreaProperties() { CurrentMagazine = 4 },
        };

        scattergun.Illustration = ScattergunIllustration;
        return scattergun;
    }

    /// <summary>
    /// creates the tactical scattergun
    /// </summary>
    /// <param name="iName">input from the game</param>
    /// <returns>the scattergun item</returns>
    private static Item CreateTacticalScattergun(ItemName iName)
    {
        AreaItem scattergun = new AreaItem(iName, IllustrationName.BurningHands, "Scattergun, Tactical", 0, 39, new[] { Analogue, Unwieldy, Trait.Ranged, Trait.TwoHanded, AreaCone, Trait.Simple, Concussive })
        {
            MainTrait = AreaCone,
            WeaponProperties = new WeaponProperties("1d6", DamageKind.Piercing) { ItemBonus = 1 }.WithRangeIncrement(3),
            Reload = 1,
            Capacity = 6,
            Usage = 1,
            AreaType = AreaItem.AreaTypes.Cone,
            AreaRange = 3,
            EphemeralItemProperties = new EphemeralAreaProperties() { CurrentMagazine = 6 },
        };

        scattergun.Illustration = ScattergunIllustration;
        return scattergun;
    }

    /// <summary>
    /// creates the advanced scattergun
    /// </summary>
    /// <param name="iName">input from the game</param>
    /// <returns>the scattergun item</returns>
    private static Item CreateAdvancedScattergun(ItemName iName)
    {
        AreaItem scattergun = new AreaItem(iName, IllustrationName.BurningHands, "Scattergun, Advanced", 0, 104, new[] { Analogue, Unwieldy, Trait.Ranged, Trait.TwoHanded, AreaCone, Trait.Simple, Concussive })
        {
            MainTrait = AreaCone,
            WeaponProperties = new WeaponProperties("2d6", DamageKind.Piercing) { ItemBonus = 1 }.WithRangeIncrement(3),
            Reload = 1,
            Capacity = 8,
            Usage = 2,
            AreaType = AreaItem.AreaTypes.Cone,
            AreaRange = 3,
            EphemeralItemProperties = new EphemeralAreaProperties() { CurrentMagazine = 8 },
        };

        scattergun.Illustration = ScattergunIllustration;
        return scattergun;
    }

    /// <summary>
    /// creates the base flame pistol
    /// </summary>
    /// <param name="iName">input from the game</param>
    /// <returns>the flame pistol item</returns>
    private static Item CreateFlamePistol(ItemName iName)
    {
        AreaItem flamePistol = new AreaItem(iName, IllustrationName.BurningHands, "Flame Pistol, Commercial", 0, 2, new[] { Analogue, Unwieldy, Trait.Ranged, AreaLine, Trait.Simple })
        {
            MainTrait = AreaCone,
            WeaponProperties = new WeaponProperties("1d6", DamageKind.Fire) { ItemBonus = 0 }.WithRangeIncrement(3),
            Reload = 1,
            Capacity = 2,
            Usage = 1,
            AreaType = AreaItem.AreaTypes.Line,
            AreaRange = 3,
            EphemeralItemProperties = new EphemeralAreaProperties() { CurrentMagazine = 2 },
        };

        flamePistol.Illustration = FlamePistolIllustration;
        return flamePistol;
    }

    /// <summary>
    /// creates the tactical flame pistol
    /// </summary>
    /// <param name="iName">input from the game</param>
    /// <returns>the flame pistol item</returns>
    private static Item CreateTacticalFlamePistol(ItemName iName)
    {
        AreaItem flamePistol = new AreaItem(iName, IllustrationName.BurningHands, "Flame Pistol, Tactical", 0, 37, new[] { Analogue, Unwieldy, Trait.Ranged, AreaLine, Trait.Simple })
        {
            MainTrait = AreaCone,
            WeaponProperties = new WeaponProperties("1d6", DamageKind.Fire) { ItemBonus = 1 }.WithRangeIncrement(3),
            Reload = 1,
            Capacity = 3,
            Usage = 1,
            AreaType = AreaItem.AreaTypes.Line,
            AreaRange = 3,
            EphemeralItemProperties = new EphemeralAreaProperties() { CurrentMagazine = 3 },
        };

        flamePistol.Illustration = FlamePistolIllustration;
        return flamePistol;
    }

    /// <summary>
    /// creates the advanced flame pistol
    /// </summary>
    /// <param name="iName">input from the game</param>
    /// <returns>the flame pistol item</returns>
    private static Item CreateAdvancedFlamePistol(ItemName iName)
    {
        AreaItem flamePistol = new AreaItem(iName, IllustrationName.BurningHands, "Flame Pistol, Advanced", 0, 102, new[] { Analogue, Unwieldy, Trait.Ranged, AreaLine, Trait.Simple })
        {
            MainTrait = AreaCone,
            WeaponProperties = new WeaponProperties("2d6", DamageKind.Fire) { ItemBonus = 1 }.WithRangeIncrement(3),
            Reload = 1,
            Capacity = 4,
            Usage = 2,
            AreaType = AreaItem.AreaTypes.Line,
            AreaRange = 3,
            EphemeralItemProperties = new EphemeralAreaProperties() { CurrentMagazine = 4 },
        };

        flamePistol.Illustration = FlamePistolIllustration;
        return flamePistol;
    }
}

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
/// stores properties for area items
/// </summary>
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