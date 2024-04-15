using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Mechanics.Enumerations;
using System.Collections.Generic;

namespace Dawnsbury.Mods.Classes.Starfinder.Envoy
{
    /// <summary>
    /// feat for the Envoy
    /// </summary>
    public class EnvoyFeat : TrueFeat
    {
        public EnvoyFeat(string name, int level, string flavorText, string rulesText, Trait[] traits, List<Feat> subfeats = null) : base(FeatName.CustomFeat, level, flavorText, rulesText, traits, subfeats)
        {
            this.WithCustomName(name);
        }
    }

    /// <summary>
    /// feat for an Envoy Leadership Style
    /// </summary>
    public class EnvoyLeadershipStyleFeat : Feat
    {
        public EnvoyLeadershipStyleFeat(string name, string flavorText, string rulesText, List<Trait> traits, List<Feat> subfeats) : base(FeatName.CustomFeat, flavorText, rulesText, traits, subfeats)
        {
            this.WithCustomName(name);
        }
    }
}
