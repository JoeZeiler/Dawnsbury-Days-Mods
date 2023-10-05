using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Modding;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Core.Intelligence;
using System.Linq;

namespace Dawnsbury.Mods.Feats.General.BonMot;

/// <summary>
/// loads the Bon Mot feat
/// </summary>
public class BonMotLoader
{
    /// <summary>
    /// here to add the linguistic trait
    /// </summary>
    public static Trait Linguistic;
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        //registering the linguistic trait so we can add it to Bon Mot
        Linguistic = ModManager.RegisterTrait(
            "Linguistic",
            new TraitProperties("Linguistic",true, "Only works on enemies that speak common")
            );
        ModManager.AddFeat(CreateBonMotFeat());
    }

    /// <summary>
    /// creates the Bon Mot feat, which adds the available Bot Mot action which can cause negatives to will saves and perception
    /// </summary>
    /// <returns>the created feat</returns>
    private static Feat CreateBonMotFeat()
    {

        return new TrueFeat(
                FeatName.CustomFeat,
                1,
                "You launch an insightful quip at a foe, distracting them.",
                "Choose a foe within 30 feet and roll a Diplomacy check against the target's Will DC.\n\nCritical Success: The target is distracted and takes a –3 status penalty to Perception and Will saves for 1 minute. " +
                "The target can end the effect early with a retort to your Bon Mot. This can either be a single action that has the concentrate trait or an appropriate skill action to frame their retort. " +
                "\r\nSuccess: As critical success, but the penalty is –2." +
                "\r\nCritical Failure: Your quip is atrocious. You take the same penalty an enemy would take had you succeeded. This ends after 1 minute or if you issue another Bon Mot and succeed.",
                new[] { Trait.Auditory, Trait.Concentrate, Trait.Emotion, Trait.General, Trait.Mental, Trait.Skill, Linguistic })
            .WithActionCost(1)
            .WithCustomName("Bon Mot")
            .WithPrerequisite(C =>
                C.Proficiencies.Get(Trait.Diplomacy) >= Proficiency.Trained, "Trained in diplomacy")
            .WithOnCreature((sheet, creature) =>
            {
                creature.AddQEffect(new QEffect("Bon Mot", "You launch an insightful quip at a foe, distracting them.")
                {
                    ProvideMainAction = (qfself)=>
                    {
                        var dude = qfself.Owner;
                        CombatAction bonmotAction = new CombatAction(dude, new ModdedIllustration(@"CaptainSmirk.png"), "Bon Mot", new Trait[] { Trait.Auditory, Trait.Concentrate, Trait.Emotion, Trait.General, Trait.Mental, Trait.Skill, Linguistic },
                            "Choose a foe within 30 feet and roll a Diplomacy check against the target's Will DC.\n\nCritical Success: The target is distracted and takes a –3 status penalty to Perception and Will saves for 1 minute. " +
                            "The target can end the effect early with a retort to your Bon Mot. This can either be a single action that has the concentrate trait or an appropriate skill action to frame their retort. " +
                            "\r\nSuccess: As critical success, but the penalty is –2." +
                            "\r\nCritical Failure: Your quip is atrocious. You take the same penalty an enemy would take had you succeeded. This ends after 1 minute or if you issue another Bon Mot and succeed.",
                            Target.RangedCreature(6).WithAdditionalConditionOnTargetCreature((caster, target) => target.DoesNotSpeakCommon ? Usability.NotUsableOnThisCreature("Target cannot understand your witty remarks") : Usability.Usable))
                            .WithActionCost(1)
                            .WithActiveRollSpecification(
                            new ActiveRollSpecification(Checks.SkillCheck(Skill.Diplomacy), Checks.DefenseDC(Defense.Will)));

                        return new ActionPossibility(bonmotAction                     
                            .WithEffectOnEachTarget(async (spell, caster, target, result) =>
                            {
                                var dairyBottle = new ModdedIllustration(@"DairyBottle.png");
                                if (result == CheckResult.CriticalSuccess)
                                {
                                    QEffect bonMotCritEffect = new QEffect("Bon Mot Crit Success", "-3 status penalty to Perception and Will saves", ExpirationCondition.CountsDownAtEndOfYourTurn, caster, dairyBottle)
                                    {
                                        BonusToDefenses = (qfSelf, incomingEffect, targetedDefense) =>
                                        {
                                            if (targetedDefense == Defense.Will || targetedDefense == Defense.Perception)
                                            {
                                                return new Bonus(-3, BonusType.Status, "Bon Mot Critical Success", false);
                                            }
                                            return null;
                                        },
                                        BonusToAttackRolls = (qfself, incomingEffect, targetedCreature) =>
                                        {
                                            if (incomingEffect.ActionId == ActionId.Seek)
                                            {
                                                return new Bonus(-3, BonusType.Status, "Bon Mot Critical Success", false);
                                            }
                                            return null;
                                        },
                                        ProvideContextualAction = (qfself) =>
                                        {
                                            var targetDude = qfself.Owner;
                                            
                                            //retort removes bon mot debuff with an action, but only if the bon mot creature is within 30 feet, can be seen by the retort user. AI also prioritizes attacking at least once.
                                            return new ActionPossibility(
                                                    new CombatAction(targetDude, new ModdedIllustration(@"GuybrushWithSword.png"), "Retort", new Trait[] { Trait.Auditory, Trait.Concentrate, Trait.Mental, Linguistic },
                                                    "retort to get rid of a Bon Mot debuff", Target.Self((innerSelf, ai) => (ai.Tactic == Tactic.Standard && (innerSelf.Actions.AttackedThisTurn.Any() || (innerSelf.Spellcasting != null)) && innerSelf.DistanceTo(caster) <= 6 && innerSelf.CanSee(caster))
                                                    ? AIConstants.EXTREMELY_PREFERRED : AIConstants.NEVER))
                                                    .WithActionCost(1)
                                                    .WithEffectOnSelf(async (innerSelf) =>
                                                    {
                                                        innerSelf.RemoveAllQEffects((q) => (q.Name == "Bon Mot Crit Success" || q.Name == "Bon Mot Success") && q.Source == caster);
                                                        innerSelf.Battle.CombatLog.Add(new Core.LogLine(2, innerSelf.Name + " says, \"First you'd better stop waving it like a feather duster.\"", "Retort", "First you'd better stop waving it like a feather duster."));
                                                    }));
                                        }
                                    };

                                    target.RemoveAllQEffects((q) => (q.Name == "Bon Mot Crit Success" || q.Name == "Bon Mot Success") && q.Source == caster);
                                    target.AddQEffect(bonMotCritEffect.WithExpirationAtStartOfSourcesTurn(caster, 10));
                                    caster.RemoveAllQEffects(q => q.Name == "Bon Mot Critical Failure");
                                    caster.Battle.CombatLog.Add(new Core.LogLine(2, caster.Name + " says, \"My tongue is sharper than any sword.\"", "Bon Mot", "My tongue is sharper than any sword."));
                                }
                                else if (result == CheckResult.Success)
                                {
                                    QEffect bonMotSuccessEffect = new QEffect("Bon Mot Success", "-2 status penalty to Perception and Will saves", ExpirationCondition.CountsDownAtEndOfYourTurn, caster, dairyBottle)
                                    {
                                        BonusToDefenses = (qfSelf, incomingEffect, targetedDefense) =>
                                        {
                                            if (targetedDefense == Defense.Will || targetedDefense == Defense.Perception)
                                            {
                                                return new Bonus(-2, BonusType.Status, "Bon Mot Success", false);
                                            }
                                            return null;
                                        },
                                        BonusToAttackRolls = (qfself, incomingEffect, targetedCreature) =>
                                        {
                                            if (incomingEffect.ActionId == ActionId.Seek)
                                            {
                                                return new Bonus(-2, BonusType.Status, "Bon Mot Success", false);
                                            }
                                            return null;
                                        },
                                        ProvideContextualAction = (qfself) =>
                                        {
                                            var targetDude = qfself.Owner;

                                            //retort removes bon mot debuff with an action, but only if the bon mot creature is within 30 feet, can be seen by the retort user. AI also prioritizes attacking at least once unless they can cast spells.
                                            return new ActionPossibility(
                                                    new CombatAction(targetDude, new ModdedIllustration(@"GuybrushWithSword.png"), "Retort", new Trait[] { Trait.Auditory, Trait.Concentrate, Trait.Mental, Linguistic },
                                                    "retort to get rid of a Bon Mot debuff", Target.Self((innerSelf, ai) => (ai.Tactic == Tactic.Standard && (innerSelf.Actions.AttackedThisTurn.Any() || (innerSelf.Spellcasting != null)) && innerSelf.DistanceTo(caster) <= 6 && innerSelf.CanSee(caster))
                                                    ? AIConstants.EXTREMELY_PREFERRED : AIConstants.NEVER))
                                                    .WithActionCost(1)
                                                    .WithEffectOnSelf(async (innerSelf) =>
                                                    {
                                                        innerSelf.RemoveAllQEffects((q) => (q.Name == "Bon Mot Crit Success" || q.Name == "Bon Mot Success") && q.Source == caster);
                                                        innerSelf.Battle.CombatLog.Add(new Core.LogLine(2, innerSelf.Name + " says, \"How appropriate, you fight like a cow\"", "Retort", "How appropriate, you fight like a cow"));
                                                    }));
                                        }
                                    };

                                    target.RemoveAllQEffects((q) => (q.Name == "Bon Mot Success") && q.Source == caster);
                                    target.AddQEffect(bonMotSuccessEffect.WithExpirationAtStartOfSourcesTurn(caster, 10));
                                    caster.RemoveAllQEffects(q => q.Name == "Bon Mot Critical Failure");
                                    caster.Battle.CombatLog.Add(new Core.LogLine(2, caster.Name + " says, \"You fight like a dairy farmer\"", "Bon Mot", "You fight like a dairy farmer"));
                                }
                                if (result == CheckResult.CriticalFailure)
                                {
                                    caster.AddQEffect(new QEffect("Bon Mot Critical Failure", "-2 status penalty to Perception and Will saves", ExpirationCondition.CountsDownAtEndOfYourTurn, caster, dairyBottle)
                                    {
                                        BonusToDefenses = (qfSelf, incomingEffect, targetedDefense) =>
                                        {
                                            if (targetedDefense == Defense.Will || targetedDefense == Defense.Perception)
                                            {
                                                return new Bonus(-2, BonusType.Status, "Bon Mot Critical Failure", false);
                                            }
                                            return null;
                                        },
                                        BonusToAttackRolls = (qfself, incomingEffect, targetedCreature) =>
                                        {
                                            if (incomingEffect.ActionId == ActionId.Seek)
                                            {
                                                return new Bonus(-2, BonusType.Status, "Bon Mot Critical Failure", false);
                                            }
                                            return null;
                                        }
                                    }.WithExpirationAtStartOfSourcesTurn(caster, 10));
                                }

                            }));
                    }
                });
            });
    }
}