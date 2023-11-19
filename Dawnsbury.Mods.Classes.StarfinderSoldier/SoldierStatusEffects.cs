using Dawnsbury.Core;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;

namespace Dawnsbury.Mods.Classes.StarfinderSoldier
{
    /// <summary>
    /// generates status effects the soldier needs
    /// </summary>
    public class SoldierStatusEffects
    {
        /// <summary>
        /// generates the status effect "supressed".
        /// </summary>
        /// <param name="Supressor">the creature suppressing others</param>
        /// <returns>the supressed effect</returns>
        public static QEffect GenerateSupressedEffect(Creature Supressor)
        {
            QEffect effect = new QEffect("Supressed", "-1 circumstance penalty on attack rolls and a -10-foot status penalty to your speed", ExpirationCondition.CountsDownAtStartOfSourcesTurn, Supressor, IllustrationName.TakeCover)
            {
                BonusToAttackRolls = (qfSelf, combatAction, target) =>
                {
                    if (combatAction.Traits.Contains(Core.Mechanics.Enumerations.Trait.Strike))
                    {
                        return new Core.Mechanics.Core.Bonus(-1, Core.Mechanics.Core.BonusType.Circumstance, "Supressed", false);
                    }
                    return null;
                },
                BonusToAllSpeeds = (qfSelf) =>
                {
                    return new Core.Mechanics.Core.Bonus(-2, Core.Mechanics.Core.BonusType.Status, "Supressed", false);
                }
            };

            if(StarfinderSoldierLoader.SuppressedIllustration != null)
            {
                effect.Illustration = StarfinderSoldierLoader.SuppressedIllustration;
            }

            return effect;
        }
    }
}
