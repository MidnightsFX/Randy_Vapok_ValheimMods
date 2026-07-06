using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // The Queen boss shard: the local player siphons eitr from the enemies they strike — a trickle on every
    // hit and a larger surge on a kill — and the siphon keeps their eitr flowing by clearing the post-cast
    // regen delay so it starts recovering immediately. Hooked on Character.Damage (attacker == local player).
    // The boss-tier shard value (5..25) scales the eitr gained. Inert unless the player has an eitr pool.
    //
    // Kill detection reads the target's health after the hit, which is exact when the player owns the enemy
    // (single-player, or enemies close to the host); against remote-owned enemies it is best-effort.
    public static class EitrSiphon
    {
        private const float EitrPerHitTier = 0.1f;  // eitr per hit, per point of value (5..25 -> 0.5..2.5)
        private const float EitrPerKillTier = 0.5f; // extra eitr on a kill, per point of value (5..25 -> 2.5..12.5)

        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        private static class Damage_Patch
        {
            [UsedImplicitly]
            private static void Postfix(Character __instance, HitData hit)
            {
                if (hit == null || hit.GetAttacker() != Player.m_localPlayer
                    || __instance.IsPlayer() || __instance.IsTamed())
                {
                    return;
                }

                var player = Player.m_localPlayer;
                var value = player.GetTotalActiveMagicEffectValue(MagicEffectType.EitrSiphon, 1f);
                if (value <= 0f || player.GetMaxEitr() <= 0f)
                {
                    return;
                }

                var siphoned = value * EitrPerHitTier;
                if (__instance.GetHealth() <= 0f)
                {
                    siphoned += value * EitrPerKillTier;
                }

                player.AddEitr(siphoned);
                player.m_eitrRegenTimer = 0f; // don't let the siphon stall natural regen
            }
        }
    }
}
