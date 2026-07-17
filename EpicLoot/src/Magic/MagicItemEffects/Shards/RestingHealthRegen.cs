namespace EpicLoot.MagicItemEffects.Shards
{
    // LightGreen shoulder shard: +% health regen while the player is Rested. Invoked from ModifyPlayerRegen's
    // ModifyHealthRegen postfix; gated on the vanilla Rested status effect (SEMan.s_statusEffectRested),
    // mirroring DayHealthRegen's day gate. Shard values are authored as whole-number percents.
    public static class RestingHealthRegen
    {
        public static void Apply(SEMan seman, ref float regenMultiplier)
        {
            var player = Player.m_localPlayer;
            if (seman.m_character != player || !seman.HaveStatusEffect(SEMan.s_statusEffectRested))
            {
                return;
            }

            regenMultiplier += player.GetTotalActiveMagicEffectValue(MagicEffectType.RestingHealthRegen, 0.01f);
        }
    }
}
