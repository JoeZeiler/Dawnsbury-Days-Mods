using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Modding;
using Dawnsbury.Core.Mechanics.Core;
using System;
using Dawnsbury.Core.Creatures;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Mods.Weapons.StarfinderWeapons;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Display.Illustrations;
using System.IO;
using Dawnsbury.Mods.StarfinderSharedFunctionality;

namespace Dawnsbury.Mods.Classes.StarfinderSoldier;

/// <summary>
/// creates the starfinder soldier as a class that can be selected in Dawnsbury Days. the Starfinder Weapons mod is required for this.
/// </summary>
public class StarfinderSoldierLoader
{
    public static Trait SoldierTrait;
    public static Trait BombardTechnical;
    public static Trait ArmorStormTechnical;
    public static Feat FearsomeBulwarkFeat;

    /// <summary>
    /// loads the starfinder soldier mod. the Starfinder Weapons mod is a dependency
    /// </summary>
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        SoldierTrait = ModManager.RegisterTrait("Soldier",new TraitProperties("Soldier",true, relevantForShortBlock: true) { IsClassTrait = true});
        BombardTechnical = ModManager.RegisterTrait("BombardTechnical", new TraitProperties("BombardTechnical", false));
        ArmorStormTechnical = ModManager.RegisterTrait("ArmorStormTechnical", new TraitProperties("ArmorStormTechnical", false));

        FearsomeBulwarkFeat = new Feat(FeatName.CustomFeat, "Fearsome Bulwark", "You can use your Constitution modifier instead of your Charisma modifier on Intimidation checks (this does not show up on your character sheet).", new List<Trait>(), new List<Feat>()).WithOnCreature((creature) =>
        {
            creature.AddQEffect(new QEffect("Fearsome Bulwark", "You can use your Constitution modifier instead of your Charisma modifier on Intimidation checks (this does not show up on your character sheet).")
            {
                BonusToSkillChecks = (skill, combatAction, creature) =>
                {
                    if(combatAction.Owner == null)
                    {
                        return new Bonus(0, BonusType.Untyped, string.Empty);
                    }
                    var conChaDif = combatAction.Owner.Abilities.Constitution - combatAction.Owner.Abilities.Charisma;
                    if (skill is Skill.Intimidation && conChaDif > 0)
                    {
                        Bonus skillBonus = new Bonus(conChaDif, BonusType.Untyped, "Fearsome Bulwark");
                        return skillBonus;
                    }
                    return new Bonus(0, BonusType.Untyped, string.Empty);
                }
            });
        }).WithCustomName("Fearsome Bulwark");

