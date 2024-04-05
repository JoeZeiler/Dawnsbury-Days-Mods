using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics;
using System.Linq;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Microsoft.Xna.Framework.Graphics;
using System;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Possibilities;
using System.Reflection;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.Creatures;
using System.Collections.Generic;
using Dawnsbury.Core.Intelligence;

namespace Dawnsbury.Mods.Classes.Starfinder.Envoy
{
    public class EnvoyFeats
    {
        public static readonly string DIVERSE_SCHEMES = "Diverse Schemes";

        public static Feat CreatePardonme()
        {
            return new EnvoyFeat("Pardon Me!", 1, "Your Charismatic self can catch even enemy combatants off-guard",
                "You can attempt to Tumble Through using Deception instead of Acrobatics. If you succeed, the enemy whose space you passed through is suppressed until the end of its next turn",
                new[] { Trait.Mental, StarfinderEnvoyLoader.EnvoyTrait, Trait.ClassFeat }).WithOnCreature((sheet, creature) =>
            {
                creature.AddQEffect(new QEffect()
                {
                    ProvideMainAction = (qfself) =>
                    {
                        var pardonQfx = new QEffect("Pardon Me! Active", "you will use Deception for tumble through and cause suppressed on success.", ExpirationCondition.Never, creature, IllustrationName.CreateADiversion)
                        {
                            BonusToSkillChecks = (usedSkill,usedAction,skillTarget) =>
                            {
                                if(usedAction.Name == "Tumble Through")
                                {
                                    var efx = usedAction.EffectOnOneTarget;

                                    usedAction.WithActiveRollSpecification(new ActiveRollSpecification(Checks.SkillCheck(Skill.Deception), Checks.DefenseDC(Defense.Reflex))).WithEffectOnEachTarget(async (spell, caster, target, checkResult) =>
                                    {
                                        await efx(spell, caster,target,checkResult);
                                        if(checkResult is CheckResult.Success || checkResult is CheckResult.CriticalSuccess)
                                        {
                                            target.AddQEffect(StarfinderSharedFunctionality.StatusEffects.GenerateSupressedEffect(caster));
                                        }
                                    });
                                }
                                return null;
                            }
                        };

                        CombatAction SetPrimaryTarget = new CombatAction(creature, IllustrationName.CreateADiversion, "Toggle use Deception for Tumble Through", new Trait[] {},
                                 "Toggles using Deception for tumble through and possibly causing suppressed.",
                                 Target.Self())
                                 .WithActionCost(0).WithEffectOnSelf((self) =>
                                {
                                    if (creature.QEffects.Any(qf => qf.Name == pardonQfx.Name))
                                    {
                                        self.RemoveAllQEffects(qf => qf.Name == pardonQfx.Name);
                                    }
                                    else
                                    {
                                        self.AddQEffect(pardonQfx);
                                    }
                                });
                        return new ActionPossibility(SetPrimaryTarget);
                    }
                });
            });
        }

