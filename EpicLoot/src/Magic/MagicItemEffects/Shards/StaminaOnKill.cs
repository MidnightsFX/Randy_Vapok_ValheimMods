namespace EpicLoot.MagicItemEffects.Shards
{
    // Yellow shoulder shard: restore a % of max stamina each time the local player kills an enemy. Hooked
    // via SharedCharacterDamagePatch.PostDamagePatch (attacker side); kill detection mirrors QueenEverflow
    // -- the hit must have dropped the target to (or below) zero health. Shard values are authored as
    // whole-number percents, hence the 0.01f.
    public static class StaminaOnKill
    {
        // Postfix handler invoked by CharacterDamageDispatch (on-hit reaction).
        public static void OnDamageDealt(Character __instance, HitData hit, Character attacker)
        {
            var player = Player.m_localPlayer;
            if (hit == null || player == null || __instance == player || attacker != player
                || __instance.IsPlayer() || __instance.IsTamed() || __instance.GetHealth() > 0f)
            {
                return;
            }

            var fraction = player.GetTotalActiveMagicEffectValue(MagicEffectType.StaminaOnKill, 0.01f);
            if (fraction > 0f)
            {
                player.AddStamina(player.GetMaxStamina() * fraction);
            }
        }
    }
}
