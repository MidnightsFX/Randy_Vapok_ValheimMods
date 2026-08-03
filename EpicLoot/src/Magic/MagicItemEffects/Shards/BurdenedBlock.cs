using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpicLoot.MagicItemEffects.Shards {
    public class BurdenedBlock 
    {
        public static void Apply(ref float baseBlock) 
        {
            var player = Player.m_localPlayer;
            float carriedWeight = player.GetInventory().GetTotalWeight();
            float burdenedBlockIncrement = player.GetTotalActiveMagicEffectValue(MagicEffectType.BurdenedBlock, 1f); // whole numbers flat block

            float burdenedBlockBonus = (int)((carriedWeight - 300f) / 50f);

            baseBlock += (burdenedBlockIncrement * burdenedBlockBonus);
        }
    }
}
