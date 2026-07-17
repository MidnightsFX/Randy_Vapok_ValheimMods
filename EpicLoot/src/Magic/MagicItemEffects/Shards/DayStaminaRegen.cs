namespace EpicLoot.MagicItemEffects.Shards
{
    // White legs shard: +% stamina regen while it is daytime (EnvMan.IsDay()). Invoked from
    // ModifyPlayerRegen's ModifyStaminaRegen postfix. Shard values are authored as whole-number percents.
    public static class DayStaminaRegen
    {
        public static void Apply(SEMan seman, ref float staminaMultiplier)
        {
            if (seman.m_character != Player.m_localPlayer || !EnvMan.IsDay())
            {
                return;
            }

            staminaMultiplier += Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                MagicEffectType.DayStaminaRegen, 0.01f);
        }
    }
}
