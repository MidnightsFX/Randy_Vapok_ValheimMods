namespace EpicLoot.MagicItemEffects.Shards
{
    // Peach (Logistics) chest shard: +% stamina regen that scales with how loaded the player's pack is.
    // Invoked from ModifyPlayerRegen's ModifyStaminaRegen postfix. Shard values are authored as
    // whole-number percents, hence the 0.01f.
    public static class StaminaRegenBonusFromPlayerWeight
    {
        public static void Apply(SEMan seman, ref float staminaMultiplier)
        {
            var player = Player.m_localPlayer;
            if (seman.m_character != player)
            {
                return;
            }

            staminaMultiplier += player.GetTotalActiveMagicEffectValue(
                MagicEffectType.StaminaRegenBonusFromPlayerWeight, 0.01f) * PenaltyScaling.WeightFactor(player);
        }
    }
}
