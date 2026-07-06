using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Fader boss shard: a last stand. When a hit drops the local player to low health, they erupt in a fire
    // nova — fire damage (and burning) to every nearby enemy — and become briefly immune to fire so the
    // eruption (and whatever set them ablaze) can't finish them off. Both hang off the local player's
    // RPC_Damage: the prefix strips the fire portion of incoming hits while the immunity window is open, and
    // the postfix checks the post-hit health and triggers. The boss-tier shard value (5..25) scales the nova.
    //
    // A long cooldown makes this a panic button, not a persistent aura, even while the player lingers low.
    public static class LastFire
    {
        private const float LowHealthThreshold = 0.25f; // fraction of max health that arms the eruption
        private const float NovaRadius = 8f;
        private const float FirePerTier = 10f;          // fire damage per point of value (5..25 -> 50..250)
        private const float ImmunityDuration = 5f;
        private const float Cooldown = 30f;

        private static float _fireImmuneUntil;
        private static float _nextTrigger;

        [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
        private static class RPC_Damage_Patch
        {
            // Fire immunity: strip the fire portion of incoming hits while the window is open.
            [UsedImplicitly]
            private static void Prefix(Character __instance, HitData hit)
            {
                if (hit != null && __instance == Player.m_localPlayer && Time.time < _fireImmuneUntil)
                {
                    hit.m_damage.m_fire = 0f;
                }
            }

            // Eruption: on a hit that leaves the player alive but at low health, blast and go fire-immune.
            [UsedImplicitly]
            private static void Postfix(Character __instance, HitData hit)
            {
                if (__instance != Player.m_localPlayer || hit == null || Time.time < _nextTrigger)
                {
                    return;
                }

                var player = Player.m_localPlayer;
                var healthFraction = player.GetHealthPercentage();
                if (healthFraction <= 0f || healthFraction > LowHealthThreshold)
                {
                    return;
                }

                var value = player.GetTotalActiveMagicEffectValue(MagicEffectType.LastFire, 1f);
                if (value <= 0f)
                {
                    return;
                }

                _nextTrigger = Time.time + Cooldown;
                _fireImmuneUntil = Time.time + ImmunityDuration;

                // Douse any fire already burning on the player — the immunity window blocks incoming fire
                // hits (via the prefix), but the existing Burning DoT ticks through ApplyDamage, so clear it.
                player.m_seman.RemoveStatusEffect(SEMan.s_statusEffectBurning);

                BossShardEffects.DamageEnemiesInRadius(player, player.GetCenterPoint(), NovaRadius,
                    new HitData.DamageTypes { m_fire = value * FirePerTier });
            }
        }
    }
}
