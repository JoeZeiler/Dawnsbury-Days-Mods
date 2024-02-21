using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dawnsbury.Mods.Ancestries.StarfinderAndroid
{
    public static class StarfinderAndroidAncestryFeats
    {
        public static Feat CreateSkillProgramming()
        {
            return new StarfinderAndroidFeat("Skill Programming", "You were programmed with extra skill training or found a way to download programming to improve your skills.", "You become trained in two additional skills")
            .WithOnSheet((sheet) =>
            {
                sheet.AddSelectionOption(new SingleFeatSelectionOption("Skill Programming", "Skill Programming skill", 1, (Feat feat) => feat is SkillSelectionFeat));
                sheet.AddSelectionOption(new SingleFeatSelectionOption("Skill Programming", "Skill Programming skill", 1, (Feat feat) => feat is SkillSelectionFeat));
            });
        }

        public static Feat CreateCleansingSubroutine()
        {
            return new StarfinderAndroidFeat("Cleansing Subroutine", "Your nanites help purge your body of harmful toxins"
                , "Each time you successd at a Fortitude save against a poison effect, you improve your saving throw by one step").WithPermanentQEffect((qf) =>
                {
                    qf.AdjustSavingThrowResult = (qfself, action, result) =>
                    {
                        if (action.Traits.Contains(Trait.Poison) && action.SavingThrow.Defense == Defense.Fortitude)
                        {
                            return result.ImproveByOneStep();
                        }
                        return result;
                    };
                });
        }

        public static Feat CreateEmotionless()
        {
            return new StarfinderAndroidFeat("Emotionless", "Your inhibited emotional processors make it difficult for you to feel strong emotions."
                , "You gain a +1 circumstance bonus to saving throws against emotion and fear effects. If you roll a success on a saving throw against an emotion or fear effect, you get a critical success instead.")
                .WithPermanentQEffect((qf) =>
                {
                    qf.BonusToDefenses = (qfSelf, action, defense) =>
                    {
                        if(action.Traits.Contains(Trait.Emotion) || action.Traits.Contains(Trait.Fear))
                        {
                            return new Bonus(1, BonusType.Circumstance, "Emotionless", true);
                        }
                        return null;
                    };
                });
        }

        public static Feat CreateNaniteSurge()
        {
            return new StarfinderAndroidFeat("Nanite Surge", "", "You gain a +2 status bonus to the triggering skill check.")
                .WithPermanentQEffect((qf) =>
                {
                    bool naniteSurgeUsed = false;

                    qf.AfterYouTakeAction = async (qfself, action) =>
                    {
                        var newAction = action;
                    };

                    qf.BeforeYourActiveRoll = async (qfself, action, target) =>
                    {
                        if (naniteSurgeUsed || qfself.Owner.QEffects.Any(qf => qf.Name == StarfinderAndroidLoader.NanitesActive.Name))
                        {
                            return;
                        }
                        if (action.HasTrait(Trait.Skill) || action.HasTrait(Trait.Deception) || action.HasTrait(Trait.Stealth) || action.HasTrait(Trait.Intimidation))
                        {
                            bool useReaction = await target.Battle.AskToUseReaction(qf.Owner, "would you like to use Nanite Surge to give yourself a +2 status bonus to this check?");
                            if (useReaction)
                            {
                                naniteSurgeUsed = true;
                                action.WithActiveRollSpecification(new ActiveRollSpecification(action.ActiveRollSpecification.DetermineBonus.WithExtraBonus((action, caster, target) =>
                                {
                                    return new Bonus(2, BonusType.Status, "Nanite Surge", true);
                                }), action.ActiveRollSpecification.DetermineDC));
                            }
                        }
                    };
                });
        }
    }
}
