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
using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Dawnsbury.Core;
using Dawnsbury.Display.Text;

namespace Dawnsbury.Mods.Feats.General.BonMot;

/// <summary>
/// loads the Bon Mot feat
/// </summary>
public class BonMotLoader
{

    public const string defaultInsult = "You fight like a dairy farmer!";
    public const string defaultRetort = "How appropriate, you fight like a cow!";
    public const string defaultCritInsult = "I will milk every drop of blood from your body!";
    private const string logDialogFormat = "{0} says, \"{1}\"";
    private static ModdedIllustration DairyBottleIllustration;
    private static ModdedIllustration GuybrushIllustration;
    private static ModdedIllustration CaptainSmirkIllustration;

    /// <summary>
    /// here to add the linguistic trait
    /// </summary>
    public static Trait Linguistic;
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        DairyBottleIllustration = new ModdedIllustration(@"BonMotResources\DairyBottle.png");
        GuybrushIllustration = new ModdedIllustration(@"BonMotResources\GuybrushWithSword.png");
        CaptainSmirkIllustration = new ModdedIllustration(@"BonMotResources\CaptainSmirk.png");
        List<Tuple<string, string, string>> insultDirectory = LoadInsultDirectory();
        //registering the linguistic trait so we can add it to Bon Mot
        Linguistic = ModManager.RegisterTrait(
            "Linguistic",
            new TraitProperties("Linguistic",true, "Only works on enemies that speak Common.")
            );
        ModManager.AddFeat(CreateBonMotFeat(insultDirectory));
    }

    /// <summary>
    /// loads the insult directory from the insults.txt file
    /// </summary>
    /// <returns>the constructed directory</returns>
    public static List<Tuple<string, string, string>> LoadInsultDirectory()
    {
        List<Tuple<string, string, string>> insultDirectory = new List<Tuple<string, string, string>>();

        try
        {
            string directory = Directory.GetCurrentDirectory();
            directory = Directory.GetParent(directory).FullName;
            directory = Path.Combine(directory, "CustomMods", "BonMotResources","Insults.txt");
            StreamReader sr = new StreamReader(directory);
            string line = sr.ReadLine();
            while (line != null)
            {
                line = line.Trim();
                List<string> lines = line.Replace("\" \"", "\"").Split("\"").ToList();
                lines.RemoveAll(l => l==string.Empty);
                if (lines.Count != 3)
                {
                    if (insultDirectory.Count == 0)
                    {
                        insultDirectory.Add(new Tuple<string, string, string>(defaultInsult, defaultRetort, defaultCritInsult));
                    }
                    return insultDirectory;
                }
                Tuple<string, string, string> thisSet = new Tuple<string, string, string>(lines[0], lines[1], lines[2]);
                insultDirectory.Add(thisSet);
                line = sr.ReadLine();
            }
            sr.Close();
        }
        catch(IOException)
        {

        }
        if(insultDirectory.Count == 0)
        {
            insultDirectory.Add(new Tuple<string, string, string>(defaultInsult, defaultRetort, defaultCritInsult));
        }
        return insultDirectory;
    }

    /// <summary>
    /// creates the Bon Mot feat, which adds the available Bot Mot action which can cause negatives to will saves and perception
    /// </summary>
    /// <returns>the created feat</returns>
    private static Feat CreateBonMotFeat(List<Tuple<string, string, string>> insultDirectory)
    {
        Random rand = new Random(DateTime.Now.GetHashCode());

        string[] badInsults = { "Boy are you ugly!", "What an idiot!", "You call yourself a creature!" };
        string[] terribleInsults = { "I am rubber, you are glue.", "I'm shaking, I'm shaking!" };

        var description = "{b}Range{/b} 30 feet\n\nRoll a Diplomacy check against the target's Will DC." + S.FourDegreesOfSuccess("The target is distracted and takes a –3 status penalty to Perception and Will saves for 1 minute. The target can end the effect early with a retort to your Bon Mot as a single action.", "As critical success, but the penalty is –2.", null, "Your quip is atrocious. You take the same penalty an enemy would take had you succeeded. This ends after 1 minute or if you issue another Bon Mot and succeed.");
        
        return new TrueFeat(
                FeatName.CustomFeat,
                1,
                "You launch an insightful quip at a foe, distracting them.",
                description,
                new[] { Trait.Auditory, Trait.Concentrate, Trait.Emotion, Trait.General, Trait.Mental, Trait.Skill, Linguistic })
            .WithActionCost(1)
            .WithCustomName("Bon Mot")
            .WithPrerequisite(C =>
                C.Proficiencies.Get(Trait.Diplomacy) >= Proficiency.Trained, "Trained in Diplomacy.")
            .WithOnCreature((sheet, creature) =>
            {
                creature.AddQEffect(new QEffect("Bon Mot", "You launch an insightful quip at a foe, distracting them.")
                {
                    ProvideActionIntoPossibilitySection = (qfself,possibilitySection)=>
                    {
                        if(possibilitySection.PossibilitySectionId != PossibilitySectionId.OtherManeuvers)
                        {
                            return null;
                        }

                        var dude = qfself.Owner;
                        CombatAction bonmotAction = new CombatAction(dude, CaptainSmirkIllustration!=null?CaptainSmirkIllustration:IllustrationName.QuestionMark, "Bon Mot", new Trait[] { Trait.Auditory, Trait.Concentrate, Trait.Emotion, Trait.General, Trait.Mental, Trait.Skill, Trait.Basic, Linguistic },
                            description,
                            Target.Ranged(6).WithAdditionalConditionOnTargetCreature((caster, target) => target.DoesNotSpeakCommon ? Usability.NotUsableOnThisCreature("target cannot understand your witty remarks") : Usability.Usable))
                            .WithActionCost(1)
                            .WithActiveRollSpecification(
                            new ActiveRollSpecification(Checks.SkillCheck(Skill.Diplomacy), Checks.DefenseDC(Defense.Will)));

                        return new ActionPossibility(bonmotAction                     
                            .WithEffectOnEachTarget(async (spell, caster, target, result) =>
                            {
                                int insultID = rand.Next(0, insultDirectory.Count); 

                                string successInsult = insultDirectory[insultID].Item1;
                                string critInsult = insultDirectory[insultID].Item3;
                                string insultRetort = insultDirectory[insultID].Item2;

                                
                                if (result == CheckResult.CriticalSuccess)
                                {
                                    QEffect bonMotCritEffect = new QEffect("Bon Mot Crit Success", "You have a -3 status penalty to Perception and Will saves.", ExpirationCondition.CountsDownAtEndOfYourTurn, caster, DairyBottleIllustration != null? DairyBottleIllustration:IllustrationName.QuestionMark)
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
                                                    new CombatAction(targetDude, GuybrushIllustration != null?GuybrushIllustration:IllustrationName.QuestionMark, "Retort", new Trait[] { Trait.Auditory, Trait.Concentrate, Trait.Mental, Linguistic },
                                                    "Retort to get rid of a Bon Mot debuff.", Target.Self((innerSelf, ai) => (ai.Tactic == Tactic.Standard && (innerSelf.Actions.AttackedThisTurn.Any() || (innerSelf.Spellcasting != null)) && innerSelf.DistanceTo(caster) <= 6 && innerSelf.CanSee(caster))
                                                    ? AIConstants.EXTREMELY_PREFERRED : AIConstants.NEVER))
                                                    .WithActionCost(1)
                                                    .WithEffectOnSelf(async (innerSelf) =>
                                                    {
                                                        innerSelf.RemoveAllQEffects((q) => (q.Name == "Bon Mot Crit Success" || q.Name == "Bon Mot Success") && q.Source == caster);
                                                        innerSelf.Battle.CombatLog.Add(new Core.LogLine(2, string.Format(logDialogFormat, innerSelf.Name,insultRetort), "Retort", insultRetort));
                                                        innerSelf.Occupies.Overhead(insultRetort, Color.Green);
                                                    }));
                                        }
                                    };

                                    target.RemoveAllQEffects((q) => (q.Name == "Bon Mot Crit Success" || q.Name == "Bon Mot Success") && q.Source == caster);
                                    target.AddQEffect(bonMotCritEffect.WithExpirationAtStartOfSourcesTurn(caster, 10));
                                    caster.RemoveAllQEffects(q => q.Name == "Bon Mot Critical Failure");
                                    caster.Battle.CombatLog.Add(new Core.LogLine(2, string.Format(logDialogFormat, caster.Name, critInsult), "Bon Mot", critInsult));
                                    caster.Occupies.Overhead(critInsult, Color.Green);
                                }
                                else if (result == CheckResult.Success)
                                {
                                    
                                    QEffect bonMotSuccessEffect = new QEffect("Bon Mot Success", "You have a -2 status penalty to Perception and Will saves.", ExpirationCondition.CountsDownAtEndOfYourTurn, caster, DairyBottleIllustration != null ? DairyBottleIllustration : IllustrationName.QuestionMark)
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
                                                    new CombatAction(targetDude, GuybrushIllustration != null ? GuybrushIllustration : IllustrationName.QuestionMark, "Retort", new Trait[] { Trait.Auditory, Trait.Concentrate, Trait.Mental, Linguistic },
                                                    "Retort to get rid of a Bon Mot debuff.", Target.Self((innerSelf, ai) => (ai.Tactic == Tactic.Standard && (innerSelf.Actions.AttackedThisTurn.Any() || (innerSelf.Spellcasting != null)) && innerSelf.DistanceTo(caster) <= 6 && innerSelf.CanSee(caster))
                                                    ? AIConstants.EXTREMELY_PREFERRED : AIConstants.NEVER))
                                                    .WithActionCost(1)
                                                    .WithEffectOnSelf(async (innerSelf) =>
                                                    {
                                                        innerSelf.RemoveAllQEffects((q) => (q.Name == "Bon Mot Crit Success" || q.Name == "Bon Mot Success") && q.Source == caster);
                                                        innerSelf.Battle.CombatLog.Add(new Core.LogLine(2, string.Format(logDialogFormat, innerSelf.Name, insultRetort), "Retort", insultRetort));
                                                        innerSelf.Occupies.Overhead(insultRetort, Color.Green);
                                                    }));
                                        }
                                    };

                                    target.RemoveAllQEffects((q) => (q.Name == "Bon Mot Success") && q.Source == caster);
                                    target.AddQEffect(bonMotSuccessEffect.WithExpirationAtStartOfSourcesTurn(caster, 10));
                                    caster.RemoveAllQEffects(q => q.Name == "Bon Mot Critical Failure");
                                    caster.Battle.CombatLog.Add(new Core.LogLine(2, string.Format(logDialogFormat, caster.Name, successInsult), "Bon Mot", successInsult));
                                    caster.Occupies.Overhead(successInsult, Color.YellowGreen);
                                }
                                else if(result == CheckResult.Failure)
                                {
                                    int failNum = rand.Next(0, 3);
                                    string failString = badInsults[failNum];
                                    caster.Battle.CombatLog.Add(new Core.LogLine(2, string.Format(logDialogFormat, caster.Name, failString), "Bon Mot", failString));
                                    caster.Occupies.Overhead(failString, Color.Yellow);
                                }
                                if (result == CheckResult.CriticalFailure)
                                {
                                    int failNum = rand.Next(0, 2);
                                    string failString = terribleInsults[failNum];
                                    caster.AddQEffect(new QEffect("Bon Mot Critical Failure", "You have a -2 status penalty to Perception and Will saves.", ExpirationCondition.CountsDownAtEndOfYourTurn, caster, DairyBottleIllustration != null ? DairyBottleIllustration : IllustrationName.QuestionMark)
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
                                    caster.Battle.CombatLog.Add(new Core.LogLine(2, string.Format(logDialogFormat, caster.Name, failString), "Bon Mot", failString));
                                    caster.Occupies.Overhead(failString, Color.Red);
                                }
                            }));
                    }
                });
            });
    }
}