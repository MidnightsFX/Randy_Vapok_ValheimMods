namespace EpicLoot.MagicItemEffects.Shards
{
    // Orange trinket shard: dealing fire damage stokes adrenaline — gain adrenaline equal to value% of
    // the fire damage the local player deals. Hooks Character.Damage (runs on the attacker's client) and
    // resolves the attacker from the hit. Uses vanilla's adrenaline pool, so it is inert unless the
    // player has a max-adrenaline source. Shard values are authored as whole-number percents.
    public static class BurningAdrenaline
    {
        // Postfix handler invoked by CharacterDamageDispatch (on-hit reaction).
        public static void OnDamageDealt(HitData hit, Character attacker)
        {
            if (hit == null || hit.m_damage.m_fire <= 0f || attacker != Player.m_localPlayer)
            {
                return;
            }

            var fraction = Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.BurningAdrenaline, 0.01f);
            if (fraction > 0f)
            {
                Player.m_localPlayer.AddAdrenaline(hit.m_damage.m_fire * fraction);
            }
        }
    }
}
