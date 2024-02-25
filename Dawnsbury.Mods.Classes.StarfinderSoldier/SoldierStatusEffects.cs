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
        /// <returns>the suppressed effect</returns>
        public static QEffect GenerateSupressedEffect(Creature Supressor)
        {
            QEffect effect = new QEffect("Suppressed", "You have a -1 circumstance penalty to attack rolls and a -10-foot status penalty to your Speed.", ExpirationCondition.CountsDownAtStartOfSourcesTurn, Supressor, IllustrationName.TakeCover)
            {
                BonusToAttackRolls = (qfSelf, combatAction, target) =>
                {
                    if (combatAction.Traits.Contains(Core.Mechanics.Enumerations.Trait.Strike))
                    {
                        return new Core.Mechanics.Core.Bonus(-1, Core.Mechanics.Core.BonusType.Circumstance, "Suppressed", false);
                    }
                    return null;
                },
                BonusToAllSpeeds = (qfSelf) =>
                {
                    return new Core.Mechanics.Core.Bonus(-2, Core.Mechanics.Core.BonusType.Status, "Suppressed", false);
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
