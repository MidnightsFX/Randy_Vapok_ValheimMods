using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Moder boss shard: when the local player is struck there is a chance the cold answers back — a frost nova
    // detonates around the player, dealing frost damage (and its usual chilling slow via AddFrostDamage) to
    // every nearby enemy. Hooked on the player's RPC_Damage (damage taken, owner-side = local). The boss-tier
    // shard value (5..25) is read as the proc chance in percent and also scales the nova's frost damage.
    //
    // A short cooldown keeps a flurry of quick hits from chaining several novas at once. Damage-over-time
    // (poison/burning) ticks through Character.ApplyDamage rather than RPC_Damage, so they can't proc this.
    public static class IcyRetribution
    {
        private const float NovaRadius = 8f;
        private const float FrostPerTier = 8f;   // frost damage per point of value (5..25 -> 40..200)
        private const float Cooldown = 3f;

        private static float _nextTime;

        [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
        private static class RPC_Damage_Patch
        {
            [UsedImplicitly]
            private static void Postfix(Character __instance, HitData hit)
            {
                if (__instance != Player.m_localPlayer || hit == null || Time.time < _nextTime)
                {
                    return;
                }

                var value = Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.IcyRetribution, 1f);
                if (value <= 0f || Random.value > value * 0.01f)
                {
                    return;
                }

                _nextTime = Time.time + Cooldown;

                var player = Player.m_localPlayer;
                BossShardEffects.DamageEnemiesInRadius(player, player.GetCenterPoint(), NovaRadius,
                    new HitData.DamageTypes { m_frost = value * FrostPerTier });
            }
        }
    }
}
