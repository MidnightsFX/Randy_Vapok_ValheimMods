using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards 
{
    public static class AnchoredBlock 
    {
        public static void Apply(ItemDrop.ItemData __instance, ref float baseBlock) 
        {
            var player = Player.m_localPlayer;
            float penaltyBonusBlock = player.GetTotalActiveMagicEffectValue(MagicEffectType.AnchoredBlock, 1) *
                (PenaltyScaling.MovementPenalty(player) * 100); // Movement Penalty returns .01f positive per -% movement speed.
                                                                // -25% movement speed is .25f so we * 100 to get a round increment per
                                                                // AnchoredBlock value are now decimals .25-1.25
            baseBlock += (penaltyBonusBlock);
        }
    }
}