        public static Feat CreateQuip()
        {
            
            return new EnvoyFeat("Quip {icon:Reaction}", 1, "you make a witty quip", "{b}Trigger{/b} A creature within 30 feat is damaged by an ally's strike.\nAttempt to Demoralize the triggering creature."
                , new[] { Trait.Concentrate, Trait.Emotion, StarfinderEnvoyLoader.EnvoyTrait, Trait.Fear, Trait.Mental, Trait.ClassFeat }).WithOnCreature((sheet, creature) =>
                {
                    var demoralizeAction = CommonCombatActions.Demoralize(creature);
                    QEffect createTriggerQuip()
                    {
                        return new QEffect(ExpirationCondition.Ephemeral)
                        {
                            AfterYouTakeDamage = async (qeffect, amount, kind, action, critical) =>
                            {
                                if (action.Owner.FriendOfAndNotSelf(creature) && !creature.Actions.IsReactionUsedUp && qeffect.Owner.DistanceTo(creature) <= 6 && !qeffect.Owner.QEffects.Any(qf => qf.PreventTargetingBy != null && qf.PreventTargetingBy(demoralizeAction) == "immunity") && action.HasTrait(Trait.Strike))
                                {
                                    ConfirmationRequest req = new ConfirmationRequest(creature, action.Owner.Name + " succesfully damaged " + qeffect.Owner.Name + ". Would you like to attempt to demoralize them?", IllustrationName.Reaction, "Yes", "Pass");
                                    var quipResult = (await creature.Battle.SendRequest(req)).ChosenOption;
                                    if (quipResult is ConfirmOption)
                                    {
                                        creature.Actions.UseUpReaction();
                                        demoralizeAction.ChosenTargets.ChosenCreature = qeffect.Owner;
                                        await demoralizeAction.AllExecute();
                                    }
                                }
                            }
                        };
                    }

                    creature.AddQEffect(new QEffect("Quip {icon:Reaction}", "{b}Trigger{/b} A creature within 30 feat is damaged by an ally's strike.\nAttempt to Demoralize the triggering creature.")
                    {
                        StateCheck = (qfSelf) =>
                        {
                            if (!creature.Actions.IsReactionUsedUp)
                            {
                                foreach (var c in creature.Battle.AllCreatures.Where(innerC => !innerC.FriendOf(creature)))
                                {
                                    c.AddQEffect(createTriggerQuip());
                                }
                            }
                        }
                    });
                });
        }

        public static Feat CreateDiverseSchemes()
        {
            return new EnvoyFeat(DIVERSE_SCHEMES, 1, "You are great at splitting focus", "you can maintain two assets at once", new[] { StarfinderEnvoyLoader.EnvoyTrait, Trait.ClassFeat }).WithOnCreature((sheet, creature) =>
                {
                    creature.AddQEffect(new QEffect("Diverse Schemes", "You can mantain two assets"));
                });
        }

        public static Feat CreateWatchOut()
        {
            return new EnvoyFeat("Watch Out! {icon:Reaction}", 1, "you got your ally's back.", "{b}Trigger{/b} an ally within 60 feet is targeted with an attack and you can see the ally and attacker.\nGive the ally a +2 circumstance bonus to AC against the attack.", new[] { Trait.Concentrate, StarfinderEnvoyLoader.EnvoyTrait, Trait.ClassFeat })
                .WithOnCreature((sheet, creature) =>
                {

                    creature.AddQEffect(new QEffect("Watch Out! {icon:Reaction}", "{b}Trigger{/b} an ally within 60 feet is targeted with an attack and you can see the ally and attacker.\nGive the ally a +2 circumstance bonus to AC against the attack.")
                    {
                        StartOfCombat = async (qfself) =>
                        {
                            if (creature.Battle == null)
                            {
                                return;
                            }
                            foreach (var c in creature.Battle.AllCreatures.Where(c => creature.FriendOfAndNotSelf(c)))
                            {
                                c.AddQEffect(new QEffect()
                                {
                                    YouAreTargeted = async (qfself, combatAction) =>
                                    {
                                        if (combatAction.HasTrait(Trait.Attack) && !creature.Actions.IsReactionUsedUp)
                                        {
                                            ConfirmationRequest req = new ConfirmationRequest(creature, combatAction.Owner.Name + " is targeting " + c.Name + " with in attack. Would you like to warn them?", IllustrationName.Reaction, "Yes", "Pass");
                                            var quipResult = (await creature.Battle.SendRequest(req)).ChosenOption;
                                            if (quipResult is ConfirmOption)
                                            {
                                                c.AddQEffect(new QEffect(ExpirationCondition.EphemeralAtEndOfImmediateAction)
                                                {
                                                    BonusToDefenses = (qfself, defendedAction, defenseType) =>
                                                    {
                                                        if (defendedAction == null)
                                                        {
                                                            return null;
                                                        }
                                                        if (defendedAction == combatAction && defenseType == Defense.AC)
                                                        {
                                                            creature.Actions.UseUpReaction();
                                                            return new Bonus(2, BonusType.Circumstance, "Watch Out! bonus from " + c.Name);
                                                        }
                                                        return null;
                                                    }
                                                });
                                            }
                                        }
                                    }
                                });
                            }
                        }
                    });

                    
                });
        }

