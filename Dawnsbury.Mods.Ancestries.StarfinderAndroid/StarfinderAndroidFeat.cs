using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Mechanics.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dawnsbury.Mods.Ancestries.StarfinderAndroid
{
    public class StarfinderAndroidFeat : TrueFeat
    {
        public StarfinderAndroidFeat(string name, string flavorText, string rulesText) 
            : base(FeatName.CustomFeat, 1, flavorText, rulesText, new[]
            {
                StarfinderAndroidLoader.AndroidTrait,
            })
        {
            this.WithCustomName(name);
        }
    }
}
