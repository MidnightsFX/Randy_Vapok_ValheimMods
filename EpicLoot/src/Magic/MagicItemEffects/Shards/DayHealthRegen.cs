namespace EpicLoot.MagicItemEffects.Shards
{
    // White trinket shard: +% health regen while it is daytime (EnvMan.IsDay()). Invoked from
    // ModifyPlayerRegen's ModifyHealthRegen postfix. Shard values are authored as whole-number percents.
    public static class DayHealthRegen
    {
        public static void Apply(SEMan seman, ref float regenMultiplier)
        {
            if (seman.m_character != Player.m_localPlayer || !EnvMan.IsDay())
            {
                return;
            }

            regenMultiplier += Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                MagicEffectType.DayHealthRegen, 0.01f);
        }
    }
}