        public static Feat CreateChangeOfPlans()
        {
            return new EnvoyFeat("Change of Plans! {icon:Reaction}", 2, "Your quick thinking allows you to adjust your battle plans.", "{b}Trigger{/b} The target of your Get 'Em! is reduced to 0 Hit Points.\nUse your Get 'Em! on a new target, following normal targeting restrictions. You don't get your Lead by Example bonus against the new target.", new[] { StarfinderEnvoyLoader.EnvoyTrait, Trait.ClassFeat })
                .WithOnCreature((sheet, creature) =>
                {

                    creature.AddQEffect(new QEffect("Change of Plans! {icon:Reaction}", "{b}Trigger{/b} The target of your Get 'Em! is reduced to 0 Hit Points.\nUse your Get 'Em! on a new target, following normal targeting restrictions. You don't get your Lead by Example bonus against the new target.")
                    {
                        StartOfCombat = async (qfself) =>
                        {
                            if (creature.Battle == null)
                            {
                                return;
                            }
                            foreach (var c in creature.Battle.AllCreatures.Where(c => !creature.FriendOf(c)))
                            {
                                c.AddQEffect(new QEffect()
                                {
                                    YouAreDealtLethalDamage = async (qfself,attacker, damageStats, target) =>
                                    {
                                        if(target.QEffects.Any(qf=>qf.Name == StarfinderEnvoyLoader.GET_ME_NAME && qf.Source == creature) && !creature.Actions.IsReactionUsedUp)
                                        {
                                            List<Option> creatureOptions = new List<Option>();

                                            foreach (var enemyC in creature.Battle.AllCreatures.Where(innerC => !creature.FriendOf(innerC) && creature.DistanceTo(innerC) <= 12 && !innerC.DeathScheduledForNextStateCheck && innerC != target))
                                            {

                                                creatureOptions.Add(new CreatureOption(enemyC, enemyC.Name, async () => { }, AIConstants.NEVER, true));
                                            }
                                            if(creatureOptions.Count == 0)
                                            {
                                                return null;
                                            }

                                            ConfirmationRequest req = new ConfirmationRequest(creature, target.Name + ", the target of your Get 'Em!, has died. Would you like to use Get 'Em! on another creature?", IllustrationName.Reaction, "Yes", "Pass");
                                            var changePlansResult = (await creature.Battle.SendRequest(req)).ChosenOption;
                                            if (changePlansResult is ConfirmOption)
                                            {
                                                creatureOptions.Add(new CancelOption(false));

                                                AdvancedRequest chooseGetem = new AdvancedRequest(creature, "select new Get 'Em! target", creatureOptions);

                                                var chosenCreature = (await creature.Battle.SendRequest(chooseGetem)).ChosenOption;

                                                if (chosenCreature is CreatureOption)
                                                {
                                                    creature.Actions.UseUpReaction();
                                                    var targetedCreature = ((CreatureOption)chosenCreature).Creature;

                                                    targetedCreature.AddQEffect(new QEffect(StarfinderEnvoyLoader.GET_ME_NAME, "target of a Get 'Em! action. -1 circumstance penalty to AC", ExpirationCondition.ExpiresAtStartOfSourcesTurn, creature, IllustrationName.HuntPrey)
                                                    {
                                                        BonusToDefenses = (qfself, action, dfence) =>
                                                        {
                                                            if (dfence == Defense.AC)
                                                            {
                                                                return new Bonus(-1, BonusType.Circumstance, "Get 'Em", false);
                                                            }
                                                            return null;
                                                        }
                                                    });
                                                }
                                            }
                                        }

                                        return null;
                                    }
                                });
                            }
                        }
                    });


                });
        }

