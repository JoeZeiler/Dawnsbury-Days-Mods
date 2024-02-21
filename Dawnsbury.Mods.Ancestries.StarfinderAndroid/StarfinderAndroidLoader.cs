using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dawnsbury.Mods.Ancestries.StarfinderAndroid
{
    public class StarfinderAndroidLoader
    {
        public static Trait AndroidTrait;
        public static Trait Tech;
        public static Trait Radiation;
        public static Trait Disease;
        public static Trait NaniteTechnical;
        public static QEffect NanitesActive;

        //public static Feat ConstructedFeat;
        //public static Feat EmotionallyUnawareFeat;

        [DawnsburyDaysModMainMethod]
        public static void LoadMod()
        {
            NanitesActive = new QEffect("Nanites Active", "The Androids Nanites are currently in use, and cannot be used for other nanite actions, activities, or reactions");
            AndroidTrait = ModManager.RegisterTrait("Android", new TraitProperties("Android", true) { IsAncestryTrait = true});
            Tech = ModManager.RegisterTrait("Tech", new TraitProperties("Tech", true, "Incorporates electronics,computer systems, and power sources.", true));
            Radiation = ModManager.RegisterTrait("Radiation", new TraitProperties("Radiation", true, null, true));
            Disease = ModManager.RegisterTrait("Disease", new TraitProperties("Disease", true, null, true));
            NaniteTechnical = ModManager.RegisterTrait("NaniteTechnical", new TraitProperties(string.Empty, false));
            ModManager.AddFeat(StarfinderAndroidAncestryFeats.CreateCleansingSubroutine());
            ModManager.AddFeat(StarfinderAndroidAncestryFeats.CreateEmotionless());
            ModManager.AddFeat(StarfinderAndroidAncestryFeats.CreateNaniteSurge());
            ModManager.AddFeat(StarfinderAndroidAncestryFeats.CreateSkillProgramming());
            ModManager.AddFeat(CreateStarfinderAndroid());
        }

        public static Feat CreateStarfinderAndroid()
        {
            Feat ConstructedFeat = new Feat(FeatName.CustomFeat, "Your synthetic body resists ailments better than that of a purely biological organism."
                , "You gain a +1 circumstance bonus to saving throws against diseases, poisons, and radiation.", new List<Trait> { }, null).WithCustomName("Constructed")
                .WithPermanentQEffect("+1 circumstance bonus to saving throws against diseases, poisons, and radiation.",(qf)=>
                {
                    qf.BonusToDefenses = (qfself, action, defense) =>
                    {
                        if(action == null)
                        {
                            return null;
                        }
                        if (action.Traits.Any(t => t == Trait.Poison || t == Radiation || t == Disease) && (defense == Defense.Will || defense == Defense.Reflex || defense == Defense.Fortitude))
                        {
                            return new Bonus(1, BonusType.Circumstance, "Constructed", true);
                        }
                        return null;
                    };
                });

            Feat EmotionallyUnawareFeat = new Feat(FeatName.CustomFeat, "You sometimes find it difficult to process and express complex emotions."
                , "You take a –1 circumstance penalty to Diplomacy and Performance checks.", new List<Trait> { }, null).WithCustomName("Emotionally Unaware")
                .WithPermanentQEffect("-1 circumstance penalty to emotion related checks", (qf) =>
                {
                    qf.BonusToSkillChecks = (skill, action, creature) =>
                    {
                        if (skill == Skill.Diplomacy || skill == Skill.Performance)
                        {
                            return new Bonus(-1, BonusType.Circumstance, "Emotionally Unaware", false);
                        }
                        
                        return null;
                    };
                });

            Feat starfinderAndroid = new AncestrySelectionFeat(FeatName.CustomFeat, "it's an android, but from starfinder", new List<Trait> { Trait.Humanoid, Tech, AndroidTrait }, 8, 5,
                new List<AbilityBoost>()
                {
                    new EnforcedAbilityBoost(Ability.Dexterity),
                    new EnforcedAbilityBoost(Ability.Intelligence),
                    new FreeAbilityBoost()
                }, CreateStarfinderAndroidHeritages().ToList())
                .WithAbilityFlaw(Ability.Charisma)
                .WithCustomName("Android (Starfinder)")
                .WithOnSheet(sheet =>
                {
                    sheet.AddFeat(ConstructedFeat, null);
                    sheet.AddFeat(EmotionallyUnawareFeat, null);
                });

            return starfinderAndroid;
        }

        public static IEnumerable<Feat> CreateStarfinderAndroidHeritages()
        {
            yield return new HeritageSelectionFeat(FeatName.CustomFeat,
                "You were created with a specific skill in mind, and your programming reflects this.",
                "You become trained in a feat of your choice, and you gain an additional skill training at level 3.")
                .WithOnSheet(sheet =>
                {
                    sheet.AddSelectionOption(new SingleFeatSelectionOption("Skill Specifications", "Skill Specifications skill", 1, (Feat feat) => feat is SkillSelectionFeat));
                    sheet.AddSkillIncreaseOption(3);
                })
                .WithCustomName("Skill Specifications");
            yield return new HeritageSelectionFeat(FeatName.CustomFeat,
                "Your body was originally forged for combat, likely created to function as a security officer or soldier.",
                "you are trained in all simple and martial weapons")
                .WithOnSheet(sheet =>
                {
                    sheet.SetProficiency(Trait.Simple, Proficiency.Trained);
                    sheet.SetProficiency(Trait.Martial, Proficiency.Trained);
                })
                .WithCustomName("Warrior Specification");
            yield return new HeritageSelectionFeat(FeatName.CustomFeat,
                "Muscle memory hints at your body’s past, and people you’ve never met strangely recognize your face.",
                "The first time in a day that you lose the dying condition, you don't gain a wounded condition.")
                .WithOnCreature(creature =>
                {
                    creature.AddQEffect(new QEffect("Renewed Android", "The first time in a day that you lose the dying condition, you don't gain a wounded condition.")
                    {
                        YouAcquireQEffect = (qfSelf,qfAcquiring) =>
                        {
                            if(qfAcquiring.Id == QEffectId.Wounded && !qfSelf.Owner.PersistentUsedUpResources.UsedUpActions.Contains("RenewedAndroid"))
                            {
                                qfSelf.Owner.PersistentUsedUpResources.UsedUpActions.Add("RenewedAndroid");
                                qfSelf.Description = "*USED UP THIS DAY* The first time in a day that you lose the dying condition, you don't gain a wounded condition.";
                                return null;
                            }
                            return qfAcquiring;
                        },
                        StartOfCombat = async(qfSelf) =>
                        {
                            if (qfSelf.Owner.PersistentUsedUpResources.UsedUpActions.Contains("RenewedAndroid"))
                            {
                                qfSelf.Description = "*USED UP THIS DAY* The first time in a day that you lose the dying condition, you don't gain a wounded condition.";
                            }
                        }
                    });
                })
                .WithCustomName("Renewed");
        }



    }
}
