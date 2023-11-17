using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Mods.Weapons.StarfinderWeapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.Roller;

namespace Dawnsbury.Mods.Classes.StarfinderSoldier
{
    public class SoldierClassFeats
    {
        public static Feat CreatePinDown()
        {
            return new SoldierFeat("Pin-Down", 1, "",
            "Requirements Your last action was an attack with an area weapon. Select one creature that was in the area of effect of your prior attack. That creature must make a save against your attack again. This effect deals no damage but can inflict the suppressed condition on a target who previously saved against it."
            , new[] { StarfinderSoldierLoader.SoldierTrait, Trait.ClassFeat }).WithActionCost(1).WithOnCreature((creature) =>
            {
                CombatAction areaAction = null;
                var lastActionWasArea = false;
                creature.AddQEffect(new QEffect()
                {
                    AfterYouTakeAction = (qfSelf, action) =>
                    {
                        Task donothing = new Task(() => { });
                        donothing.Start();
                        if (action.Traits.Any(t => t == StarfinderWeaponsLoader.Area))
                        {
                            lastActionWasArea = true;
                            areaAction = action;
                            return donothing;
                        }
                        if (action.Name != "Primary Target Strike" && (action.ActionCost != 0 || action.Owner.Battle.ActiveCreature != creature))
                        {
                            lastActionWasArea = false;
                            areaAction = null;
                        }
                        return donothing;
                    },
                    StateCheck = (qfSelf) =>
                    {
                        CombatAction pastAction = areaAction;
                        if (lastActionWasArea && pastAction != null && pastAction.Item is AreaItem)
                        {
                            creature.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                            {
                                ProvideMainAction = (qfSelf) =>
                                {
                                    CombatAction pinDownAction = new CombatAction(creature, IllustrationName.TakeCover, "Pin-Down", new[] { StarfinderSoldierLoader.SoldierTrait, }
                                    , "Select one creature that was in the area of effect of your prior attack. That creature must make a save against your attack again. This effect deals no damage but can inflict the suppressed condition on a target who previously saved against it."
                                    , CreatureTarget.Distance(100).WithAdditionalConditionOnTargetCreature((caster, defender) =>
                                    {
                                        if (defender == null)
                                        {
                                            return Usability.NotUsable("no target chosen");
                                        }
                                        if (!pastAction.ChosenTargets.ChosenCreatures.Contains(defender))
                                        {
                                            return Usability.NotUsableOnThisCreature("Target not in area of last attack");
                                        }
                                        if (defender.DeathScheduledForNextStateCheck)
                                        {
                                            return Usability.NotUsableOnThisCreature("Target about to die");
                                        }
                                        return Usability.Usable;
                                    })).WithActionCost(1).WithSavingThrow(new SavingThrow(Defense.Reflex, (creature) =>
                                    {
                                        return StarfinderWeaponsLoader.GetBestAreaDC(creature, pastAction.Item as AreaItem, pastAction.Traits.Contains(StarfinderWeaponsLoader.AutomaticTechnical));
                                    }))
                                    .WithEffectOnEachTarget(async (spell, caster, target, result) =>
                                    {
                                        if (result == CheckResult.Failure || result == CheckResult.CriticalFailure)
                                        {
                                            if (target.QEffects.Any(ef => ef.Name == "Supressed"))
                                            {
                                                target.RemoveAllQEffects(ef => ef.Name == "Supressed");
                                            }
                                            target.AddQEffect(SoldierStatusEffects.GenerateSupressedEffect(caster).WithExpirationAtStartOfSourcesTurn(caster, 1));
                                        }
                                    });

                                    return new ActionPossibility(pinDownAction);
                                }
                            });
                        }
                    }
                });
            });
        }

