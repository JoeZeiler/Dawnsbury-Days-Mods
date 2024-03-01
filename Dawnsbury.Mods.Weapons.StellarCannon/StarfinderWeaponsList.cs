using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dawnsbury.Mods.Weapons.StarfinderWeapons
{
    public partial class StarfinderWeaponsLoader
    {
        #region Stellar Cannon
        /// <summary>
        /// creates the base stellar cannon
        /// </summary>
        /// <param name="iName">input from the game</param>
        /// <returns>the stellar cannon item</returns>
        private static Item CreateStellarCannon(ItemName iName)
        {
            AreaItem stellarCannon = new AreaItem(iName, IllustrationName.QuestionMark, "Stellar Cannon, Commercial", 0, 4, new[] { Analogue, Unwieldy, Trait.Ranged, Trait.TwoHanded, AreaBurst10ft, AreaTechnical, Trait.Martial, Gun, StarfinderGun })
            {
                BaseGunItemName = "Stellar Cannon, Commercial",
                MainTrait = AreaBurst10ft,
                WeaponProperties = new WeaponProperties("1d10", DamageKind.Piercing) { ItemBonus = 0 }.WithRangeIncrement(10),
                Reload = 1,
                Capacity = 8,
                Usage = 2,
                AreaType = AreaItem.AreaTypes.Burst,
                AreaRange = 2,
                EphemeralItemProperties = new EphemeralAreaProperties() { CurrentMagazine = 8 },
            };

            if (StellarCannonIllustration != null)
            {
                stellarCannon.Illustration = StellarCannonIllustration;
            }
            return stellarCannon;
        }

        /// <summary>
        /// creates the tactical stellar cannnon
        /// </summary>
        /// <param name="iName">input from the game</param>
        /// <returns>the stellar cannon item</returns>
        private static Item CreateTacticalStellarCannon(ItemName iName)
        {
            AreaItem stellarCannon = new AreaItem(iName, IllustrationName.QuestionMark, "Stellar Cannon, Tactical", 2, 39, new[] { Analogue, Unwieldy, Trait.Ranged, Trait.TwoHanded, AreaBurst10ft, AreaTechnical, Trait.Martial, Gun, StarfinderGun })
            {
                BaseGunItemName = "Stellar Cannon, Tactical",
                MainTrait = AreaBurst10ft,
                WeaponProperties = new WeaponProperties("1d10", DamageKind.Piercing) { ItemBonus = 1 }.WithRangeIncrement(10),
                Reload = 1,
                Capacity = 12,
                Usage = 2,
                AreaType = AreaItem.AreaTypes.Burst,
                AreaRange = 2,
                EphemeralItemProperties = new EphemeralAreaProperties() { CurrentMagazine = 12 },
            };

            if (StellarCannonIllustration != null)
            {
                stellarCannon.Illustration = StellarCannonIllustration;
            }
            return stellarCannon;
        }

        /// <summary>
        /// creates the advanced stellar cannnon
        /// </summary>
        /// <param name="iName">input from the game</param>
        /// <returns>the stellar cannon item</returns>
        private static Item CreateAdvancedStellarCannon(ItemName iName)
        {
            AreaItem stellarCannon = new AreaItem(iName, IllustrationName.QuestionMark, "Stellar Cannon, Advanced", 4, 104, new[] { Analogue, Unwieldy, Trait.Ranged, Trait.TwoHanded, AreaBurst10ft, AreaTechnical, Trait.Martial, Gun, StarfinderGun })
            {
                BaseGunItemName = "Stellar Cannon, Advanced",
                MainTrait = AreaBurst10ft,
                WeaponProperties = new WeaponProperties("2d10", DamageKind.Piercing) { ItemBonus = 1 }.WithRangeIncrement(10),
                Reload = 1,
                Capacity = 16,
                Usage = 4,
                AreaType = AreaItem.AreaTypes.Burst,
                AreaRange = 2,
                EphemeralItemProperties = new EphemeralAreaProperties() { CurrentMagazine = 16 },
            };

            if (StellarCannonIllustration != null)
            {
                stellarCannon.Illustration = StellarCannonIllustration;
            }
            return stellarCannon;
        }
        #endregion

        #region Scattergun
        /// <summary>
        /// creates the base scattergun
        /// </summary>
        /// <param name="iName">input from the game</param>
        /// <returns>the scattergun item</returns>
        private static Item CreateScattergun(ItemName iName)
        {
            AreaItem scattergun = new AreaItem(iName, IllustrationName.QuestionMark, "Scattergun, Commercial", 0, 4, new[] { Analogue, Unwieldy, Trait.Ranged, Trait.TwoHanded, AreaCone, AreaTechnical, Trait.Simple, Concussive, Gun, StarfinderGun })
            {
                BaseGunItemName = "Scattergun, Commercial",
                MainTrait = AreaCone,
                WeaponProperties = new WeaponProperties("1d6", DamageKind.Piercing) { ItemBonus = 0 }.WithRangeIncrement(3),
                Reload = 1,
                Capacity = 4,
                Usage = 1,
                AreaType = AreaItem.AreaTypes.Cone,
                AreaRange = 3,
                EphemeralItemProperties = new EphemeralAreaProperties() { CurrentMagazine = 4 },
            };

            if (ScattergunIllustration != null)
            {
                scattergun.Illustration = ScattergunIllustration;
            }
            return scattergun;
        }

        /// <summary>
        /// creates the tactical scattergun
        /// </summary>
        /// <param name="iName">input from the game</param>
        /// <returns>the scattergun item</returns>
        private static Item CreateTacticalScattergun(ItemName iName)
        {
            AreaItem scattergun = new AreaItem(iName, IllustrationName.QuestionMark, "Scattergun, Tactical", 2, 39, new[] { Analogue, Unwieldy, Trait.Ranged, Trait.TwoHanded, AreaCone, AreaTechnical, Trait.Simple, Concussive, Gun, StarfinderGun })
            {
                BaseGunItemName = "Scattergun, Tactical",
                MainTrait = AreaCone,
                WeaponProperties = new WeaponProperties("1d6", DamageKind.Piercing) { ItemBonus = 1 }.WithRangeIncrement(3),
                Reload = 1,
                Capacity = 6,
                Usage = 1,
                AreaType = AreaItem.AreaTypes.Cone,
                AreaRange = 3,
                EphemeralItemProperties = new EphemeralAreaProperties() { CurrentMagazine = 6 },
            };

            if (ScattergunIllustration != null)
            {
                scattergun.Illustration = ScattergunIllustration;
            }
            return scattergun;
        }

        /// <summary>
        /// creates the advanced scattergun
        /// </summary>
        /// <param name="iName">input from the game</param>
        /// <returns>the scattergun item</returns>
        private static Item CreateAdvancedScattergun(ItemName iName)
        {
            AreaItem scattergun = new AreaItem(iName, IllustrationName.QuestionMark, "Scattergun, Advanced", 4, 104, new[] { Analogue, Unwieldy, Trait.Ranged, Trait.TwoHanded, AreaCone, AreaTechnical, Trait.Simple, Concussive, Gun, StarfinderGun })
            {
                BaseGunItemName = "Scattergun, Advanced",
                MainTrait = AreaCone,
                WeaponProperties = new WeaponProperties("2d6", DamageKind.Piercing) { ItemBonus = 1 }.WithRangeIncrement(3),
                Reload = 1,
                Capacity = 8,
                Usage = 2,
                AreaType = AreaItem.AreaTypes.Cone,
                AreaRange = 3,
                EphemeralItemProperties = new EphemeralAreaProperties() { CurrentMagazine = 8 },
            };

            if (ScattergunIllustration != null)
            {
                scattergun.Illustration = ScattergunIllustration;
            }
            return scattergun;
        }
        #endregion

        #region Flame Pistol
        /// <summary>
        /// creates the base flame pistol
        /// </summary>
        /// <param name="iName">input from the game</param>
        /// <returns>the flame pistol item</returns>
        private static Item CreateFlamePistol(ItemName iName)
        {
            AreaItem flamePistol = new AreaItem(iName, IllustrationName.QuestionMark, "Flame Pistol, Commercial", 0, 2, new[] { Analogue, Unwieldy, Trait.Ranged, AreaLine, AreaTechnical, Trait.Simple, Gun, StarfinderGun })
            {
                BaseGunItemName = "Flame Pistol, Commercial",
                MainTrait = AreaCone,
                WeaponProperties = new WeaponProperties("1d6", DamageKind.Fire) { ItemBonus = 0 }.WithRangeIncrement(3),
                Reload = 1,
                Capacity = 2,
                Usage = 1,
                AreaType = AreaItem.AreaTypes.Line,
                AreaRange = 3,
                EphemeralItemProperties = new EphemeralAreaProperties() { CurrentMagazine = 2 },
            };

            if (FlamePistolIllustration != null)
            {
                flamePistol.Illustration = FlamePistolIllustration;
            }
            return flamePistol;
        }

        /// <summary>
        /// creates the tactical flame pistol
        /// </summary>
        /// <param name="iName">input from the game</param>
        /// <returns>the flame pistol item</returns>
        private static Item CreateTacticalFlamePistol(ItemName iName)
        {
            AreaItem flamePistol = new AreaItem(iName, IllustrationName.QuestionMark, "Flame Pistol, Tactical", 2, 37, new[] { Analogue, Unwieldy, Trait.Ranged, AreaLine, AreaTechnical, Trait.Simple, Gun, StarfinderGun })
            {
                BaseGunItemName = "Flame Pistol, Tactical",
                MainTrait = AreaCone,
                WeaponProperties = new WeaponProperties("1d6", DamageKind.Fire) { ItemBonus = 1 }.WithRangeIncrement(3),
                Reload = 1,
                Capacity = 3,
                Usage = 1,
                AreaType = AreaItem.AreaTypes.Line,
                AreaRange = 3,
                EphemeralItemProperties = new EphemeralAreaProperties() { CurrentMagazine = 3 },
            };

            if (FlamePistolIllustration != null)
            {
                flamePistol.Illustration = FlamePistolIllustration;
            }
            return flamePistol;
        }

        /// <summary>
        /// creates the advanced flame pistol
        /// </summary>
        /// <param name="iName">input from the game</param>
        /// <returns>the flame pistol item</returns>
        private static Item CreateAdvancedFlamePistol(ItemName iName)
        {
            AreaItem flamePistol = new AreaItem(iName, IllustrationName.QuestionMark, "Flame Pistol, Advanced", 4, 102, new[] { Analogue, Unwieldy, Trait.Ranged, AreaLine, AreaTechnical, Trait.Simple, Gun, StarfinderGun })
            {
                BaseGunItemName = "Flame Pistol, Advanced",
                MainTrait = AreaCone,
                WeaponProperties = new WeaponProperties("2d6", DamageKind.Fire) { ItemBonus = 1 }.WithRangeIncrement(3),
                Reload = 1,
                Capacity = 4,
                Usage = 2,
                AreaType = AreaItem.AreaTypes.Line,
                AreaRange = 3,
                EphemeralItemProperties = new EphemeralAreaProperties() { CurrentMagazine = 4 },
            };

            if (FlamePistolIllustration != null)
            {
                flamePistol.Illustration = FlamePistolIllustration;
            }
            return flamePistol;
        }
        #endregion

        /// <summary>
        /// creates the rotolaser
        /// </summary>
        /// <param name="iName">input from the game</param>
        /// <returns>the rotolaser item</returns>
        private static Item CreateRotolaser(ItemName iName)
        {
            AreaItem rotolaser = new AreaItem(iName, IllustrationName.QuestionMark, "Rotolaser", 0, 6, new[] { Automatic, Tech, Trait.Ranged, Trait.Martial, Trait.Weapon, Trait.TwoHanded, Gun, StarfinderGun })
            {
                BaseGunItemName = "Rotolaser",
                MainTrait = Trait.Weapon,
                WeaponProperties = new WeaponProperties("1d8", DamageKind.Fire).WithRangeIncrement(6),
                Reload = 1,
                Capacity = 10,
                CommercialCapacity = 10,
                TacticalCapacity = 20,
                AdvancedCapacity = 20,
                Usage = 1,
                CommercialUsage = 1,
                TacticalUsage = 1,
                AdvancedUsage = 2,
                CommercialRange = 6,
                TacticalRange = 8,
                AdvancedRange = 8,
                AreaType = AreaItem.AreaTypes.Line,
                AreaRange = 3,
                EphemeralItemProperties = new EphemeralAreaProperties() { CurrentMagazine = 10 },
            };

            if (RotolaserIllustration != null)
            {
                rotolaser.Illustration = RotolaserIllustration;
            }
            return rotolaser;
        }

        /// <summary>
        /// creates the laser pistol
        /// </summary>
        /// <param name="iName">input from the game</param>
        /// <returns>the laser pistol item</returns>
        private static Item CreateLaserPistol(ItemName iName)
        {
            GunItem laserPistol = new GunItem(iName, IllustrationName.QuestionMark, "Laser Pistol", 0, 3, new[] { Tech, Trait.Ranged, Trait.Simple, Trait.Weapon, Gun, StarfinderGun })
            {
                BaseGunItemName = "Laser Pistol",
                MainTrait = Trait.Weapon,
                WeaponProperties = new WeaponProperties("1d6", DamageKind.Fire).WithRangeIncrement(8),
                Reload = 1,
                Capacity = 5,
                CommercialCapacity = 5,
                TacticalCapacity = 10,
                AdvancedCapacity = 10,
                Usage = 1,
                CommercialUsage = 1,
                TacticalUsage = 1,
                AdvancedUsage = 1,
                CommercialRange = 8,
                TacticalRange = 8,
                AdvancedRange = 8,
                EphemeralItemProperties = new EphemeralAreaProperties() { CurrentMagazine = 10 },
            };

            if (LaserPistolIllustration != null)
            {
                laserPistol.Illustration = LaserPistolIllustration;
            }
            return laserPistol;
        }
    }
}
