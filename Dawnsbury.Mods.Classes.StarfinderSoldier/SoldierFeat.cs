﻿using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Mechanics.Enumerations;
using System.Collections.Generic;

namespace Dawnsbury.Mods.Classes.StarfinderSoldier
{
    /// <summary>
    /// feat for the soldier
    /// </summary>
    public class SoldierFeat : TrueFeat
    {
        public SoldierFeat(string name, int level, string flavorText, string rulesText, Trait[] traits, List<Feat> subfeats = null) : base(FeatName.CustomFeat, level, flavorText, rulesText, traits, subfeats)
        {
            this.WithCustomName(name);
        }
    }

    /// <summary>
    /// feat for a soldier fighting style
    /// </summary>
    public class SoldierFightingStyleFeat : Feat
    {
        public SoldierFightingStyleFeat(string name, string flavorText, string rulesText, List<Trait> traits, List<Feat> subfeats) : base(FeatName.CustomFeat, flavorText, rulesText, traits, subfeats)
        {
            this.WithCustomName(name);
        }
    }
}
