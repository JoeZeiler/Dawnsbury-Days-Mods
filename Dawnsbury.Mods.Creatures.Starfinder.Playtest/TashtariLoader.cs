using Dawnsbury.Core;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dawnsbury.Mods.Creatures.Starfinder.Playtest
{
    public class TashtariLoader
    {
        /// <summary>
        /// loads the starfinder soldier mod. the Starfinder Weapons mod is a dependency
        /// </summary>
        [DawnsburyDaysModMainMethod]
        public static void LoadMod()
        {
            ModManager.RegisterActionOnEachCreature(UpdateWolfToTashtari);
        }


        private static Action<Creature> UpdateWolfToTashtari = (Creature creature) =>
        {
            if (creature.MainName.Equals("Blood Wolf"))// && creature.Battle.Encounter.MapFilename.ToLower().Contains("starfinder"))
            {
                creature.MainName = "Tashitari";
                AddDistinguishesIfNeeded(creature);
                creature.RemoveAllQEffects(q => q.Name.Contains("resistance") ||
                                                q.Name.Contains("weakness") ||
                                                q.Name.Contains("Attack of Opportunity") ||
                                                q.Name.Contains("Pack Attack") ||
                                                q.Name.Contains("Aggressive Rush"));
                creature.Defenses.Set(Defense.AC, 19);
                creature.Defenses.Set(Defense.Fortitude, 8);
                creature.Defenses.Set(Defense.Reflex, 11);
                creature.Defenses.Set(Defense.Will, 6);
                creature.Abilities.Strength = 2;
                creature.Abilities.Dexterity = 4;
                creature.Abilities.Constitution = 0;
                creature.Abilities.Intelligence = 1;
                creature.Abilities.Wisdom = 0;
                creature.Abilities.Charisma = -1;
                creature.MaxHP = 42;
                creature.WeaknessAndResistance.AddResistance(DamageKind.Fire, 5);
                creature.BaseSpeed = 7;
                creature.UnarmedStrike = new Core.Mechanics.Treasure.Item(IllustrationName.Jaws, "jaws", new[] {Trait.Melee, Trait.Unarmed, Trait.Weapon })
                {
                    WeaponProperties = new Core.Mechanics.Treasure.WeaponProperties("1d8", DamageKind.Piercing)
                    {
                        ItemBonus = 1,
                    }
                };
            }
            if (creature.BaseName.Equals("Wolf"))
            {
                creature.MainName = "Tashitari Pup";
                AddDistinguishesIfNeeded(creature);
            }
            creature.AddQEffect(new QEffect()
            {
                StateCheck = (qfSelf) =>
                {
                    foreach (Creature c in creature.Battle.AllCreatures.Where(cr => cr.BaseName.Equals("Blood Wolf") || cr.BaseName.Equals("Wolf")))
                    {
                        UpdateWolfToTashtari(c);
                    }
                }
            });
        };

        private static void AddDistinguishesIfNeeded(Creature thisCreature)
        {
            List<Creature> list = thisCreature.Battle.AllCreatures.Where((Creature cr) => cr.MainName == thisCreature.MainName && cr != thisCreature).ToList();
            if (list.Count == 0)
            {
                return;
            }

            char c = '\0';
            foreach (Creature item in list)
            {
                if (item.Distinguisher == '\0')
                {
                    item.Distinguisher = 'A';
                    c = 'A';
                }
                else if (item.Distinguisher > c)
                {
                    c = item.Distinguisher;
                }
            }

            thisCreature.Distinguisher = (char)(c + 1);
        }


    }
}
