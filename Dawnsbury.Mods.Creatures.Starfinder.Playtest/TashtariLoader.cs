using Dawnsbury.Core;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Core.Intelligence;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Campaign.Encounters;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Audio;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.Mechanics.Treasure;

namespace Dawnsbury.Mods.Creatures.Starfinder.Playtest
{
    public class TashtariLoader
    {
        public static Trait TashtariTechnical;
        /// <summary>
        /// loads the starfinder soldier mod. the Starfinder Weapons mod is a dependency
        /// </summary>
        [DawnsburyDaysModMainMethod]
        public static void LoadMod()
        {
            TashtariTechnical = ModManager.RegisterTrait("TashtariTechnical", new TraitProperties("TashtariTechnical", false));
            ModManager.RegisterNewCreature("Tashtari", CreateTashtari);
        }


        private static Creature CreateTashtari(Encounter encounter)
        {
            var tashtariIllustration = new ModdedIllustration(@"StarfinderCreaturesResources\Tashtari.png");
            var newCreature = new Creature(tashtariIllustration, "Tashtari", new[] { Trait.Beast, Trait.Homebrew, TashtariTechnical }, 3, 10, 7, 
                                    new Defenses(19, 8, 11, 6), 42, 
                                    new Abilities(2, 4, 0, 1, 0, -1), 
                                    new Skills(acrobatics: 11, athletics: 9, stealth: 11)).WithProficiency(Trait.Unarmed, Proficiency.Expert);

            newCreature.AddQEffect(QEffect.DamageResistance(DamageKind.Fire, 5));

            var fireRayAttack = new Item(IllustrationName.FireRay, "muzzle beam", new[] { Trait.Ranged, Trait.Unarmed, Trait.Weapon, Trait.Fire })
            {
                WeaponProperties = new WeaponProperties("1d6", DamageKind.Fire)
                {
                    ItemBonus = 1,

                }.WithAdditionalDamage("4", DamageKind.Fire).WithAdditionalPersistentDamage("1d4", DamageKind.Fire).WithRangeIncrement(12),
            };

            newCreature.WithUnarmedStrike(fireRayAttack);

            var jawsAttack = new Item(IllustrationName.Jaws, "jaws", new[] { Trait.Melee, Trait.Unarmed, Trait.Weapon, Trait.Trip })
            {
                WeaponProperties = new WeaponProperties("1d8", DamageKind.Piercing)
                {
                    ItemBonus = 1,
                    VfxStyle = new VfxStyle(15, ProjectileKind.Ray, IllustrationName.FireRay),
                    Sfx = SfxName.FireRay
                }
            };

            newCreature.WithAdditionalUnarmedStrike(jawsAttack);

            
            bool flashUsed = false;


            QEffect TashtariTrip()
            {
                return new QEffect("Trip", "When your Strike hits, you can spend an action to trip without a trip a check", ExpirationCondition.Never, null, IllustrationName.None)
                {
                    Innate = true,
                    ProvideMainAction = delegate (QEffect qfTrip)
                    {
                        Creature tashtari = qfTrip.Owner;
                        IEnumerable<Creature> source = from cr in tashtari.Battle.AllCreatures.Where(delegate (Creature cr)
                        {
                            CombatAction combatAction = tashtari.Actions.ActionHistoryThisTurn.LastOrDefault();
                            return combatAction != null && combatAction.CheckResult >= CheckResult.Success && combatAction.HasTrait(Trait.Trip) && combatAction.ChosenTargets.ChosenCreature == cr;
                        })
                            where !cr.QEffects.Any((QEffect qf) => qf.Id == QEffectId.Prone)
                            select cr;
                        return new SubmenuPossibility(IllustrationName.Trip, "Trip")
                        {
                            Subsections =
                            {
                                new PossibilitySection("Trip")
                                {
                                    Possibilities = source.Select(
                                        (Func<Creature, Possibility>)((Creature tripTarget) => 
                                        new ActionPossibility(new CombatAction(tashtari, 
                                            IllustrationName.Trip, 
                                            "Trip " + tripTarget.Name, 
                                            new Trait[1] { Trait.Melee }, 
                                            "Trip the target.", 
                                            Target.Melee((Target t, Creature a, Creature d) => (!d.HasEffect(QEffectId.Unconscious)) ? 1.07374182E+09f : (-2.14748365E+09f))
                                            .WithAdditionalConditionOnTargetCreature((Creature a, Creature d) => (d != tripTarget) ? Usability.CommonReasons.TargetIsNotPossibleForComplexReason : Usability.Usable))
                                            .WithEffectOnEachTarget(async delegate(CombatAction ca, Creature a, Creature d, CheckResult cr)
                                                {
                                                    await tripTarget.FallProne();
                                                })
                                            )
                                        )
                                    ).ToList()
                                }
                            }
                        };
                    }
                };
            };

            newCreature.AddQEffect(TashtariTrip());

            var BristleFlashTargeting = Target.SelfExcludingEmanation(8).WithIncludeOnlyIf((t, c) => !c.HasTrait(TashtariTechnical)) as EmanationTarget;

            var brislteGoodness = (Target targeting, Creature caster, Creature targetCreature) =>
            {
                int numberOfCreatures = 0;
                foreach (Creature creature in caster.Battle.AllCreatures.Where(c => c.DistanceTo(caster) <= 8))
                {
                    if (flashUsed)
                    {
                        return AIConstants.NEVER;
                    }
                    if (!creature.FriendOf(caster) && !creature.QEffects.Any(q => q.Id == QEffectId.Blinded || q.Name == QEffect.Dazzled().Name))
                    {
                        numberOfCreatures++;
                    }
                    if (creature.FriendOf(caster) && !creature.HasTrait(TashtariTechnical))
                    {
                        numberOfCreatures--;
                    }
                }
                if (numberOfCreatures >= 2)
                {
                    numberOfCreatures = 0;
                    return AIConstants.EXTREMELY_PREFERRED;
                }
                return AIConstants.NEVER;
            };


            CombatAction BristleFlash = new CombatAction(newCreature, IllustrationName.DazzlingFlash, "Bristle Flash", new[] { Trait.Visual, Trait.Light },
                "The tashtari causes its filaments to glow with intense light. " +
                "Non-tashtaris within a 40-foot emanation must attempt a DC 19 Fortitude save. A creature " +
                "that attempts this save is immune to all Bristle Flashes for 1 minute. The tashtari’s fur " +
                "loses its glow, and it can’t use this ability until it basks in sunlight for at least 10 minutes." +
                "\r\nCritical Success: The creature is unaffected." +
                "\r\nSuccess: The creature is dazzled for 1 round." +
                "\r\nFailure The creature is dazzled for 1 minute." +
                "\r\nCritical Failure The creature is blinded for 1 round and dazzled for 1 minute.", BristleFlashTargeting).WithGoodness(brislteGoodness)
            .WithActionCost(1).WithSavingThrow(new SavingThrow(Defense.Fortitude, (c) => 19)).WithProjectileCone(IllustrationName.DazzlingFlash, 8,ProjectileKind.None).WithSoundEffect(SfxName.SprayPerfume)
            .WithEffectOnEachTarget(async (action, user, target, result) =>
            {
                if(result == CheckResult.CriticalSuccess)
                {
                    return;
                }
                if(result == CheckResult.Success)
                {
                    target.AddQEffect(QEffect.Dazzled().WithExpirationOneRoundOrRestOfTheEncounter(user, false));
                }
                if(result == CheckResult.Failure)
                {
                    target.AddQEffect(QEffect.Dazzled().WithExpirationAtStartOfSourcesTurn(user, 10));
                }
                if( result == CheckResult.CriticalFailure) 
                {
                    target.AddQEffect(QEffect.Blinded().WithExpirationOneRoundOrRestOfTheEncounter(user, false));
                    target.AddQEffect(QEffect.Dazzled().WithExpirationAtStartOfSourcesTurn(user, 10));
                }
                flashUsed = true;
                
            });

            newCreature.AddQEffect(new QEffect()
            {
                ProvideMainAction = (qfself)=>
                {
                    return new ActionPossibility(BristleFlash);
                }
            });
            return newCreature;
        }

    }
}
