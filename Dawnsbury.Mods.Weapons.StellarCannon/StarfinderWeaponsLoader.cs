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
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dawnsbury.Mods.Weapons.StarfinderWeapons;

/// <summary>
/// adds the stellar cannon and its variants from the starfinder 2E field test with some small modifications
/// </summary>
public partial class StarfinderWeaponsLoader
{
    public static Trait Analogue;
    public static Trait Tech;
    public static Trait Unwieldy;
    public static Trait AreaBurst10ft;
    public static Trait AreaBurstTechincal;
    public static Trait Area;
    public static Trait AreaCone;
    public static Trait AreaLine;
    public static Trait Concussive;
    public static Trait Automatic;
    public static Trait AutomaticTechnical;
    public static Trait AreaTechnical;
    public static Trait Gun;
    public static Trait StarfinderGun;
    public static Trait NoAmmoAttack;
    private static ModdedIllustration StellarCannonIllustration = null;
    private static ModdedIllustration ScattergunIllustration = null;
    private static ModdedIllustration FlamePistolIllustration = null;
    private static ModdedIllustration RotolaserIllustration = null;
    private static ModdedIllustration LaserPistolIllustration = null;

    /// <summary>
    /// loads the appropriate mods
    /// </summary>
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        Analogue = ModManager.RegisterTrait("Analogue", new TraitProperties("Analogue", true, "does not rely on digital technology"));
        Tech = ModManager.RegisterTrait("Tech", new TraitProperties("Tech", true, "Incorporates electronics,computer systems, and power sources.", true));
        Unwieldy = ModManager.RegisterTrait("Unwieldy", new TraitProperties("Unwieldy", true, "You can’t use an unwieldy weapon more than once per round and can’t use it to Strike as part of a reaction",true));
        AreaBurst10ft = ModManager.RegisterTrait("AreaBurst10ft", new TraitProperties("Area (Burst 10 ft.)", true, "Weapons with this trait can only fire using the Area Fire action. The DC is equal to 10 + your attack bonus with this weapon using strength or constitution (except expert proficiency is treated as trained).", true));
        AreaCone = ModManager.RegisterTrait("AreaCone", new TraitProperties("Area (Cone)", true, "Weapons with this trait can only fire using the Area Fire action. The DC is equal to 10 + your attack bonus with this weapon using strength or constitution (except expert proficiency is treated as trained).", true));
        AreaLine = ModManager.RegisterTrait("AreaLine", new TraitProperties("Area (Line)", true, "Weapons with this trait can only fire using the Area Fire action. The DC is equal to 10 + your attack bonus with this weapon using strength or constitution (except expert proficiency is treated as trained).", true));
        Concussive = ModManager.RegisterTrait("Concussive", new TraitProperties("Concussive", true, "Use the weaker of the target’s resistance or immunity\r\nto piercing or to bludgeoning."));
        Area = ModManager.RegisterTrait("Area", new TraitProperties("Area", true, "the effect happens in a targeted area", true));
        Automatic = ModManager.RegisterTrait("Automatic", new TraitProperties("Automatic", true, "Can use the 'Automatic Fire' action.The DC is equal to 10 + your attack bonus with this weapon using strength, constitution, or dexterity (except expert proficiency is treated as trained).", true));
        AutomaticTechnical = ModManager.RegisterTrait("AutomaticTechnical", new TraitProperties("AutomaticTechnical", false));
        AreaTechnical = ModManager.RegisterTrait("AreaTech", new TraitProperties("AreaTechnical", false));
        Gun = ModManager.RegisterTrait("Gun", new TraitProperties("Gun", true, "fires projectiles and has a magazine", false));
        StarfinderGun = ModManager.RegisterTrait("StarfinderGun", new TraitProperties("StarfinderGun", false));
        NoAmmoAttack = ModManager.RegisterTrait("NoAmmoAttack", new TraitProperties("NoAmmoAttack", false));

        StellarCannonIllustration = new ModdedIllustration(@"StarfinderWeaponsResources\StellarCannon.png");

        ScattergunIllustration = new ModdedIllustration(@"StarfinderWeaponsResources\Scattergun.png");

        FlamePistolIllustration = new ModdedIllustration(@"StarfinderWeaponsResources\FlamePistol.png");

        RotolaserIllustration = new ModdedIllustration(@"StarfinderWeaponsResources\Rotolaser.png");

        LaserPistolIllustration = new ModdedIllustration(@"StarfinderWeaponsResources\LaserPistol.png");

