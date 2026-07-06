using System.Collections;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Black trinket shard: when the local player's adrenaline fills up ("activates"), summon a tamed bat
    // to fight alongside them for a short time. Detected by watching AddAdrenaline cross up to max. Uses
    // vanilla's adrenaline pool, so it is inert unless the player has a max-adrenaline source. The summon
    // is rate-limited by a cooldown and despawns after a lifetime so bats don't accumulate.
    public static class SummonBatWhenActivatingAdrenaline
    {
        private const string BatPrefabName = "Bat";
        private const float SummonCooldown = 10f;
        private const float BatLifetime = 30f;
        private static float _lastSummonTime = -999f;

        [HarmonyPatch(typeof(Player), nameof(Player.AddAdrenaline))]
        private static class AddAdrenaline_Patch
        {
            [UsedImplicitly]
            private static void Prefix(Player __instance, out float __state)
            {
                __state = __instance.GetAdrenaline();
            }

            [UsedImplicitly]
            private static void Postfix(Player __instance, float __state)
            {
                if (__instance != Player.m_localPlayer)
                {
                    return;
                }

                // Only when adrenaline crosses up to full. (If gear resets the pool to 0 on full via a
                // full-adrenaline status effect, that edge is intentionally not caught.)
                var max = __instance.GetMaxAdrenaline();
                if (max <= 0f || __state >= max || __instance.GetAdrenaline() < max)
                {
                    return;
                }

                if (Time.time - _lastSummonTime < SummonCooldown)
                {
                    return;
                }

                if (__instance.GetTotalActiveMagicEffectValue(MagicEffectType.SummonBatWhenActivatingAdrenaline) <= 0f)
                {
                    return;
                }

                _lastSummonTime = Time.time;
                SummonBat(__instance);
            }
        }

        private static void SummonBat(Player player)
        {
            if (ZNetScene.instance == null)
            {
                return;
            }

            var prefab = ZNetScene.instance.GetPrefab(BatPrefabName);
            if (prefab == null)
            {
                EpicLoot.LogError($"SummonBatWhenActivatingAdrenaline: could not find prefab '{BatPrefabName}'.");
                return;
            }

            var spawnPos = player.transform.position + player.transform.forward * 2f + Vector3.up;
            var bat = Object.Instantiate(prefab, spawnPos, Quaternion.identity);

            var ai = bat.GetComponent<MonsterAI>();
            if (ai != null)
            {
                ai.MakeTame();
                ai.SetFollowTarget(player.gameObject);
            }
            else
            {
                bat.GetComponent<Character>()?.SetTamed(true);
            }

            player.StartCoroutine(DespawnAfter(bat, BatLifetime));
        }

        private static IEnumerator DespawnAfter(GameObject creature, float lifetime)
        {
            yield return new WaitForSeconds(lifetime);
            if (creature == null)
            {
                yield break;
            }

            if (ZNetScene.instance != null)
            {
                ZNetScene.instance.Destroy(creature);
            }
            else
            {
                Object.Destroy(creature);
            }
        }
    }
}
