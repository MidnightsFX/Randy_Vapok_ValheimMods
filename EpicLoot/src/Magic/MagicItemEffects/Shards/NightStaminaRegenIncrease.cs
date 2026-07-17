namespace EpicLoot.MagicItemEffects.Shards
{
    // Black head shard: +% stamina regen while it is night (EnvMan.IsNight()). Invoked from
    // ModifyPlayerRegen's ModifyStaminaRegen postfix. Shard values are authored as whole-number percents.
    public static class NightStaminaRegenIncrease
    {
        public static void Apply(SEMan seman, ref float staminaMultiplier)
        {
            if (seman.m_character != Player.m_localPlayer || !EnvMan.IsNight())
            {
                return;
            }

            staminaMultiplier += Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                MagicEffectType.NightStaminaRegenIncrease, 0.01f);
        }
    }
}