        ModManager.AddFeat(GenerateClassSelectionFeat());
        ModManager.AddFeat(SoldierClassFeats.CreatePinDown());
        ModManager.AddFeat(SoldierClassFeats.QuickSwapFeat());
        ModManager.AddFeat(SoldierClassFeats.MenacingLaughter());
        ModManager.AddFeat(SoldierClassFeats.RelentlessEnduranceFeat());
        ModManager.AddFeat(SoldierClassFeats.OverwhelmingAssaultFeat());
        ModManager.AddFeat(SoldierClassFeats.PunishingSalvoFeat());
        //ModManager.AddFeat(SoldierClassFeats.WidenAreaFeat());
    }

    /// <summary>
    /// generates the soldier class selection
    /// </summary>
    /// <returns>the Soldier class selection feat</returns>
    private static Feat GenerateClassSelectionFeat()
    {
        var soldierSelection = new ClassSelectionFeat(FeatName.CustomFeat, "Master of area weapons, heavy armor, and taking punishment.", SoldierTrait,
            new EnforcedAbilityBoost(Ability.Constitution), 10, new[] { Trait.Perception, Trait.Reflex, Trait.Simple, Trait.Martial, Trait.Unarmed, Trait.UnarmoredDefense, Trait.LightArmor, Trait.MediumArmor, Trait.HeavyArmor },
            new[] { Trait.Fortitude, Trait.Will }, 4, "{b}1. Suppressing Fire.{/b} Creatures who fail their save against your area attack from an area weapon become Suppressed until the start of your next turn." +
            "\n\n{b}2. Primary Target.{/b} You may choose a primary target. If they are the closest creature to the area origin point of an area attack, you may also make a Strike against that creature as part of that attack." +
            "\n\n{b}3. Fighting Style.{/b} As a Soldier, you applied yourself to a specific style of combat. Your style determines how you tend to approach combat and how you take advantage of your ability to suppress targets." +
            "\n\n{b}4. Walking Armory.{/b} When determining your Strength threshold for using medium or heavy armor, you can instead choose to use your Constitution modifier." +
            "\n\n{b}At higher levels:{/b}\n{b}Level 3:{/b} Fearsome Bulwark (you can use your Constitution modifier instead of your Charisma modifier on Intimidation checks)", GenerateSoldierSubclasses()).WithCustomName("Soldier").WithOnCreature((creature)=>
            {
                SetupPrimaryTarget(creature);
                SetupSoldierSuppression(creature);
                SetupWalkingArmory(creature);
            }).WithOnSheet(sheet =>
            {
                sheet.AddAtLevel(3, (values) => { values.AddFeat(FearsomeBulwarkFeat, null); });
                sheet.AddSelectionOption(new SingleFeatSelectionOption("level1SoldierFeat", "Soldier feat", 1, (feat) => feat.HasTrait(SoldierTrait) && feat is TrueFeat && ((TrueFeat)feat).Level == 1));
            });
        return soldierSelection;
    }

    /// <summary>
    /// sets up the ability for the creature to use con instead of str for meeting armor strength requirements
    /// </summary>
    /// <param name="creature">the creature to add walking armory to</param>
    private static void SetupWalkingArmory(Creature creature)
    {
        creature.AddQEffect(new QEffect("Walking Armory","When determining your Strength threshold for using medium or heavy armor, you can instead choose to use your Constitution modifier.")
        {
            StateCheck = (qfSelf)=>
            {
                creature.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                {
                    ProvidesArmor = DetermineNewConArmor(creature),
                });
            }
        });
    }

    /// <summary>
    /// sets up the creature with the ability to select a primary target, and have them fire at the primary target if it is closest to origin of area fire after using area fire.
    /// </summary>
    /// <param name="creature">creature to add primary targeting to</param>
    private static void SetupPrimaryTarget(Creature creature)
    {
        Creature actionOwner = creature;
        Creature mainTarget = null;
        creature.AddQEffect(
        new QEffect("Primary Target", "You may choose a primary target. If they are the closest creature to the area origin point of an area attack, you may also make a Strike against that creature as part of that attack.")
        {
            //provides the free action to seelct a primary target
            StateCheck = (qfSelf) =>
            {
                var carriedItems = actionOwner.HeldItems;
                if (carriedItems.Any(i => i is AreaItem))
                {
                    creature.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                    {
                        ProvideMainAction = (qfSelf) =>
                        {
                            Creature thisCreature = actionOwner;
                            var addedText = (mainTarget != null ? "\n[currently: " + mainTarget.Name + "]" : string.Empty);
                            CombatAction SetPrimaryTarget = new CombatAction(actionOwner, IllustrationName.TrueStrike, "Select Primary Target" + addedText, new Trait[] { Trait.AlwaysHits, Trait.IsNotHostile, Trait.DoesNotBreakStealth },
                                 "Select a foe. As long as they are the closest creature to the center of a burst area fire or the closest creature to you when using a a cone or line area fire, you will also attempt to Strike that creature.",
                                 Target.RangedCreature(100).WithAdditionalConditionOnTargetCreature((caster, target) => target.FriendOf(caster) ? Usability.NotUsableOnThisCreature("ally") : Usability.Usable))
                                 .WithActionCost(0)
                                .WithEffectOnEachTarget(async (spell, caster, target, result) =>
                                {
                                    mainTarget = target;
                                });
                            return new ActionPossibility(SetPrimaryTarget);
                        }
                    });
                }

            },
            //after you take an area attack action, fire at the primary target
            AfterYouTakeAction = async (qfSelf, action) =>
            {
                if (!action.Traits.Contains(StarfinderWeaponsLoader.Area) || !(action.Item is AreaItem))
                {
                    return;
                }

                var aItem = action.Item as AreaItem;
                var tiles = new List<Tile>
                {
                    action.Owner.Occupies
                };

                if (aItem.AreaType == AreaItem.AreaTypes.Burst)
                {
                    tiles.Clear();
                    tiles = DetermineCenterTiles(action.ChosenTargets.ChosenTiles);
                }
                var chosen = action.ChosenTargets.ChosenCreatures;
                if (!chosen.Any())
                {
                    return;
                }
                List<Creature> closest = new List<Creature>();
                int closestDistance = 999;
                //deterimines which creatures are closest to the origin of the attack
                foreach (var creature in chosen)
                {
                    var bestDist = 999;
                    foreach (var tile in tiles)
                    {
                        var centerDist = creature.DistanceTo(tile);
                        if (centerDist < bestDist)
                        {
                            bestDist = centerDist;
                        }
                    }
                    if (bestDist > closestDistance)
                    {
                        continue;
                    }
                    if (bestDist == closestDistance)
                    {
                        closest.Add(creature);
                        continue;
                    }
                    if (bestDist < closestDistance)
                    {
                        closest.Clear();
                        closest.Add(creature);
                        closestDistance = bestDist;
                    }
                }
                if (!closest.Any())
                {
                    return;
                }
                var areaItem = action.Item;

                if (mainTarget != null && closest.Contains(mainTarget) && !mainTarget.DeathScheduledForNextStateCheck)
                {
                    var attackAmmount = action.Owner.Actions.AttackedThisManyTimesThisTurn;
                    await actionOwner.MakePrimaryTargetStrike(mainTarget, areaItem, true, attackAmmount);
                    action.Owner.Actions.AttackedThisManyTimesThisTurn = attackAmmount;
                    if (mainTarget.HP <= 0 && !actionOwner.FriendOf(mainTarget))
                    {
                        mainTarget.DeathScheduledForNextStateCheck = true;
                        await mainTarget.Battle.GameLoop.StateCheck();
                    }
                }
            },
        });
    }

    /// <summary>
    /// algorithm to determine what the center tiles in a burst is
    /// </summary>
    /// <param name="tiles">the list of tiles to determine the center of</param>
    /// <returns>the list of tiles that make up the center of the burst</returns>
    private static List<Tile> DetermineCenterTiles(List<Tile> tiles)
    {
        int shortestLongestDistance = 999;
        List<Tile> CenterTiles = new List<Tile>();
        if(tiles.Count == 1)
        {
            CenterTiles.Add(tiles[0]);
            return CenterTiles;
        }
        foreach (var tile in tiles)
        {
            int longestDistance = 0;
            foreach(var innerTile in tiles)
            {
                if(innerTile ==  tile)
                {
                    continue;
                }
                var dist = tile.DistanceTo(innerTile);
                if(dist > longestDistance)
                {
                    longestDistance = dist;
                }
            }
            if(longestDistance == shortestLongestDistance)
            {
                CenterTiles.Add(tile);
                continue;
            }
            if(longestDistance > shortestLongestDistance)
            {
                continue;
            }
            if(longestDistance < shortestLongestDistance)
            {
                CenterTiles.Clear();
                CenterTiles.Add(tile);
                shortestLongestDistance = longestDistance;
            }
        }
        return CenterTiles;
    }

    /// <summary>
    /// sets up the ability for the soldier to suppress targets in its area fire.
    /// </summary>
    /// <param name="creature">the creature to add the ability to supress to.</param>
    private static void SetupSoldierSuppression(Creature creature)
    {
        creature.AddQEffect(new QEffect("Suppressing Fire", "Creatures in the affected area who fail their save against your area attack become Suppressed until the start of your next turn.")
        {
            AfterYouTakeHostileAction = (qfSelf, action) =>
            {
                if(!action.Traits.Contains(StarfinderWeaponsLoader.Area))
                {
                    return;
                }
                var chosen = action.ChosenTargets;
                foreach (Creature chosenTarget in chosen.ChosenCreatures)
                {
                    if(action.Owner.FriendOf(chosenTarget))
                    {
                        continue;
                    }

                    if (!chosen.CheckResults.ContainsKey(chosenTarget))
                    {
                        continue;
                    }
                    var result = chosen.CheckResults[chosenTarget];
                    if (result == CheckResult.Failure || result == CheckResult.CriticalFailure || (result == CheckResult.Success && action.Owner.Traits.Contains(BombardTechnical)))
                    {
                        if (chosenTarget.QEffects.Any(ef => ef.Name == "Suppressed"))
                        {
                            chosenTarget.RemoveAllQEffects(ef => ef.Name == "Suppressed");
                        }
                        chosenTarget.AddQEffect(StatusEffects.GenerateSupressedEffect(action.Owner).WithExpirationAtStartOfSourcesTurn(action.Owner, 1));
                    }
                }
            }
        });
    }

    /// <summary>
    /// generates two of the soldier subclasses
    /// </summary>
    /// <returns>the list of the two subclasses</returns>
    private static List<Feat> GenerateSoldierSubclasses()
    {
        return new List<Feat>()
        {
            new SoldierFightingStyleFeat(
            "Armor Storm",
            "Your armor is like an extension of your skin (or other appropriate surface layer), and you're able to leverage it alongside the heavy weapons you employ. Foes you suppress quickly stumble while attempting to overcome your durability, granting you an edge in absorbing their incoming firepower. You likely move to the forefront and try to focus your enemy's attention on yourself.",
            "You never count as being in the area of a ranged weapon you've made an attack with. In addition, you gain resistance equal to half your level (minimum 1) against attacks made from suppressed targets.",
            new List<Trait>(),null).WithOnCreature((sheet, creature) =>
            {
                creature.Traits.Add(ArmorStormTechnical);
                creature.AddQEffect(new QEffect("Armor Storm","You never count as being in the area of a ranged weapon you've made an attack with. In addition, you gain resistance equal to half your level (minimum 1) against attacks made from suppressed targets.")
                {
                    AdjustSavingThrowResult = (qfSelf, action, result) =>
                    {
                        if (action.Traits.Contains(StarfinderWeaponsLoader.Area) && action.Owner == creature)
                        {
                            return CheckResult.CriticalSuccess;
                        }
                        return result;
                    },
                    YouAreDealtDamage = (qfSelf, attacker, dStuff, self) =>
                    {
                        Task<DamageModification> damageModTask = new Task<DamageModification>(() =>
                        {
                            if (attacker.QEffects.Any((qft)=>qft.Name == "Suppressed"))
                            {
                                return new ReduceDamageModification(Math.Max(Math.Max(self.Level / 2, 1) - self.WeaknessAndResistance.Resistances.FirstOrDefault(r => r.DamageKind == dStuff.Kind, new Resistance(DamageKind.Chaotic, 0)).Value, 0), "Resistance equal to 1/2 level (minimum 1) against suppressed targets");
                            }
                            else
                            {
                                return null;
                            }
                        });
                        damageModTask.Start();
                        return damageModTask;
                    }
                });
            }),
             new SoldierFightingStyleFeat(
            "Bombard",
            "There's nothing like a reliable heavy gun (or maybe several different types of heavy guns) to get you through the tough times of adventuring in space. You've come to terms with the fact that your weapons might sometimes hit your allies but work to minimize such instances of unintentional \"friendly fire.\" In fact, you've honed your skill with heavy weapons so much that all but the most indirect of strikes causes your opponents to duck down or force them to adapt to the havoc you unleash.",
            "When you attack with an area weapon, you adjust the shot to allow allies to better avoid it. Your allies are not affected by your area attacks. In addition, enemies who succeed (but not critically succeed) their save against an area attack you make are still Suppressed until the start of your next turn.",
            new List<Trait>(),null).WithOnCreature((sheet, creature) =>
            {
                creature.Traits.Add(BombardTechnical);
                creature.AddQEffect(new QEffect("Bombard","When you attack with an area weapon, you adjust the shot to allow allies to better avoid it. Your allies are not affected by your area attacks. In addition, enemies who succeed (but not critically succeed) their save against an area attack you make are still Suppressed until the start of your next turn.")
                {
                    StartOfCombat = (qfSelf) =>
                    {
                        Task makeAlliesImmuneTask = new Task(() =>
                        {
                            var battleCreatures = creature.Battle.AllCreatures;
                            foreach (var c in battleCreatures)
                            {
                                if (creature.FriendOfAndNotSelf(c))
                                {
                                    c.AddQEffect(new QEffect()
                                    {
                                        AdjustSavingThrowResult = (qfSelf, action, result) =>
                                        {
                                            if (action.Traits.Contains(StarfinderWeaponsLoader.Area) && action.Owner == creature)
                                            {
                                                return CheckResult.CriticalSuccess;
                                            }
                                            return result;
                                        }
                                    });
                                }
                            }
                        });
                        makeAlliesImmuneTask.Start();
                        return makeAlliesImmuneTask;
                    }
                });
            })
        };
    }

    /// <summary>
    /// if con is high enough, change armor to not require strength so soldiers with too low stength, but high enough con can use the armor
    /// </summary>
    /// <param name="wearer">wearer of the armor</param>
    /// <returns>the new armor</returns>
    private static Item DetermineNewConArmor(Creature wearer)
    {
        var oldItem = wearer.BaseArmor;
        if(oldItem == null || oldItem.ArmorProperties == null)
        {
            return oldItem;
        }
        if (oldItem.ArmorProperties.Strength <= wearer.Abilities.Constitution)
        {
            ArmorProperties oldArmorProps = oldItem.ArmorProperties;
            var newItem = new Item(oldItem.ItemName, (Illustration)IllustrationName.QuestionMark, oldItem.Name, oldItem.Price, oldItem.Price, oldItem.Traits.ToArray());
            newItem.Illustration = oldItem.Illustration;
            newItem.ArmorProperties = new ArmorProperties(oldArmorProps.ACBonus, oldArmorProps.DexCap, oldArmorProps.CheckPenalty, oldArmorProps.SpeedPenalty, 8);
            return newItem;
        }
        else
        {
            return oldItem;
        }
    }
}

/// <summary>
/// extension methods for soldier
/// </summary>
public static class SoldierCreatureExtensions
{
    /// <summary>
    /// more specialized version of the MakeStrike function
    /// </summary>
    /// <param name="actionOwnder">owner of the strike</param>
    /// <param name="targetCreature">target of the strike</param>
    /// <param name="weapon">weapon being used for the strike</param>
    /// <param name="mapCount">multiple attack penalty multiplier to use, -1 for use current</param>
    /// <returns>the result of the roll to strike.</returns>
    public static async Task<CheckResult> MakePrimaryTargetStrike(this Creature actionOwnder, Creature targetCreature, Item weapon,bool free, int mapCount = -1)
    {
        CombatAction meleeStrike = actionOwnder.CreateStrike(weapon, mapCount).WithActionCost(0);
        meleeStrike.Name = "Primary Target Strike";
        meleeStrike.ChosenTargets = new ChosenTargets
        {
            ChosenCreature = targetCreature
        };
        if (free) { meleeStrike.Traits.Add(StarfinderWeaponsLoader.NoAmmoAttack); }
        await meleeStrike.AllExecute();
        return meleeStrike.CheckResult;
    }
}