        public static Feat QuickSwapFeat()
        {
            return new SoldierFeat("Quick-Swap", 1, "",
            "Trigger You are wielding a two-handed weapon and a hostile creature moves adjacent to you. If you are wielding a two handed ranged weapon, you stow your current weapon and draw the first two-handed melee weapon in your inventory. If you are wielding a two handed melee weapon, you instead stow your current weapon and draw the first two handed range weapon in your inventory."
            , new[] { StarfinderSoldierLoader.SoldierTrait, Trait.ClassFeat }).WithOnCreature((creature) =>
            {
                Creature lastCreatureToAct = null;
                int lastActionsLeft= 4;
                bool alreadyAsked = false;
                creature.AddQEffect(new QEffect("Quick-Swap", "Trigger You are wielding a two-handed weapon and a hostile creature moves adjacent to you. If you are wielding a two handed ranged weapon, you stow your current weapon and draw the first two-handed melee weapon in your inventory. If you are wielding a two handed melee weapon, you instead stow your current weapon and draw the first two handed range weapon in your inventory.")
                {
                    StateCheckWithVisibleChanges = async (qfSelf) =>
                    {
                        var activeCreature = qfSelf.Owner.Battle.ActiveCreature;

                        if(lastCreatureToAct?.Actions != null && (lastCreatureToAct != activeCreature || lastCreatureToAct.Actions.ActionsLeft != lastActionsLeft))
                        {
                            alreadyAsked = false;
                        }

                        if(alreadyAsked)
                        {
                            return;
                        }

                        var previousLastCreature = lastCreatureToAct;
                        var previousActionsLeft = lastActionsLeft;

                        lastCreatureToAct= activeCreature;
                        if (activeCreature?.Actions != null)
                        {
                            lastActionsLeft = activeCreature.Actions.ActionsLeft;
                        }

                        if (!creature.Actions.CanTakeReaction() || !creature.HeldItems.Any() || activeCreature == null || activeCreature == creature || !creature.CarriedItems.Any() || !creature.CarriedItems.First().HasTrait(Trait.TwoHanded) || !creature.CarriedItems.Any(i => i.HasTrait(Trait.TwoHanded)))
                        {
                            return;
                        }
                        if(activeCreature.FriendOf(creature))
                        {
                            return;
                        }
                        if (activeCreature?.AnimationData?.LongMovement?.CombatAction == null)
                        {
                            return;
                        }
                        var currentAction = activeCreature.AnimationData.LongMovement.CombatAction;

                        var heldItem = creature.HeldItems.First();
                        if(!heldItem.Traits.Contains(Trait.TwoHanded))
                        {
                            return;
                        }

                        var ranged = heldItem.Traits.Contains(Trait.Ranged);

                        if (currentAction.TilesMoved > 0 && creature.Occupies.Neighbours.Creatures.Contains(activeCreature))
                        {
                            if (creature.OwningFaction == creature.Battle.You)
                            {
                                Sfxs.Play(SfxName.ReactionQuestion);
                            }

                            Item swapToItem = null;

                            foreach (var item in creature.CarriedItems)
                            {
                                if (item.Traits.Contains(Trait.TwoHanded) && item.Traits.Contains(ranged?Trait.Melee:Trait.Ranged))
                                {
                                    swapToItem = item;
                                    break;
                                }
                            }

                            ConfirmationRequest req = new ConfirmationRequest(creature, "Would you like to swap to " + swapToItem.Name + "?", IllustrationName.Reaction, "yes", "no");

                            //req.PassByButtonText = "passing";
                            alreadyAsked = true;
                            var swapResult = (await creature.Battle.SendRequest(req)).ChosenOption;
                            if (swapResult is ConfirmOption)
                            {
                                var swappedInItem = swapToItem;
                                var swappedOutItem = heldItem;
                                creature.HeldItems.Remove(swappedOutItem);
                                creature.CarriedItems.Insert(0,swappedOutItem);
                                creature.CarriedItems.Remove(swappedInItem);
                                creature.HeldItems.Add(swappedInItem);
                                creature.Actions.UseUpReaction();
                            }

                        }
                    }
                });
            });
        }

        public static Feat MenacingLaughter()
        {
            return new SoldierFeat("Menacing Laughter", 2, "",
            "Attempt Intimidation checks to Demoralize each creature within 30 feet who you suppressed this turn."
            , new[] { StarfinderSoldierLoader.SoldierTrait, Trait.ClassFeat }).WithOnCreature((creature) =>
            {
                creature.AddQEffect(new QEffect()
                {
                    ProvideActionIntoPossibilitySection = (qfself, possibilitySection) =>
                    {
                        if (possibilitySection.PossibilitySectionId != PossibilitySectionId.OtherManeuvers)
                        {
                            return null;
                        }
                        var self = qfself.Owner;
                        CombatAction laugh = new CombatAction(self, IllustrationName.HideousLaughter, "Menacing Laughter", new[] { Trait.Auditory, StarfinderSoldierLoader.SoldierTrait }
                        , "Attempt Intimidation checks to Demoralize each creature within 30 feet who you suppressed this turn.", Target.Emanation(6).WithIncludeOnlyIf((target, creature) =>
                        {
                            if (creature.FriendOf(self))
                            {
                                return false;
                            }
                            if (creature.QEffects.Any(q => q.Source == self && q.Name == "Supressed"))
                            {
                                return true;
                            }
                            return false;
                        })).WithActiveRollSpecification(new ActiveRollSpecification(Checks.SkillCheck(Skill.Intimidation), Checks.DefenseDC(Defense.Will)))
                        .WithEffectOnEachTarget(async (spell, caster, target, result) =>
                        {
                            if (result == CheckResult.Success)
                            {
                                target.AddQEffect(QEffect.Frightened(1));
                            }
                            else if (result == CheckResult.CriticalSuccess)
                            {
                                target.AddQEffect(QEffect.Frightened(2));
                            }
                        });
                        return new ActionPossibility(laugh);
                    }
                });
            });
        }

        public static Feat RelentlessEnduranceFeat()
        {
            return new SoldierFeat("Relentless Endurance", 2, "",
            "Trigger You take damage.\r\nFrequency once per encounter\r\nYou come back stronger. You gain 1d8+4 temporary Hit Points that last for the encounter."
            , new[] { StarfinderSoldierLoader.SoldierTrait, Trait.ClassFeat }).WithOnCreature((creature) =>
            {
                bool enduranceUsed = false;

                async Task TakeDamage(QEffect qeffect, int amount, DamageKind kind, CombatAction action, bool critical)
                {
                    if (!enduranceUsed && creature.Actions.CanTakeReaction())
                    {
                        ConfirmationRequest req = new ConfirmationRequest(creature, "Would you like to use Relentless Endurance to gain 1d8+4 temporary HP?", IllustrationName.Reaction, "yes", "no");

                        var enduranceResult = (await creature.Battle.SendRequest(req)).ChosenOption;
                        if (enduranceResult is ConfirmOption)
                        {
                            enduranceUsed = true;
                            creature.AddQEffect(new QEffect("Relentless Endurance used", ""));
                            var healing = DiceFormula.FromText("1d8+4");
                            creature.GainTemporaryHP(healing.RollResult());
                        }
                    }
                }

                creature.AddQEffect(new QEffect("Relentless Endurance", "Trigger You take damage.\r\nFrequency once per encounter\r\nYou come back stronger. You gain 1d8+4 temporary Hit Points that last for the encounter.")
                {
                    StartOfCombat = async (qfself) =>
                    {
                        enduranceUsed = false;
                    },
                    AfterYouTakeDamage = TakeDamage,
                });
            });
        }
    }
}
