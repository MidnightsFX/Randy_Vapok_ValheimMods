using System.Collections;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Yagluth boss shard: the local player's hits have a chance to call down a fiery meteor on the struck
    // enemy. After a short fall delay a fire blast erupts at the target's position, dealing fire damage (and
    // its usual burning DoT via AddFireDamage) to everything caught in the impact. Hooked on Character.Damage
    // (attacker == local player). The boss-tier shard value (5..25) is read as the proc chance in percent and
    // also scales the meteor's fire damage. A cooldown keeps a rapid combo from raining meteors.
    public static class MeteorSummoner
    {
        private const float ImpactRadius = 5f;
        private const float FirePerTier = 10f;  // fire damage per point of value (5..25 -> 50..250)
        private const float FallDelay = 0.6f;   // seconds between the proc and the impact
        private const float Cooldown = 5f;

        private static float _nextTime;

        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        private static class Damage_Patch
        {
            [UsedImplicitly]
            private static void Postfix(Character __instance, HitData hit)
            {
                if (hit == null || __instance == Player.m_localPlayer
                    || hit.GetAttacker() != Player.m_localPlayer || Time.time < _nextTime)
                {
                    return;
                }

                var value = Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.MeteorSummoner, 1f);
                if (value <= 0f || Random.value > value * 0.01f)
                {
                    return;
                }

                _nextTime = Time.time + Cooldown;
                Player.m_localPlayer.StartCoroutine(SummonMeteor(__instance.transform.position, value));
            }
        }

        private static IEnumerator SummonMeteor(Vector3 impact, float value)
        {
            yield return new WaitForSeconds(FallDelay);

            BossShardEffects.DamageEnemiesInRadius(Player.m_localPlayer, impact, ImpactRadius,
                new HitData.DamageTypes { m_fire = value * FirePerTier });
        }
    }
}
