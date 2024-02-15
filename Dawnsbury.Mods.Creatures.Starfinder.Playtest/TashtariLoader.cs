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
using MapReaderUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Possibilities;

namespace Dawnsbury.Mods.Creatures.Starfinder.Playtest
{
    public class TashtariLoader
    {
        public static Trait TashtariTechnical;
        private static TiledMapData currentMap = null;
        /// <summary>
        /// loads the starfinder soldier mod. the Starfinder Weapons mod is a dependency
        /// </summary>
        [DawnsburyDaysModMainMethod]
        public static void LoadMod()
        {
            TashtariTechnical = ModManager.RegisterTrait("TashtariTechnical", new TraitProperties("TashtariTechnical", false));
            ModManager.RegisterActionOnEachCreature(UpdateCreatureToTashtari);
        }


        private static Action<Creature> UpdateCreatureToTashtari = (Creature creature) =>
        {
            if(creature.Occupies == null || creature.HasTrait(Trait.Homebrew))
            {
                return;
            }
            if(currentMap == null || currentMap.MapName != creature.Battle.Encounter.MapFilename)
            {
                currentMap = new TiledMapData(creature.Battle.Encounter.MapFilename);
            }
            if (currentMap.GetValueAtLocationForLayer("Tashtari",creature.Occupies.X,creature.Occupies.Y) != 0)// && creature.Battle.Encounter.MapFilename.ToLower().Contains("starfinder"))
            {
                var oldOccupies = creature.Occupies;
                var oldBattle = creature.Battle;
                var oldFaction = creature.OwningFaction;
                oldBattle.RemoveCreatureFromGame(creature);

                var newCreature = new Creature(IllustrationName.BloodWolf256, "Tashtari", new[] { Trait.Beast, Trait.Homebrew, TashtariTechnical }, 3, 10, 7, 
                                        new Core.Creatures.Parts.Defenses(19, 8, 11, 6), 42, 
                                        new Core.Creatures.Parts.Abilities(2, 4, 0, 1, 0, -1), 
                                        new Core.Creatures.Parts.Skills(acrobatics: 11, athletics: 9, stealth: 11));
                newCreature.WeaknessAndResistance.AddResistance(DamageKind.Fire, 5);

                var fireRayAttack = new Core.Mechanics.Treasure.Item(IllustrationName.FireRay, "muzzle beam", new[] { Trait.Ranged, Trait.Unarmed, Trait.Weapon, Trait.Fire })
                {
                    WeaponProperties = new Core.Mechanics.Treasure.WeaponProperties("1d8", DamageKind.Fire)
                    {
                        ItemBonus = 1,

                    }.WithAdditionalDamage("4", DamageKind.Fire).WithAdditionalPersistentDamage("1d4", DamageKind.Fire).WithRangeIncrement(12),
                };

                newCreature.WithUnarmedStrike(fireRayAttack);

                var jawsAttack = new Core.Mechanics.Treasure.Item(IllustrationName.Jaws, "jaws", new[] { Trait.Melee, Trait.Unarmed, Trait.Weapon })
                {
                    WeaponProperties = new Core.Mechanics.Treasure.WeaponProperties("1d8", DamageKind.Piercing)
                    {
                        ItemBonus = 1,
                    }
                };

                newCreature.WithAdditionalUnarmedStrike(jawsAttack);

                int numberOfCreatures = 0;
                bool flashUsed = false;

                var BristleFlashTargeting = Target.SelfExcludingEmanation(8).WithIncludeOnlyIf((t, c) => !c.HasTrait(TashtariTechnical)) as EmanationTarget;
                if (BristleFlashTargeting != null)
                {
                    BristleFlashTargeting.CreatureGoodness = (targeting, user, target) =>
                    {
                        if(flashUsed)
                        {
                            return AIConstants.NEVER;
                        }
                        if (!target.FriendOf(user) && !target.QEffects.Any(q=>q.Id == QEffectId.Blinded && q.Name == QEffect.Dazzled().Name))
                        {
                            numberOfCreatures++;
                        }
                        if(target.FriendOf(user) && !target.HasTrait(TashtariTechnical))
                        {
                            numberOfCreatures--;
                        }
                        if(numberOfCreatures >= 2)
                        {
                            return AIConstants.EXTREMELY_PREFERRED;
                        }
                        return AIConstants.NEVER;
                    };
                }

                CombatAction BristleFlash = new CombatAction(newCreature, IllustrationName.DazzlingFlash, "Bristle Flash", new[] { Trait.Visual, Trait.Light },
                    "The tashtari causes its filaments to glow with intense light. " +
                    "Non-tashtaris within a 40-foot emanation must attempt a DC 19 Fortitude save. A creature " +
                    "that attempts this save is immune to all Bristle Flashes for 1 minute. The tashtari’s fur " +
                    "loses its glow, and it can’t use this ability until it basks in sunlight for at least 10 minutes." +
                    "\r\nCritical Success: The creature is unaffected." +
                    "\r\nSuccess: The creature is dazzled for 1 round." +
                    "\r\nFailure The creature is dazzled for 1 minute." +
                    "\r\nCritical Failure The creature is blinded for 1 round and dazzled for 1 minute.", BristleFlashTargeting)
                .WithActionCost(1).WithSavingThrow(new SavingThrow(Defense.Fortitude, (c) => 19))
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
                    numberOfCreatures = 0;
                    flashUsed = true;
                });

                newCreature.AddQEffect(new QEffect("Bristle Flash", "Bright Filaments Glow to blind enemies")
                {
                    ProvideMainAction = (qfself)=>
                    {
                        return new ActionPossibility(BristleFlash);
                    }
                });

                //newCreature.AddQEffect(new QEffect()
                //{
                //    StateCheck = (qfself) =>
                //    {
                //        foreach (Creature c in newCreature.Battle.AllCreatures)
                //        {
                //            if (newCreature.IsAdjacentTo(c) && newCreature.EnemyOf(c))
                //            {
                //                newCreature.ReplacementUnarmedStrike = jawsAttack;
                //            }
                //        }
                //    }
                //});

                oldBattle.SpawnCreature(newCreature, oldFaction, oldOccupies);
                
            }
        };

        //private static void AddDistinguishesIfNeeded(Creature thisCreature)
        //{
        //    List<Creature> list = thisCreature.Battle.AllCreatures.Where((Creature cr) => cr.MainName == thisCreature.MainName && cr != thisCreature).ToList();
        //    if (list.Count == 0)
        //    {
        //        return;
        //    }

        //    char c = '\0';
        //    foreach (Creature item in list)
        //    {
        //        if (item.Distinguisher == '\0')
        //        {
        //            item.Distinguisher = 'A';
        //            c = 'A';
        //        }
        //        else if (item.Distinguisher > c)
        //        {
        //            c = item.Distinguisher;
        //        }
        //    }

        //    thisCreature.Distinguisher = (char)(c + 1);
        //}


    }
}
