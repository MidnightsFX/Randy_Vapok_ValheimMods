using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to block based on the player's carried weight
    public class BurdenedBlock 
    {
        public static void Apply(ItemDrop.ItemData __instance, ref float baseBlock) 
        {
            var player = Player.m_localPlayer;
            float carriedWeight = player.GetInventory().GetTotalWeight();
            float burdenedBlockBonus = player.GetTotalActiveMagicEffectValue(MagicEffectType.BurdenedBlock, 1f);
            float burdenedBlockIncrement = Math.Max(0, (int)((carriedWeight - 300f) / 50f));

            baseBlock += (burdenedBlockIncrement * burdenedBlockBonus);

        }
    }
}
