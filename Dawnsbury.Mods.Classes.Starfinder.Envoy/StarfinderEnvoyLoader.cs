using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dawnsbury.Mods.Classes.Starfinder.Envoy
{
    public class StarfinderEnvoyLoader
    {
        public static Trait EnvoyTrait;
        public static Trait Directive;
        public static Trait ActOfLeadershipTechnical;
        public static Feat WiseToTheGameFeat;

        public const string FROM_THE_FRONT = "From The Front";
        public const string FROM_THE_SHADOWS = "From The Shadows";
        public const string GUNS_BLAZING = "Guns Blazing";
        public const string THROUGH_DESPERATE_TIMES = "Through Desperate Times";
        public const string ACQUIRED_ASSET = "Acquired Asset";
        public const string DIRECTIVES_SUBMENU_CAPTION = "Directives";
        public const string DIRECTIVES_POSSIBILITY_SECTION_NAME = "DirectiveSection";
        public const string GET_ME_NAME = "Get Me!";
        /// <summary>
        /// loads the starfinder envoy mod.
        /// </summary>
        [DawnsburyDaysModMainMethod]
        public static void LoadMod()
        {
            EnvoyTrait = ModManager.RegisterTrait("Envoy", new TraitProperties("Envoy", true, relevantForShortBlock: true) { IsClassTrait = true });
            Directive = ModManager.RegisterTrait("Directive", new TraitProperties("Directive", true, relevantForShortBlock: true));
            ActOfLeadershipTechnical = ModManager.RegisterTrait("ActOfLeadershipTechnical", new TraitProperties("ActOfLeadershipTechnical", false));
            WiseToTheGameFeat = CreateWiseToTheGameFeat();
            var EnvoyFeat = GenerateClassSelectionFeat();
            ModManager.AddFeat(EnvoyFeat);
            ModManager.AddFeat(EnvoyFeats.CreateQuip());
            ModManager.AddFeat(EnvoyFeats.CreateDiverseSchemes());
            ModManager.AddFeat(EnvoyFeats.CreateChangeOfPlans());
            ModManager.AddFeat(EnvoyFeats.CreateWatchOut());
            ModManager.AddFeat(EnvoyFeats.CreateGetInThere());
            ModManager.AddFeat(EnvoyFeats.CreateSearchHighAndLow());
            ModManager.AddFeat(EnvoyFeats.CreateBroadenedAssessment());
            ModManager.AddFeat(EnvoyFeats.CreateNotInTheFace());
            //ModManager.AddFeat(EnvoyFeats.CreatePardonme());
            //LoadOrder.WhenFeatsBecomeLoaded += () =>
            //{
            //    TryThroughDesperateTimes(EnvoyFeat);
            //};
        }

        /// <summary>
        /// generates the Envoy class selection
        /// </summary>
        /// <returns>the Envoy class selection feat</returns>
        private static Feat GenerateClassSelectionFeat()
        {
            var envoySelection = new ClassSelectionFeat(FeatName.CustomFeat, "Master Influencer, a versatile leader.", EnvoyTrait,
                new EnforcedAbilityBoost(Ability.Charisma), 10, new[] { Trait.Perception, Trait.Fortitude, Trait.Simple, Trait.Martial, Trait.Unarmed, Trait.UnarmoredDefense, Trait.LightArmor},
                new[] { Trait.Reflex, Trait.Will }, 6, "{b}1. Get 'Em! {icon:Action}{/b} Select a creature within 60 feet that you can see. That creature gets a -1 circumstance penalty to their AC until the beginning of your next turn. " +
                "\n{b}Lead by Example{/b} If you attack the Get 'Em target before the end of your turn, you and your allies gain a +1 circumstance bonus to damage rolls against the target." +
                "\n\n{b}2. Acquire Asset {icon:Action}{/b} Make a melee or ranged strike against a creature that is not your asset. On a hit, your target becomes your asset." +
                "\n You gain a +1 circumstance bonus to Deception, Diplomacy, Intimidation, and Perception checks against your asset. You can only mantain one asset at a time, acquiring a new one replaces your current one." +
                "\n\n{b}3. Saw It Coming {icon:FreeAction}{/b} You gain a +1 circumstance bonus to your initiative roll, and you can immediately step or stride." +
                "\n\n{b}At higher levels:{/b}\n{b}Level 3:{/b}" +
                "\n{b}Adaptive Talent{/b}: You gain an additional general feat" +
                "\n{b}Wise to the Game{/b}: You gain a +1 status bonus to your Perception DC against feint and attempts to divert your attention with Create a Diversion. Also you gain a +1 status bonus to your Will DC against attempts to Demoralize you. This bonus is +2 against your asset.", GenerateEnvoySubclasses().ToList()).WithCustomName("Combat Envoy").WithOnCreature((creature) =>
                {
                    SetupGetEm(creature);
                    SetupAcquireAsset(creature);
                    SetupSawItComing(creature);
                }).WithOnSheet(sheet =>
                {
                    sheet.AddSelectionOption(new SingleFeatSelectionOption("level1EnvoyFeat", "Envoy feat", 1, (feat) => feat.HasTrait(EnvoyTrait) && feat is TrueFeat && ((TrueFeat)feat).Level == 1));
                    sheet.AddSelectionOption(new SingleFeatSelectionOption("skillTrainingBonus", "Skill Training Bonus", 1, (feat) => feat.FeatName == FeatName.Deception || feat.FeatName == FeatName.Diplomacy || feat.FeatName == FeatName.Intimidation));
                    sheet.AddSelectionOption(new SingleFeatSelectionOption("adaptiveTalent", "Adaptive Talent", 3, (feat) => feat.Traits.Contains(Trait.General)));
                    sheet.AddAtLevel(3, (values) => { values.AddFeat(WiseToTheGameFeat, null); });
                });
            return envoySelection;
        }

        private static void SetupSawItComing(Creature creature)
        {
            creature.AddQEffect(
                new QEffect("Saw It Coming {icon:FreeAction}","{b}Trigger{/b} You are about to roll initiative." +
                "\nYou get a +1 circumstance bonus to your initiative roll, and you can immediately Step or Stride.")
                {
                    StartOfCombat = async (qfSelf) =>
                    {
                        if (!creature.HasFeat(FeatName.IncredibleInitiative) && creature.Battle.InitiativeOrder.First() != creature)
                        {
                            var creatures = creature.Battle.AllCreatures.Where(c => c.Initiative == creature.Initiative && !creature.FriendOf(c));
                            if (creatures.Any())
                            {
                                var creatureToPass = creature.Battle.InitiativeOrder.First(c => creatures.Contains(c));
                                int targetIndex = creature.Battle.InitiativeOrder.IndexOf(creatureToPass);
                                creature.Initiative = creature.Initiative + 1;
                                creature.Battle.MoveInInitiativeOrder(creature, targetIndex);
                            }
                        }
                        await creature.StrideAsync("Saw it Coming: Stride or Step", true, allowCancel: true);
                    }
                }
            );
        }

        private static void SetupAcquireAsset(Creature creature)
        {
            Creature target1 = null;
            Creature target2 = null;

            QEffect GenerateAssetEffect(Creature owner)
            {
                return new QEffect(ACQUIRED_ASSET, owner.Name + " has acquired you as an asset, giving them special bonuses against you", ExpirationCondition.Never, owner, IllustrationName.CalculateThreats)
                {
                    YouAreDealtLethalDamage = async (qfself,attacker,dmgStuff,defender) =>
                    {
                        if(target1 == defender)
                        {
                            target1 = null;
                        }
                        if(target2 == defender)
                        {
                            target2 = null;
                        }
                        defender.RemoveAllQEffects(qf => qf == qfself);
                        return null;
                    }
                };
            }

            QEffect AcqAssetQFX = GenerateAssetEffect(creature);

            creature.AddQEffect(
                new QEffect()
                {
                    ProvideStrikeModifier = (item) =>
                    {
                        CombatAction AcquireAsset = creature.CreateStrike(item);

                        
                        var addedText = (target1 != null || target2 != null) ? "\n[currently: " + (target1 != null ? target1.Name:string.Empty) + ((target1 != null && target2 != null)?" and ":string.Empty) + (target2!=null?target2.Name:string.Empty) + "]" : string.Empty;

                        AcquireAsset.Name = "Acquire Asset" + addedText;
                        AcquireAsset.Traits.Add(EnvoyTrait);
                        AcquireAsset.Illustration = IllustrationName.CalculateThreats;
                        AcquireAsset.Description = "Make a strike against a foe, if you hit or critically hit, that creature becomes your asset.";
                        StrikeModifiers strikeModifiers = AcquireAsset.StrikeModifiers;
                        strikeModifiers.OnEachTarget = (Func<Creature, Creature, CheckResult, Task>)Delegate.Combine(strikeModifiers.OnEachTarget, async delegate (Creature caster, Creature target, CheckResult checkResult)
                        {
                            if (checkResult >= CheckResult.Success)
                            {
                                if(creature.QEffects.Any(qf=>qf.Name == EnvoyFeats.DIVERSE_SCHEMES))
                                {
                                    if(target1 == null)
                                    {
                                        target1 = target;
                                        target.AddQEffect(AcqAssetQFX);
                                        creature.Battle.CombatLog.Add(new LogLine(2, creature.Name + " has acquired " + target.Name + " as an asset.", "Accquire Asset", creature.Name + " has acquired " + target.Name + " as an asset."));
                                        creature.Occupies.Overhead("Asset Acquired", Color.Yellow);
                                    }
                                    else if (target2 == null)
                                    {
                                        target2 = target;
                                        target.AddQEffect(AcqAssetQFX);
                                        creature.Battle.CombatLog.Add(new LogLine(2, creature.Name + " has acquired " + target.Name + " as an asset.", "Accquire Asset", creature.Name + " has acquired " + target.Name + " as an asset."));
                                        creature.Occupies.Overhead("Asset Acquired", Color.Yellow);
                                    }
                                    else
                                    {
                                        Creature selectedCreature = null;

                                        var yesNoReq = new ConfirmationRequest(creature, "Would you like to Select an asset to replace with " + target + "?", IllustrationName.CalculateThreats, "yes", "no");

                                        var yesNoResult = (await creature.Battle.SendRequest(yesNoReq)).ChosenOption;

                                        if(yesNoResult is ConfirmOption)
                                        { 
                                            AdvancedRequest req = new AdvancedRequest(creature, "which asset would you like to replace?",
                                                new List<Option>() {new CreatureOption(target1,target1.Name,async ()=>{
                                                        selectedCreature = target1; target1=target; 
                                                    },0,true),
                                                new CreatureOption(target2,target2.Name,async ()=>{
                                                        selectedCreature = target2; target2 = target; 
                                                    },0,true),
                                                new CancelOption(false)});

                                            //req.PassByButtonText = "passing";
                                            //alreadyAsked = true;
                                            var swapResult = (await creature.Battle.SendRequest(req)).ChosenOption;
                                            if (swapResult is not CancelOption)
                                            {
                                                await swapResult.Action();
                                                selectedCreature.RemoveAllQEffects(qf => qf.Name == AcqAssetQFX.Name && qf.Source == creature);
                                                target.AddQEffect(AcqAssetQFX);
                                                creature.Battle.CombatLog.Add(new LogLine(2, creature.Name + " has acquired " + target.Name + " as an asset.", "Accquire Asset", creature.Name + " has acquired " + target.Name + " as an asset."));
                                                creature.Occupies.Overhead("Asset Acquired", Color.Yellow);
                                            }
                                        }
                                    }

                                }
                                else if (!target.QEffects.Any(qf => qf.Name == AcqAssetQFX.Name && qf.Source == AcqAssetQFX.Source))
                                {
                                    bool accquire = false;
                                    if (target1 == null)
                                    {
                                        accquire = true;
                                    }
                                    else
                                    {
                                        var yesNoReq = new ConfirmationRequest(creature, "Would you like to replace your acquired asset with " + target + "?", IllustrationName.CalculateThreats, "yes", "no");

                                        var yesNoResult = (await creature.Battle.SendRequest(yesNoReq)).ChosenOption;

                                        accquire = yesNoResult is ConfirmOption;
                                    }
                                    if (accquire)
                                    {
                                        target1?.RemoveAllQEffects(qf => qf.Name == AcqAssetQFX.Name && qf.Source == creature);
                                        target1 = target;
                                        target.AddQEffect(AcqAssetQFX);
                                        creature.Battle.CombatLog.Add(new LogLine(2, creature.Name + " has acquired " + target.Name + " as an asset.", "Accquire Asset", creature.Name + " has acquired " + target.Name + " as an asset."));
                                        creature.Occupies.Overhead("Asset Acquired", Color.Yellow);
                                    }
                                }
                            }
                        });
                        return AcquireAsset;
                    },
                    BonusToSkillChecks = (usedSkill, combatAction, target) =>
                    {
                        if ((combatAction?.ActionId == ActionId.Seek || usedSkill == Skill.Deception || usedSkill == Skill.Diplomacy || usedSkill == Skill.Intimidation)
                        && target != null && target.QEffects.Any(qf => qf.Name == ACQUIRED_ASSET && qf.Source == creature))
                        {
                            return new Bonus(1, BonusType.Circumstance, "Acquired Asset",true);
                        }
                        return null;
                    },
                    BonusToDefenses = (qfself, action, dfence) =>
                    {
                        if(action!= null && action.Owner.QEffects.Any(qf => qf.Name == AcqAssetQFX.Name && qf.Source == AcqAssetQFX.Source) && dfence == Defense.Perception)
                        {
                            return new Bonus(1, BonusType.Circumstance, "Acquired Asset");
                        }
                        return null;
                    }
                });
        }

        private static void SetupGetEm(Creature creature)
        {
            QEffect generateGottenByExample(Creature targetOfGetem)
            {
                return new QEffect("Shown how to Get 'Em!", "A friendly envoy attacked an enemy they used Get 'Em! on. You get a +1 circumstance bonus to damage against them", ExpirationCondition.ExpiresAtStartOfSourcesTurn, creature, IllustrationName.Fist)
                {
                    BonusToDamage = (qfSelf, action, target) =>
                    {
                        if (target.QEffects.Any(qf => qf.Source == qfSelf.Source))
                        {
                            return new Bonus(1, BonusType.Circumstance, "Get 'Em! by " + creature.Name);
                        }
                        return null;
                    }
                };
            }
            

            creature.AddQEffect(
                new QEffect()
                {
                    ProvideMainAction = (qfSelf)=>
                    {
                        SubmenuPossibility DirectiveSubmenu = new SubmenuPossibility(IllustrationName.Command, DIRECTIVES_SUBMENU_CAPTION);

                        PossibilitySection directiveSection = new PossibilitySection(DIRECTIVES_POSSIBILITY_SECTION_NAME);

                        CombatAction GetEm = new CombatAction(creature, IllustrationName.HuntPrey, "Get 'Em!", new Trait[] { Directive, EnvoyTrait, Trait.Visual }, "Select a creature within 60 feet that you can see. That creature gets a -1 circumstance penalty to their AC until the beginning of your next turn." +
                                "\n{b}Lead by Example{/b} If you attack the Get 'Em target before the end of your turn, you and your allies gain a +1 circumstance bonus to damage rolls against the target.", Target.RangedCreature(12).WithAdditionalConditionOnTargetCreature((caster, target) => target.FriendOf(caster) ? Usability.NotUsableOnThisCreature("ally") : Usability.Usable)).WithActionCost(1)
                        .WithEffectOnEachTarget(async (spell, caster, target, result) =>
                        {
                            target.AddQEffect(new QEffect(GET_ME_NAME, "target of a Get 'Em! action. -1 circumstance penalty to AC", ExpirationCondition.ExpiresAtStartOfSourcesTurn, creature, IllustrationName.HuntPrey)
                            {
                                BonusToDefenses = (qfself,action,dfence) =>
                                {
                                    if(dfence == Defense.AC)
                                    {
                                        return new Bonus(-1, BonusType.Circumstance, "Get 'Em", false);
                                    }
                                    return null;
                                }
                            });

                            target.AddQEffect(new QEffect(ExpirationCondition.ExpiresAtEndOfAnyTurn)
                            {
                                AfterYouAreTargeted = async (qfSelf, action) =>
                                {
                                    if (action.HasTrait(Trait.Attack) && action.Owner == creature)
                                    {
                                        foreach(Creature friend in creature.Battle.AllCreatures.Where(c=>c.FriendOf(creature)))
                                        {
                                            if (!friend.QEffects.Any(qf => qf.Name.Contains("Shown how to Get 'Em!") && qf.Owner == creature))
                                            {
                                                friend.AddQEffect(generateGottenByExample(target));
                                            }
                                        }
                                    }
                                }
                            });
                        });

                        directiveSection.AddPossibility(new ActionPossibility(GetEm));

                        DirectiveSubmenu.Subsections.Add(directiveSection);

                        return DirectiveSubmenu;
                    }
                }
                );
        }

        private static IEnumerable<Feat> GenerateEnvoySubclasses()
        {
            yield return new EnvoyLeadershipStyleFeat(FROM_THE_FRONT, "You inspire your allies by leading them head first, allowing your confidence to infect others, and making them feel under your protection."
                , "You are trained in Athletics and in medium armor.", new List<Trait>(), null).WithOnSheet(delegate (CalculatedCharacterSheetValues sheet)
                {
                    sheet.GrantFeat(FeatName.Athletics);
                    sheet.GrantFeat(FeatName.ArmorProficiencyMedium);
                });
            yield return new EnvoyLeadershipStyleFeat(FROM_THE_SHADOWS, "You direct your team from the shadows, they can fight in confidence, knowing you have their back."
                , "You are trained in Stealth. When you succesfully hide from all enemies, you also become undetected.", new List<Trait>(), null)
                .WithOnSheet(delegate (CalculatedCharacterSheetValues sheet)
                {
                    sheet.GrantFeat(FeatName.Stealth);
                }).WithOnCreature((sheet, creature) =>
                {
                    creature.AddQEffect(new QEffect()
                    {
                        StateCheckWithVisibleChanges = async (qfSelf) =>
                        {
                            if(!creature.Battle.AllCreatures.Where(c => !c.FriendOf(creature)).Any(innerC => !creature.DetectionStatus.EnemiesYouAreHiddenFrom.Contains(innerC)))
                            {
                                creature.DetectionStatus.Undetected = true;
                            }
                        }
                    });

                });
            yield return new EnvoyLeadershipStyleFeat(GUNS_BLAZING, "Like a certain infamous smuggler, you shoot first! Inspire allies into senseless violence!"
                , "You are trained in Acrobatics. You get the Incredible Initiative feat", new List<Trait>(), null).WithOnSheet(delegate (CalculatedCharacterSheetValues sheet)
                {
                    sheet.GrantFeat(FeatName.Acrobatics);
                    sheet.GrantFeat(FeatName.IncredibleInitiative);
                });
        }

        private static void TryThroughDesperateTimes(Feat Envoy)
        {
            Feat BM = AllFeats.All.First(f => f.CustomName != null && f.CustomName.Contains("Battle Medicine"));
            if(BM != null)
            {
                Envoy.Subfeats.Add(
                    new EnvoyLeadershipStyleFeat(THROUGH_DESPERATE_TIMES, "You are there to back everyone up, keeping them alive with your medical knowledge"
                , "You are trained in Acrobatics. You get the Incredible Initiative feat", new List<Trait>(), null).WithOnSheet(delegate (CalculatedCharacterSheetValues sheet)
                {
                    sheet.GrantFeat(FeatName.Medicine);
                    sheet.AddFeat(BM,null);
                }));
            }
        }

        private static Feat CreateWiseToTheGameFeat()
        {
            return new Feat(FeatName.CustomFeat, "Wise to the Game", "You gain a +1 status bonus to your Perception DC against Feint or Create a Diversion and +1 status bonus to Will DC against Demoralize. These bonuses are +2 against your asset", new List<Trait>(), new List<Feat>()).WithOnCreature((creature) =>
            {
                creature.AddQEffect(new QEffect("Wise to the Game", "You gain a +1 status bonus to your Perception DC against Feint or Create a Diversion and +1 status bonus to Will DC against Demoralize. These bonuses are +2 against your asset")
                {
                    BonusToDefenses = (qfSelf,action,defense) =>
                    {
                        if(action == null)
                        {
                            return null;
                        }
                        if (action.Name == "Feint" || action.ActionId == ActionId.CreateADiversion || action.ActionId == ActionId.Demoralize)
                        {
                            if (action.Owner.QEffects.Any(qf => qf.Source == creature && qf.Name == ACQUIRED_ASSET))
                            {
                                return new Bonus(2, BonusType.Status, "Wise to the Game (against Acquired Asset)");
                            }
                            return new Bonus(1, BonusType.Status, "Wise to the Game");
                        }
                        return null;
                    }
                });
            }).WithCustomName("Wise to the Game");
        }
    }
}
