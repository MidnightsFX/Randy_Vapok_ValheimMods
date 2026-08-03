using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards 
{
    public static class BlockFromMovementPenalty 
    {
        public static void Apply(ref float baseBlock) 
        {
            var player = Player.m_localPlayer;
            float penaltyBonusBlock = player.GetTotalActiveMagicEffectValue(MagicEffectType.BurdenedBlock, .01f) *
                PenaltyScaling.MovementPenalty(player);

            baseBlock += (penaltyBonusBlock);
        }
    }
}