        public static Feat CreateGetInThere()
        {
            return new EnvoyFeat("Get In There! {icon:Action}", 2, "You yell at your buddies so they can get a move on.",
                "Until your next turn, you and your allies gain a +5-foot status bonus to Speed.\n{b}Lead by Example{/b} if you step or stride before your turn ends, each ally can immediately Step or Stride up to half their Speed (rounded down to nearest 5-feet) as a free action",
                new[] { Trait.Concentrate, StarfinderEnvoyLoader.Directive, StarfinderEnvoyLoader.EnvoyTrait, Trait.Visual, Trait.ClassFeat })
                .WithOnCreature((sheet, creature) =>
                {
                    creature.AddQEffect(new QEffect()
                    {
                        ProvideActionIntoPossibilitySection = (qfself,possSection) =>
                        {
                            if(possSection.Name == StarfinderEnvoyLoader.DIRECTIVES_POSSIBILITY_SECTION_NAME)
                            {

                                CombatAction GetInThereAction = new CombatAction(creature, IllustrationName.FleetStep, "Get In There!", new[] { Trait.Concentrate, StarfinderEnvoyLoader.Directive, StarfinderEnvoyLoader.EnvoyTrait, Trait.Visual }, "Until your next turn, you and your allies gain a +5-foot status bonus to Speed.\n{b}Lead by Example{/b} if you step or stride before your turn ends, each ally can immediately Step or Stride up to half their Speed (rounded down to nearest 5-feet) as a free action", Target.Self())
                                .WithEffectOnSelf((thisCreature) =>
                                {
                                    foreach(var friendly in thisCreature.Battle.AllCreatures.Where(c=>c.FriendOf(thisCreature)))
                                    {
                                        friendly.AddQEffect(new QEffect("Getting in there", "+5 ft status bonus to speed from your friendly neighborhood envoy", ExpirationCondition.ExpiresAtStartOfSourcesTurn, thisCreature, IllustrationName.FleetStep)
                                        {
                                            BonusToAllSpeeds = (qfSelf) =>
                                            {
                                                return new Bonus(1, BonusType.Status, "Get In There!");
                                            }
                                        });
                                    }
                                    thisCreature.AddQEffect(new QEffect(ExpirationCondition.ExpiresAtEndOfYourTurn)
                                    {
                                        AfterYouTakeAction = async (qfSelf,takenAction) =>
                                        {
                                            if(takenAction.TilesMoved > 0)
                                            {
                                                foreach (var friendly in thisCreature.Battle.AllCreatures.Where(c => c.FriendOfAndNotSelf(thisCreature)))
                                                {
                                                    await friendly.StrideAsync("Get In There!: Stride half your speed or Step", true, allowCancel: true, maximumHalfSpeed: true);
                                                }
                                            }
                                        }
                                    });
                                });

                                return new ActionPossibility(GetInThereAction);
                            }
                            return null;
                        }
                    });
                });
        }


