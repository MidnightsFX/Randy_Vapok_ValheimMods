using EpicLoot.General;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Red trinket shard: gain a flat amount of adrenaline whenever the local player takes damage. Hooks
    // RPC_Damage (the local player is the victim), so it only fires for hits that survive the other
    // on-damage effects. Uses vanilla's adrenaline pool, so it is inert unless the player has a
    // max-adrenaline source. Shard values are the flat adrenaline amount granted per damaging hit.
    public static class DamageTakenGivesAdrenaline
    {
        // Postfix handler invoked by CharacterRpcDamageDispatch (on-damage-taken reaction).
        public static void OnDamageTaken(Character __instance, HitData hit)
        {
            if (hit == null || __instance != Player.m_localPlayer)
            {
                return;
            }

            var amount = Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                MagicEffectType.DamageTakenGivesAdrenaline);
            if (amount <= 0f)
            {
                return;
            }

            // Flat grant, but only for hits that actually landed damage -- a fully mitigated or
            // zero-damage hit shouldn't build adrenaline.
            if (hit.m_damage.EpicLootGetTotalDamageAgainstPlayer() > 0f)
            {
                Player.m_localPlayer.AddAdrenaline(amount);
            }
        }
    }
}
