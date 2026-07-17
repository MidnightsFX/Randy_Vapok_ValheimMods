using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Purple shoulder shard: reduce all Eitr costs by value%. Prefixes Player.UseEitr -- the single funnel
    // every eitr expenditure passes through (staff casts, eitr attacks, etc.) -- and scales the amount
    // down before it is spent. Shard values are authored as whole-number percents, hence the 0.01f.
    public static class ReduceEitrCost
    {
        [HarmonyPatch(typeof(Player), nameof(Player.UseEitr))]
        private static class Player_UseEitr_Patch
        {
            [UsedImplicitly]
            private static void Prefix(Player __instance, ref float v)
            {
                if (v <= 0f || __instance != Player.m_localPlayer)
                {
                    return;
                }

                var reduction = __instance.GetTotalActiveMagicEffectValue(MagicEffectType.ReduceEitrCost, 0.01f);
                if (reduction > 0f)
                {
                    v *= Mathf.Max(0f, 1f - reduction);
                }
            }
        }
    }
}
