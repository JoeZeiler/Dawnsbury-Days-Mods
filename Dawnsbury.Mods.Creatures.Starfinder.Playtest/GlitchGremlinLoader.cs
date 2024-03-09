//using Dawnsbury.Audio;
//using Dawnsbury.Campaign.Encounters;
//using Dawnsbury.Core.Animations;
//using Dawnsbury.Core.CombatActions;
//using Dawnsbury.Core.Creatures.Parts;
//using Dawnsbury.Core.Creatures;
//using Dawnsbury.Core.Mechanics.Core;
//using Dawnsbury.Core.Mechanics.Enumerations;
//using Dawnsbury.Core.Mechanics.Targeting.Targets;
//using Dawnsbury.Core.Mechanics.Targeting;
//using Dawnsbury.Core.Mechanics.Treasure;
//using Dawnsbury.Core.Mechanics;
//using Dawnsbury.Core;
//using Dawnsbury.Display.Illustrations;
//using Dawnsbury.Modding;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Dawnsbury.Mods.Creatures.Starfinder.Playtest
//{
//    public class GlitchGremlinLoader
//    {
//        public static Trait Fey;
//        public static Trait Gremlin;
//        public static Trait Tech;
//        public static Trait Tiny;

//        [DawnsburyDaysModMainMethod]
//        public static void LoadMod()
//        {
//            Tech = ModManager.RegisterTrait("Tech", new TraitProperties("Tech", true, "Incorporates electronics, computer systems, and power sources.", true));
//            Fey = ModManager.RegisterTrait("Fey", new TraitProperties("Fey", true, "", true));
//            Gremlin = ModManager.RegisterTrait("Gremlin", new TraitProperties("Gremlin", true, "", true));
//            Tiny = ModManager.RegisterTrait("Tiny", new TraitProperties("Tiny", true, "", true));
//            //ModManager.RegisterNewSpell("GGThunderstrike")
//        }

//        private static Creature CreateGlitchGremlin(Encounter encounter)
//        {
//            var GlitchGremlinIllustration = new ModdedIllustration(@"StarfinderCreaturesResources\ComputerGlitchGremlinToken.png");
//            var newCreature = new Creature(GlitchGremlinIllustration, "Computer Glitch Gremlin", new[] { Trait.Homebrew, Tech, Fey, Gremlin, Tiny }, -1, 5, 4,
//                                    new Defenses(14, 5, 8, 6), 8,
//                                    new Abilities(0, 2, 1, 3, 1, 0),
//                                    new Skills(acrobatics: 4, athletics: 3, arcana: 8, crafting: 5, stealth: 4)).WithProficiency(Trait.Unarmed, Proficiency.Expert);
//            newCreature.AnimationData.SizeMultiplier = 0.4F;

//            newCreature.AddQEffect(QEffect.DamageResistance(DamageKind.Cold, 1));
//            newCreature.AddQEffect(QEffect.DamageResistance(DamageKind.Electricity, 1));
//            newCreature.AddQEffect(QEffect.DamageWeakness(DamageKind.Fire, 2));
//            newCreature.AddQEffect(new QEffect("Cold Iron Weakness", "")
//            {
//                Name = "Cold Iron Weakness",
//                Value = 2
//            });

//            var UnstableSparkAttack = new Item(IllustrationName.FireRay, "muzzle beam", new[] { Trait.Ranged, Trait.Unarmed, Trait.Weapon, Trait.Agile, Trait.Electricity, Trait.Magical })
//            {
//                WeaponProperties = new WeaponProperties("1d4", DamageKind.Electricity)
//                {
//                    ItemBonus = 2,
//                    VfxStyle = new VfxStyle(15, ProjectileKind.Arrow, IllustrationName.LightningBolt),
//                    Sfx = SfxName.ElectricArc

//                }.WithAdditionalDamage("1", DamageKind.Electricity).WithRangeIncrement(4),
//            };

//            newCreature.WithUnarmedStrike(UnstableSparkAttack);

//            var biteAttack = new Item(IllustrationName.Jaws, "bite", new[] { Trait.Melee, Trait.Unarmed, Trait.Agile, Trait.Finesse, Trait.Magical })
//            {
//                WeaponProperties = new WeaponProperties("1d4", DamageKind.Piercing)
//                {
//                    ItemBonus = 1,
//                    Sfx = SfxName.ScratchFlesh
//                }.WithAdditionalDamage("2", DamageKind.Piercing)
//            };

//            newCreature.WithAdditionalUnarmedStrike(biteAttack);

//            //newCreature.WithS

//            return newCreature;
//        }
//    }
//}