        public static Feat CreateSearchHighAndLow()
        {
            return new EnvoyFeat("Search High and Low {icon:Action}", 2, "You make cool hand gestures to indicate that your allies should keep their eyes open.",
                "Until your next turn, You and your allies get a +2 circumstance bonus to Perception checks to Seek.\n{b}Lead by Example{/b} if you Seek before your turn ends, each ally is hasted but may only use the extra action to seek.",
                new[] { Trait.Concentrate, StarfinderEnvoyLoader.Directive, StarfinderEnvoyLoader.EnvoyTrait, Trait.Visual, Trait.ClassFeat })
                .WithOnCreature((sheet, creature) =>
                {
                    creature.AddQEffect(new QEffect()
                    {
                        ProvideActionIntoPossibilitySection = (qfself, possSection) =>
                        {
                            if (possSection.Name == StarfinderEnvoyLoader.DIRECTIVES_POSSIBILITY_SECTION_NAME)
                            {

                                CombatAction GetInThereAction = new CombatAction(creature, IllustrationName.Seek, "Search High and Low", new[] { Trait.Concentrate, StarfinderEnvoyLoader.Directive, StarfinderEnvoyLoader.EnvoyTrait, Trait.Visual }, "Until your next turn, You and your allies get a +2 circumstance bonus to Perception checks to Seek.\n{b}Lead by Example{/b} if you Seek before your turn ends, each ally is hasted but may only use the extra action to seek.", Target.Self())
                                .WithEffectOnSelf((thisCreature) =>
                                {
                                    foreach (var friendly in thisCreature.Battle.AllCreatures.Where(c => c.FriendOf(thisCreature)))
                                    {
                                        friendly.AddQEffect(new QEffect("Search High and Low", "+2 circumstance bonus to Perception checks from your fellow envoy", ExpirationCondition.ExpiresAtStartOfSourcesTurn, thisCreature, IllustrationName.Seek)
                                        {                                            
                                            BonusToAttackRolls = (qfself, combatAction, targetCreature) =>
                                            {
                                                if(combatAction.Name.Contains("Seek ("))
                                                {
                                                    return new Bonus(2, BonusType.Circumstance, "Search High and Low");
                                                }
                                                return null;
                                            }
                                        });
                                    }
                                    thisCreature.AddQEffect(new QEffect(ExpirationCondition.ExpiresAtEndOfYourTurn)
                                    {
                                        AfterYouTakeAction = async (qfSelf, takenAction) =>
                                        {
                                            if (takenAction.Name.Contains("Seek ("))
                                            {
                                                foreach (var friendly in thisCreature.Battle.AllCreatures.Where(c => c.FriendOfAndNotSelf(thisCreature)))
                                                {
                                                    if (!friendly.HasEffect(QEffectId.Quickened))
                                                    {
                                                        friendly.AddQEffect(QEffect.Quickened((hastedAction) => hastedAction.Name.Contains("Seek ") ? true : false).WithExpirationAtEndOfOwnerTurn());
                                                    }
                                                }
                                            }
                                        }
                                    });
                                });

                                return new ActionPossibility(GetInThereAction);
                            }
                            return null;
                        }
                    });
                });
        }

        public static Feat CreateBroadenedAssessment()
        {
            return new EnvoyFeat("Broadened Assesment", 4, "Your focus on an asset extends to more physical skills.",
                "You gain a +1 circumstance bonus to Acrobatics, Athletics, and Stealth checks against your assets", new[] { StarfinderEnvoyLoader.EnvoyTrait, Trait.ClassFeat })
                .WithOnCreature((sheet, creature) =>
                {
                    creature.AddQEffect(new QEffect()
                    {
                        BonusToSkillChecks = (usedSkill, combatAction, target) =>
                        {
                            if ((usedSkill == Skill.Acrobatics || usedSkill == Skill.Athletics || usedSkill == Skill.Stealth)
                            && target != null && target.QEffects.Any(qf => qf.Name == StarfinderEnvoyLoader.ACQUIRED_ASSET && qf.Source == creature))
                            {
                                return new Bonus(1, BonusType.Circumstance, "Acquired Asset",true);
                            }
                            return null;
                        },
                    });
                });
        }

