using Dawnsbury.Core;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;

namespace Dawnsbury.Mods.Classes.StarfinderSoldier
{
    public class SoldierStatusEffects
    {
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
            return effect;
        }
    }
}
