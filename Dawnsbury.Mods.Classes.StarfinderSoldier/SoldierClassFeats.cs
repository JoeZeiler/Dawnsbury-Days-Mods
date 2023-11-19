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
using Dawnsbury.Mods.Weapons.StarfinderWeapons;
using System;
using System.Linq;
using System.Threading.Tasks;
using Dawnsbury.Core.Roller;

namespace Dawnsbury.Mods.Classes.StarfinderSoldier
{
    /// <summary>
    /// generates the class feats for soldier
    /// </summary>
    public class SoldierClassFeats
    {
        /// <summary>
        /// creates the Pin Down soldier feat
        /// </summary>
        /// <returns>the feat</returns>
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
                    //after you take an area action, enable the pin-down action
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
                    //provides the pin-down action if it is enabled
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

                                    if(StarfinderSoldierLoader.SuppressedIllustration!=null)
                                    {
                                        pinDownAction.Illustration = StarfinderSoldierLoader.SuppressedIllustration;
                                    }

                                    return new ActionPossibility(pinDownAction);
                                }
                            });
                        }
                    }
                });
            });
        }

        /// <summary>
        /// creates the Quick Swap soldier feat
        /// </summary>
        /// <returns>the feat</returns>
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
                    //when a creature first moves within 5 feet of this creature, find the first two-handed melee (if carrying ranged) or ranged (if carrying melee) weapon in inventory.
                    //ask player if they want to switch to that weapon, if yes, switch the held weapon.
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

        /// <summary>
        /// creates the menacing laughter feat
        /// </summary>
        /// <returns>the feat</returns>
        public static Feat MenacingLaughter()
        {
            return new SoldierFeat("Menacing Laughter", 2, "",
            "Attempt Intimidation checks to Demoralize each creature within 30 feet who you suppressed this turn."
            , new[] { StarfinderSoldierLoader.SoldierTrait, Trait.ClassFeat }).WithOnCreature((creature) =>
            {
                creature.AddQEffect(new QEffect()
                {
                    //provides the menacing laughter action, which will check if the target was supress by the laughing creature before making the intimidation check against them.
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

        /// <summary>
        /// provides the relentless endurance feat
        /// </summary>
        /// <returns>the feat</returns>
        public static Feat RelentlessEnduranceFeat()
        {
            return new SoldierFeat("Relentless Endurance", 2, "",
            "Trigger You take damage.\r\nFrequency once per encounter\r\nYou come back stronger. You gain 1d8+4 temporary Hit Points that last for the encounter."
            , new[] { StarfinderSoldierLoader.SoldierTrait, Trait.ClassFeat }).WithOnCreature((creature) =>
            {
                bool enduranceUsed = false;

                //the task to ask the player if they want to gain temp HP
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
                    //makes sure the feat can be used only once per combat
                    StartOfCombat = async (qfself) =>
                    {
                        enduranceUsed = false;
                    },
                    //provide the take damage task defined earlier
                    AfterYouTakeDamage = TakeDamage,
                });
            });
        }

        /// <summary>
        /// provides the overwhelming assault feat
        /// </summary>
        /// <returns>the feat</returns>
        public static Feat OverwhelmingAssaultFeat()
        {
            return new SoldierFeat("Overwhelming Assault", 4, "",
                "Your multiple attack penalty for attacks against suppressed targets is –4 (–3 with an agile weapon) on your second attack of the turn instead of –5, and –8 (–6 with an agile weapon) on your third or subsequent attack of the turn, instead of –10.",
                new[] { StarfinderSoldierLoader.SoldierTrait, Trait.ClassFeat }).WithOnCreature((creature) =>
                {
                    creature.AddQEffect(new QEffect("Overwhelming Assault",
                        "Your multiple attack penalty for attacks against suppressed targets is –4 (–3 with an agile weapon) on your second attack of the turn instead of –5, and –8 (–6 with an agile weapon) on your third or subsequent attack of the turn, instead of –10.")
                    {
                        //provides an untyped bonus equal to the number of attacks made to simulate lowering the multiple attack penalty
                        BonusToAttackRolls = (qfself,action,target) =>
                        {
                            if(target == null)
                            {
                                return null;
                            }
                            if(target.QEffects.Any(fx=>fx.Name == "Supressed"))
                            {
                                var num = Math.Min(creature.Actions.AttackedThisManyTimesThisTurn, 2);
                                return new Bonus(num, BonusType.Untyped, "Overwhelming Assault Multiple Attack Penalty Reduction");
                            }
                            return null;
                        }
                    });
                });
        }

        /// <summary>
        /// provides the punishing salvo feat
        /// </summary>
        /// <returns>the feat</returns>
        public static Feat PunishingSalvoFeat()
        {
            return new SoldierFeat("Punishing Salvo", 4, "",
                "Requirements Your last action this turn was a primary target Strike.\r\nYou can make a second Strike against your primary target, ignoring the effect of the unwieldy trait that prevents additional attacks. This doesn’t make a new area attack and is instead treated as just a single Strike against the target made using the primary target rules.",
                new[] { StarfinderSoldierLoader.SoldierTrait, Trait.ClassFeat }).WithOnCreature((creature) => 
                {
                    Item lastActionPrimStrikeWeapon = null;
                    Creature strikedCreature = null;
                    creature.AddQEffect(new QEffect("Punishing Salvo", "Requirements Your last action this turn was a primary target Strike.\r\nYou can make a second Strike against your primary target, ignoring the effect of the unwieldy trait that prevents additional attacks. This doesn’t make a new area attack and is instead treated as just a single Strike against the target made using the primary target rules.")
                    {
                        //if the last action you took was to attempt to strike a primary target, enable the punishing salvo action
                        AfterYouTakeHostileAction = (qfself,action) =>
                        {
                            if(action.Name == "Primary Target Strike")
                            {
                                lastActionPrimStrikeWeapon = action.Item;
                                strikedCreature = action?.ChosenTargets?.ChosenCreature;
                            }
                            else
                            {
                                lastActionPrimStrikeWeapon = null;
                                strikedCreature = null;
                            }
                        },
                        //when your turn ends, remove the ability to use the punishing salvo action
                        EndOfYourTurn = async (qfself,creature) =>
                        {
                            lastActionPrimStrikeWeapon = null;
                            strikedCreature = null;
                        },
                        //if the punishing salvo action is enable, provide it
                        StateCheck = (qfself) =>
                        {
                            if(lastActionPrimStrikeWeapon != null && strikedCreature != null)
                            {
                                var areaItem = lastActionPrimStrikeWeapon as AreaItem;
                                var targetCreature = strikedCreature;
                                creature.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                                {
                                    //the punishing salvo action
                                    ProvideMainAction = (qfSelf) =>
                                    {
                                        if (areaItem == null)
                                        {
                                            return null;
                                        }
                                        var strikeTraits = areaItem.Traits;
                                        strikeTraits.Add(StarfinderSoldierLoader.SoldierTrait);
                                        var punishSalvo = new CombatAction(creature, IllustrationName.TrueStrike, "Punishing Salve", strikeTraits.ToArray(),
                                            "make a second Strike against the primary target of your last area attack, ignoring the effect of the unwieldy trait that prevents additional attacks.",
                                            Target.Ranged(areaItem.WeaponProperties.RangeIncrement * 5).WithAdditionalConditionOnTargetCreature((creature, target) =>
                                            {
                                                return target == targetCreature ? Usability.Usable : Usability.NotUsableOnThisCreature("not the primary target of last area attack");
                                            })).WithActionCost(1).WithEffectOnEachTarget(async (spell, caster, target, result) =>
                                            {
                                                await creature.MakePrimaryTargetStrike(targetCreature, areaItem, false);
                                                if (targetCreature.HP <= 0 && !creature.FriendOf(targetCreature))
                                                {
                                                    targetCreature.DeathScheduledForNextStateCheck = true;
                                                    await targetCreature.Battle.GameLoop.StateCheck();
                                                }
                                            });
                                        punishSalvo.Item = areaItem;
                                        return new ActionPossibility(punishSalvo);
                                    }
                                });
                            }
                        }

                    });
                });
        }

        //public static Feat WidenAreaFeat()
        //{
        //    return new SoldierFeat("Widen Area", 4, "",
        //        "If the next action you use is to make an attack with an area weapon that has an area of burst, cone, or line, increase the area of that attack. Add 5 feet to the radius of a burst. Add 5 feet to the length of a cone or line that is normally 15 feet long or smaller, and add 10 feet to the length of a larger cone or line.",
        //        new[] { StarfinderSoldierLoader.SoldierTrait, Trait.ClassFeat, Trait.Manipulate }).WithOnCreature((creature) =>
        //        {
        //            var lastActionWiden = false;
        //            creature.AddQEffect(new QEffect()
        //            {
        //                ProvideMainAction = (qfSelf)=>
        //                {
        //                    var widen = new CombatAction(creature, IllustrationName.KineticistAuraCircle, "Widen Area", new[] { StarfinderSoldierLoader.SoldierTrait, Trait.Manipulate },
        //                        "If the next action you use is to make an attack with an area weapon that has an area of burst, cone, or line, increase the area of that attack. Add 5 feet to the radius of a burst. Add 5 feet to the length of a cone or line that is normally 15 feet long or smaller, and add 10 feet to the length of a larger cone or line."
        //                        , Target.Self()).WithActionCost(1).WithEffectOnSelf(async (self) =>
        //                        {
        //                            lastActionWiden= true;
        //                        });
        //                    return new ActionPossibility(widen);
        //                },
        //                MetamagicProvider = new MetamagicProvider("Widen Area",(action) =>
        //                {
        //                    if (action.HasTrait(StarfinderWeaponsLoader.Area) && lastActionWiden)
        //                    {
        //                        if (action.Target is BurstAreaTarget)
        //                        {
        //                            ((BurstAreaTarget)action.Target).Radius += 5;
        //                        }
        //                        if (action.Target is ConeAreaTarget)
        //                        {
        //                            var cLength = ((ConeAreaTarget)action.Target).ConeLength;
        //                            ((ConeAreaTarget)action.Target).ConeLength += cLength <= 15 ? 5 : 10;
        //                        }
        //                        if (action.Target is LineAreaTarget)
        //                        {
        //                            var cLength = ((LineAreaTarget)action.Target).LineLength;
        //                            ((LineAreaTarget)action.Target).LineLength += cLength <= 15 ? 5 : 10;
        //                        }
        //                    }
        //                    return action;
        //                }),
        //                YouBeginAction = async (qfSelf,action)=>
        //                {
        //                    lastActionWiden = false;
        //                }
        //            });
        //        });
        //}
    }
}
