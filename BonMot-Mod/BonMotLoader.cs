using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Modding;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Mechanics.Core;
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
                C.Proficiencies.Get(Trait.Diplomacy) == Proficiency.Trained, "Trained in diplomacy")
            .WithOnCreature((sheet, creature) =>
            {
                creature.AddQEffect(new QEffect("Bon Mot", "You launch an insightful quip at a foe, distracting them.")
                {
                    ProvideMainAction = (qfself)=>
                    {
                        var dude = qfself.Owner;
                        CombatAction bonmotAction = new CombatAction(dude, null, "Bon Mot", new Trait[] { Trait.Auditory, Trait.Concentrate, Trait.Emotion, Trait.General, Trait.Mental, Trait.Skill, Linguistic },
                            "Choose a foe within 30 feet and roll a Diplomacy check against the target's Will DC.\n\nCritical Success: The target is distracted and takes a –3 status penalty to Perception and Will saves for 1 minute. " +
                            "The target can end the effect early with a retort to your Bon Mot. This can either be a single action that has the concentrate trait or an appropriate skill action to frame their retort. " +
                            "\r\nSuccess: As critical success, but the penalty is –2." +
                            "\r\nCritical Failure: Your quip is atrocious. You take the same penalty an enemy would take had you succeeded. This ends after 1 minute or if you issue another Bon Mot and succeed.",
                            Target.RangedCreature(6).WithAdditionalConditionOnTargetCreature((caster, target) => target.DoesNotSpeakCommon ? Usability.NotUsableOnThisCreature("Target cannot understand your witty remarks") : Usability.Usable));

                        return new ActionPossibility(bonmotAction
                            .WithActionCost(1)
                            .WithActiveRollSpecification(
                            new ActiveRollSpecification(Checks.SkillCheck(Skill.Diplomacy), Checks.DefenseDC(Defense.Will)))
                            .WithEffectOnEachTarget(async (spell, caster, target, result) =>
                            {
                                if (result == CheckResult.CriticalSuccess)
                                {
                                    target.AddQEffect(new QEffect("Bon Mot Crit Sucess", "-3 status penalty to Perception and Will saves", ExpirationCondition.CountsDownAtEndOfYourTurn, caster)
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
                                        }
                                    }.WithExpirationAtStartOfSourcesTurn(caster, 10));
                                    caster.RemoveAllQEffects(q => q.Name == "Bon Mot Critical Failure");
                                }
                                else if (result == CheckResult.Success)
                                {
                                    target.AddQEffect(new QEffect("Bon Mot Success", "-2 status penalty to Perception and Will saves", ExpirationCondition.CountsDownAtEndOfYourTurn, caster)
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
                                        }
                                    }.WithExpirationAtStartOfSourcesTurn(caster, 10));
                                    caster.RemoveAllQEffects(q => q.Name == "Bon Mot Critical Failure");
                                }
                                if (result == CheckResult.CriticalFailure)
                                {
                                    caster.AddQEffect(new QEffect("Bon Mot Critical Failure", "-2 status penalty to Perception and Will saves", ExpirationCondition.CountsDownAtEndOfYourTurn, caster)
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