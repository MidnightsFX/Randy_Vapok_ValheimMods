namespace EpicLoot.MagicItemEffects.Shards 
{
    public static class GainOnBlockResource {
        public static void GainOnBlock(bool IsBlocked) 
        {
            var player = Player.m_localPlayer;
            
            if (!IsBlocked || player.IsDead()) return;

            player.Heal(player.GetTotalActiveMagicEffectValue(MagicEffectType.LifeGainOnBlock, 1f));
            player.AddStamina(player.GetTotalActiveMagicEffectValue(MagicEffectType.StaminaGainOnBlock, 1f));
            player.AddEitr(player.GetTotalActiveMagicEffectValue(MagicEffectType.EitrGainOnBlock, 1f));
        }
    }
}