        ModManager.RegisterActionOnEachCreature(ApplyStarfinderGun);
        ModManager.RegisterActionOnEachCreature(ApplyAreaFire);
        ModManager.RegisterActionOnEachCreature(ApplyAutoFire);
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
        ModManager.RegisterNewItemIntoTheShop("Rotolaser0", CreateRotolaser);
        ModManager.RegisterNewItemIntoTheShop("LaserPistol0", CreateLaserPistol);
    }

    /// <summary>
    /// creates the action for equipped starfinder gun items, including reloading, preventing fire if loaded ammo is too low, and reducing ammo when using strikes.
    /// also renames weapon so it shows current and max ammo
    /// </summary>
    private static Action<Creature> ApplyStarfinderGun = (Creature creature) =>
    {
        creature.AddQEffect(new QEffect()
        {
            StartOfCombat = (qfSelf) =>
            {
                Task gunSetupTask = new Task(() =>
                {
                    creature = qfSelf.Owner;
                    var gunItems = creature.HeldItems.FindAll(heldItem => heldItem is GunItem).Cast<GunItem>();
                    if (gunItems.Count() <= 0)
                    {
                        return;
                    }
                    foreach (var gunItem in gunItems)
                    {
                        SetupGunProperties(qfSelf, gunItem);
                        ((EphemeralAreaProperties)gunItem.EphemeralItemProperties).CurrentMagazine = gunItem.Capacity;
                    }
                });
                gunSetupTask.Start();
                return gunSetupTask;
            },
            StateCheck = (qfSelf) =>
            {
                creature = qfSelf.Owner;
                var gunItems = creature.HeldItems.FindAll(heldItem => heldItem is GunItem).Cast<GunItem>();
                if (gunItems.Count() <= 0)
                {
                    return;
                }
                foreach (var gunItem in gunItems)
                {
                    if (gunItem.Traits.Contains(StarfinderGun))
                    {
                        SetupGunProperties(qfSelf, gunItem);
                    }
                }
            }
        });
    };

    private static void SetupGunProperties(QEffect qfSelf, GunItem gunItem)
    {
        if (gunItem.Traits.Contains(StarfinderGun))
        {
            string plusOne = string.Empty;
            string striking = string.Empty;
            string extraMoniker = string.Empty;
            int newUsage = gunItem.Usage;
            int newCapacity = gunItem.Capacity;
            int newRange = gunItem.WeaponProperties.RangeIncrement;
            if (gunItem.Traits.Contains(Trait.Weapon))
            {
                extraMoniker = ", Commercial";
                newUsage = gunItem.CommercialUsage;
                newCapacity = gunItem.CommercialCapacity;
                newRange = gunItem.CommercialRange;
            }
            if (gunItem.Traits.Contains(Trait.Weapon) && gunItem.ItemModifications.Any(imod => imod.Kind == ItemModificationKind.PlusOne))
            {
                extraMoniker = ", Tactical";
                newUsage = gunItem.TacticalUsage;
                newCapacity = gunItem.TacticalCapacity;
                newRange = gunItem.TacticalRange;
            }
            if (gunItem.Traits.Contains(Trait.Weapon) && gunItem.ItemModifications.Any(imod => imod.Kind == ItemModificationKind.Striking))
            {
                extraMoniker = ", Striking";
                newUsage = gunItem.AdvancedUsage;
                newCapacity = gunItem.CommercialCapacity;
                newRange = gunItem.CommercialRange;
            }
            if (gunItem.Traits.Contains(Trait.Weapon) && gunItem.ItemModifications.Any(imod => imod.Kind == ItemModificationKind.PlusOneStriking))
            {
                extraMoniker = ", Advanced";
                newUsage = gunItem.AdvancedUsage;
                newCapacity = gunItem.AdvancedCapacity;
                newRange = gunItem.AdvancedRange;
            }

            gunItem.Capacity = newCapacity;
            gunItem.Usage = newUsage;
            gunItem.WeaponProperties = gunItem.WeaponProperties.WithRangeIncrement(newRange);
            gunItem.Name = gunItem.BaseGunItemName + extraMoniker + " [" + ((EphemeralAreaProperties)gunItem.EphemeralItemProperties).CurrentMagazine + "/" + gunItem.Capacity + "]";
            GenerateStarfinderGunActions(qfSelf.Owner, gunItem);
        }
    }

    /// <summary>
    /// creates the area fire actions for equipped area weapons and stops usage of area fire if too low on loaded ammo
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
                    if (areaItem.Traits.Contains(AreaTechnical))
                    {
                        GenerateAreaFireActions(qfSelf.Owner, areaItem);
                    }
                }
            },

        });
    };

    /// <summary>
    /// creates the area fire actions for equipped area weapons and stops usage if too low on loaded ammo
    /// </summary>
    private static Action<Creature> ApplyAutoFire = (creature) =>
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
                    if (areaItem.Traits.Contains(Automatic))
                    {
                        GenerateAutoFireActions(qfSelf.Owner, areaItem);
                    }
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
                if(cAction.Item?.Traits.Contains(Concussive) != true)
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
    /// generates the actions needed for starfinder guns
    /// </summary>
    /// <param name="itemOwner">gun ownder</param>
    /// <param name="gunItem">the gun to generate actions for</param>
    private static void GenerateStarfinderGunActions(Creature itemOwner, GunItem gunItem) 
    {
        QEffect starfinderGunEffect = new QEffect(ExpirationCondition.Ephemeral)
        {
            AfterYouTakeAction = (qfSelf, combatAction) =>
            {
                Task fireTask = new Task(() =>
                {
                    if (combatAction.Traits.Contains(Trait.Strike) && combatAction.Item == gunItem && !combatAction.Traits.Contains(NoAmmoAttack))
                    {
                        ((EphemeralAreaProperties)gunItem.EphemeralItemProperties).CurrentMagazine -= gunItem.Usage;

                    };
                });
                fireTask.Start();
                return fireTask;
            },
            PreventTakingAction = (action) =>
            {
                if (action.Item == gunItem)
                {
                    if (((EphemeralAreaProperties)gunItem.EphemeralItemProperties).CurrentMagazine < gunItem.Usage)
                    {
                        return "Not Enough Ammo Loaded.";
                    }
                }
                return null;
            }
        };
        itemOwner.AddQEffect(starfinderGunEffect);

        if (((EphemeralAreaProperties)gunItem.EphemeralItemProperties).CurrentMagazine < gunItem.Capacity)
        {
            QEffect reloadEffect = new QEffect(ExpirationCondition.Ephemeral)
            {
                ProvideMainAction = (qfSelf) =>
                {
                    CombatAction reloadAction = new CombatAction(itemOwner, gunItem.Illustration, "Reload " + gunItem.Name, new[] { Trait.Interact }, "interact to reload", Target.Self()).WithActionCost(gunItem.Reload)
                    .WithEffectOnSelf((self) => ((EphemeralAreaProperties)gunItem.EphemeralItemProperties).CurrentMagazine = gunItem.Capacity);

                    return new ActionPossibility(reloadAction);
                },
            };

            itemOwner.AddQEffect(reloadEffect);
        }
    }

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
                effectRange = areaItem.AreaRange;
                target = Target.Burst(areaItem.WeaponProperties.RangeIncrement, effectRange);
                break;
            case AreaItem.AreaTypes.Cone:
                areaType = "cone";
                effectRange = areaItem.WeaponProperties.RangeIncrement;
                target = Target.Cone(areaItem.WeaponProperties.RangeIncrement);
                break;
            case AreaItem.AreaTypes.Line:
                areaType = "line";
                effectRange = areaItem.WeaponProperties.RangeIncrement;
                target = Target.Line(areaItem.WeaponProperties.RangeIncrement);
                break;
        }

        CombatAction areaFireAction = null;

        QEffect areaFireEffect = new QEffect(ExpirationCondition.Ephemeral)
        {
            PreventTakingAction = (action) =>
            {
                if (action.Item == areaItem)
                {
                    if (((EphemeralAreaProperties)areaItem.EphemeralItemProperties).CurrentMagazine < areaItem.Usage)
                    {
                        return "Not Enough Ammo Loaded.";
                    }
                }
                return null;
            },
            ProvideMainAction = (qfSelf) =>
            {
                var weaponOwner = qfSelf.Owner;

                areaFireAction = new CombatAction(weaponOwner, IllustrationName.BurningJet,
                    areaItem.Name + " Area Fire",
                    new[] { Area, Trait.Attack, Trait.Manipulate },
                    "DC " + GetBestAreaDC(weaponOwner, areaItem) + " Basic Reflex. " +
                     "use an area fire weapon to attack in a " + effectRange * 5 + " ft. " + areaType + " for " + areaItem.WeaponProperties.Damage + " " + areaItem.WeaponProperties.DamageKind.ToString() + " damage.", target)
                    { Item = areaItem }
                    .WithActionCost(2)
                    .WithSavingThrow(new SavingThrow(Defense.Reflex, (creature) =>
                    {
                        return GetBestAreaDC(creature, areaItem);
                    }
                    ))
                    .WithEffectOnSelf((self) =>
                    {
                        ((EphemeralAreaProperties)areaItem.EphemeralItemProperties).CurrentMagazine -= areaItem.Usage;
                        self.DetectionStatus.Undetected = false;
                    })
                    .WithEffectOnEachTarget(async (action, user, target, result) =>
                    {
                        DamageKind kind = areaItem.WeaponProperties.DamageKind;
                        if (areaItem.Traits.Contains(Concussive))
                        {
                            kind = target.WeaknessAndResistance.WhatDamageKindIsBestAgainstMe(new List<DamageKind>(){ DamageKind.Piercing, DamageKind.Bludgeoning});
                        }
                        if (areaItem.Traits.Contains(Trait.VersatileP))
                        {
                            kind = target.WeaknessAndResistance.WhatDamageKindIsBestAgainstMe(new List<DamageKind>() { kind, DamageKind.Piercing });
                        }
                        if(areaItem.Traits.Contains(Trait.VersatileS))
                        {
                            kind = target.WeaknessAndResistance.WhatDamageKindIsBestAgainstMe(new List<DamageKind>() { kind, DamageKind.Slashing });
                        }

                        await CommonSpellEffects.DealBasicDamage(action, user, target, result, areaItem.WeaponProperties.Damage, kind);
                        //only level 4 means no critical specialization effects, yay!
                    });

                return new ActionPossibility(areaFireAction);
            }
        };
        itemOwner.AddQEffect(areaFireEffect);
    }

    /// <summary>
    /// adds actions for auto fire guns
    /// </summary>
    /// <param name="itemOwner">item owner</param>
    /// <param name="areaItem">the item to generate actions for</param>
    private static void GenerateAutoFireActions(Creature itemOwner, AreaItem areaItem)
    {
        int effectRange = areaItem.WeaponProperties.RangeIncrement/2;

        CombatAction autoFireAction = null;

        QEffect autoFireEffect = new QEffect(ExpirationCondition.Ephemeral)
        {
            PreventTakingAction = (action) =>
            {
                if (action.Item == areaItem && ((EphemeralAreaProperties)areaItem.EphemeralItemProperties).CurrentMagazine < areaItem.Capacity / 2 && action.Traits.Contains(AutomaticTechnical))
                {
                    return "You need half your magazine loaded to autofire";
                }
                return null;
            },

            ProvideActionIntoPossibilitySection = (qfSelf, posSelection) =>
            {
                if (posSelection.Possibilities.Any(p => p is ActionPossibility && ((ActionPossibility)p)?.CombatAction.Item != null && ((ActionPossibility)p).CombatAction.Item == areaItem && ((ActionPossibility)p).CombatAction.Traits.Contains(Trait.Strike)))
                {
                    var weaponOwner = qfSelf.Owner;

                    autoFireAction = new CombatAction(weaponOwner, IllustrationName.HailOfSplinters,
                        areaItem.Name + " Automatic Fire",
                        new[] { Area, Trait.Attack, AutomaticTechnical, Trait.Manipulate},
                        "DC " + GetBestAreaDC(weaponOwner, areaItem, true) + " Basic Reflex. " +
                         "use an automatic fire weapon to attack in a " + effectRange * 5 + " ft. cone for " + areaItem.WeaponProperties.Damage + " " + areaItem.WeaponProperties.DamageKind.ToString() + " damage.", Target.Cone(effectRange))
                    { Item = areaItem }
                        .WithActionCost(2)
                        .WithSavingThrow(new SavingThrow(Defense.Reflex, (creature) =>
                        {
                            return GetBestAreaDC(creature, areaItem, true);
                        }
                        ))
                        .WithEffectOnSelf((self) => ((EphemeralAreaProperties)areaItem.EphemeralItemProperties).CurrentMagazine -= areaItem.Capacity / 2)
                        .WithEffectOnEachTarget(async (action, user, target, result) =>
                        {
                            DamageKind kind = areaItem.WeaponProperties.DamageKind;
                            if (areaItem.Traits.Contains(Concussive))
                            {
                                kind = target.WeaknessAndResistance.WhatDamageKindIsBestAgainstMe(new List<DamageKind>() { DamageKind.Piercing, DamageKind.Bludgeoning });
                            }
                            if (areaItem.Traits.Contains(Trait.VersatileP))
                            {
                                kind = target.WeaknessAndResistance.WhatDamageKindIsBestAgainstMe(new List<DamageKind>() { kind, DamageKind.Piercing });
                            }
                            if (areaItem.Traits.Contains(Trait.VersatileS))
                            {
                                kind = target.WeaknessAndResistance.WhatDamageKindIsBestAgainstMe(new List<DamageKind>() { kind, DamageKind.Slashing });
                            }

                            await CommonSpellEffects.DealBasicDamage(action, user, target, result, areaItem.WeaponProperties.Damage, kind);
                            //only level 4 means no critical specialization effects, yay!
                        });

                    return new ActionPossibility(autoFireAction);
                }
                return null;
            }
        };
        itemOwner.AddQEffect(autoFireEffect);
    }



    /// <summary>
    /// determines the str and con (sometimes dex) DCs for the given item held by the given creature with the given trained bonus taking bonuses and penalties into account
    /// </summary>
    /// <param name="creature">creature wielding the area item</param>
    /// <param name="item">the area item being wiedlded</param>
    /// <param name="isAutoFire">Determine if the DC being figured out is autofire or not, as that allows dexterity bonus</param>
    /// <returns>the final best DC</returns>
    public static int GetBestAreaDC(Creature creature, AreaItem item, bool isAutoFire = false)
    {
        var martialTraining = creature.Proficiencies.Get(Trait.Martial);
        var simpleTraining = creature.Proficiencies.Get(Trait.Simple);
        var trainedBonus = creature.Level;

        if (item.Traits.Contains(Trait.Martial))
        {
            //+4 is too good to add for an area reflex save, so nerfing fighters here.
            //This is closer to how starfinder does it since that uses class DC, and nothing will have higher than trained at the first 4 levels... but I wanted weapon training and ability mod to matter
            trainedBonus = martialTraining == Proficiency.Untrained ? 0 : creature.Level + 2;
        }
        if (item.Traits.Contains(Trait.Simple))
        {
            //+4 is too good to add for an area reflex save, so nerfing fighters here.
            //This is closer to how starfinder does it since that uses class DC, and nothing will have higher than trained at the first 4 levels... but I wanted weapon training and ability mod to matter
            trainedBonus = simpleTraining == Proficiency.Untrained ? 0 : creature.Level + 2;
        }

        var conBonuses = new List<Bonus>();
        var strBonuses = new List<Bonus>();
        var dexBonuses = new List<Bonus>();
        foreach (var qEffect in creature.QEffects)
        {
            conBonuses.Add(qEffect.BonusToAllChecksAndDCs?.Invoke(qEffect));
            strBonuses.Add(qEffect.BonusToAllChecksAndDCs?.Invoke(qEffect));
            conBonuses.Add(qEffect.BonusToAbilityBasedChecksRollsAndDCs?.Invoke(qEffect, Ability.Constitution));
            strBonuses.Add(qEffect.BonusToAbilityBasedChecksRollsAndDCs?.Invoke(qEffect, Ability.Strength));
            if(isAutoFire)
            {
                dexBonuses.Add(qEffect.BonusToAllChecksAndDCs?.Invoke(qEffect));
                dexBonuses.Add(qEffect.BonusToAbilityBasedChecksRollsAndDCs?.Invoke(qEffect, Ability.Dexterity));
            }
        }

        var (strBonusTotal, _) = Bonus.CalculateBest(strBonuses, false);
        var (conBonusTotal, _) = Bonus.CalculateBest(conBonuses, false);
        var (dexBonusTotal, _) = Bonus.CalculateBest(dexBonuses, false);

        var strDC = trainedBonus + creature.Abilities.Strength + item.WeaponProperties.ItemBonus + 10 + strBonusTotal;
        var conDC = trainedBonus + creature.Abilities.Constitution + item.WeaponProperties.ItemBonus + 10 + conBonusTotal;
        var dexDC = trainedBonus + creature.Abilities.Dexterity + item.WeaponProperties.ItemBonus + 10 + dexBonusTotal;

        var bestDC = Math.Max(strDC, conDC);
        if(isAutoFire)
        {
            return Math.Max(bestDC, dexDC);
        }
        return bestDC;
    }
}