        public static Feat CreateNotInTheFace()
        {
            return new EnvoyFeat("Not in the Face! {icon:Reaction}", 4, "Your charisma is enough to make even enemies second guess themselves for a split second, giving you an edge against them.",
                "{b}Trigger{/b} A creature targets you with a melee attack.\n  Attempt a Deception check against the triggering creature's Will DC. after the effects are applied, the triggering creature is immune to this reaction." +
                "\n{b}Critical Success{/b} The attacker takes a -2 circumstance penalty to attack and damage rolls against you until the start of its next turn." +
                "\n{b}Success{/b} Your attacker takes a -1 circumstance penalty to attack rolls against you until the start of its next turn." +
                "\n{b}Failure{/b} Your attacker takes a -1 circumstance penalty to the triggering attack roll." +
                "\n{b}Critical Failure{/b} Nothing happens.", new[] { Trait.Emotion, Trait.Mental, StarfinderEnvoyLoader.EnvoyTrait, Trait.ClassFeat })
                .WithPrerequisite((sheet) => sheet.GetProficiency(Trait.Deception) != Proficiency.Untrained, "Trained in Deception")
                .WithOnCreature((sheet, creature) =>
                {
                    creature.AddQEffect(new QEffect()
                    {
                        YouAreTargeted = async (qfSelf, action) =>
                        {
                            Creature attackingCreature = action.Owner;
                            if (!creature.Actions.IsReactionUsedUp && action.Traits.Contains(Trait.Melee) && action.Traits.Contains(Trait.Attack) && attackingCreature != null && !attackingCreature.HasTrait(Trait.Mindless))
                            {

                                ConfirmationRequest req = new ConfirmationRequest(creature, attackingCreature.Name + " is attacking you with a melee attack. Would you like to use you \"Not in the Face!\" reaction?", IllustrationName.Reaction, "Yes", "Pass");
                                var changePlansResult = (await creature.Battle.SendRequest(req)).ChosenOption;

                                if(changePlansResult is ConfirmOption)
                                {

                                    CombatAction NotInTheFaceReactionAction = new CombatAction(creature, IllustrationName.ExclamationMark, "Not in the Face {icon:Reaction}",
                                        new[] { Trait.Emotion, Trait.Mental, StarfinderEnvoyLoader.EnvoyTrait }, "attempt to decieve the attacker into second guessing their attacks.", Target.AdjacentCreature())
                                    .WithActionCost(0)
                                    .WithActiveRollSpecification(new ActiveRollSpecification(Checks.SkillCheck(Skill.Deception), Checks.DefenseDC(Defense.Will)))
                                    .WithEffectOnEachTarget(async (spell, caster, target, result) =>
                                    {
                                        if (result == CheckResult.CriticalSuccess)
                                        {
                                            target.AddQEffect(new QEffect("Avoiding the Face Crit Success", "Not in the Face critically succeeded against this creature.", ExpirationCondition.ExpiresAtStartOfYourTurn, creature, IllustrationName.ExclamationMarkQEffect)
                                            {
                                                BonusToAttackRolls = (qfSelf,attackAction,defender) =>
                                                {
                                                    if(defender == creature)
                                                    {
                                                        return new Bonus(-2, BonusType.Circumstance, "Avoiding the Face Crit Success", false);
                                                    }
                                                    return null;
                                                },
                                                BonusToDamage = (qfSelf,attackAction,defender) =>
                                                {
                                                    if(defender == creature)
                                                    {
                                                        return new Bonus(-2,BonusType.Circumstance, "Avoiding the Face Crit Success", false);
                                                    }
                                                    return null;
                                                }
                                            });
                                        }
                                        if (result == CheckResult.Success)
                                        {
                                            target.AddQEffect(new QEffect("Avoiding the Face Success", "Not in the Face succeeded against this creature.", ExpirationCondition.ExpiresAtStartOfYourTurn, creature, IllustrationName.ExclamationMarkQEffect)
                                            {
                                                BonusToAttackRolls = (qfSelf, attackAction, defender) =>
                                                {
                                                    if (defender == creature)
                                                    {
                                                        return new Bonus(-1, BonusType.Circumstance, "Avoiding the Face Success", false);
                                                    }
                                                    return null;
                                                },
                                            });
                                        }
                                        if (result == CheckResult.Failure)
                                        {
                                            target.AddQEffect(new QEffect("Avoiding the Face Failure", "Not in the Face failed against this creature.", ExpirationCondition.Immediately, creature, IllustrationName.ExclamationMarkQEffect)
                                            {
                                                BonusToAttackRolls = (qfSelf, attackAction, defender) =>
                                                {
                                                    if (defender == creature)
                                                    {
                                                        return new Bonus(-1, BonusType.Circumstance, "Avoiding the Face Failure", false);
                                                    }
                                                    return null;
                                                },
                                            });
                                        }
                                    });
                                    ChosenTargets targetOfReaction = new ChosenTargets() { ChosenCreature = attackingCreature };

                                    creature.Actions.UseUpReaction();
                                    await creature.Battle.GameLoop.FullCast(NotInTheFaceReactionAction, targetOfReaction);
                                }
                            }
                        }
                    });
                });
        }
    }
}
