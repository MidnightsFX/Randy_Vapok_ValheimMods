using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Purple utility shard: when a stamina cost exceeds current stamina, pay up to value% of the
    // shortfall from health (1:1) so the stamina-gated action can still go through. Health is never
    // driven below 1. Shard values are authored as whole-number percents, hence the 0.01f.
    public static class RunningOnEmpty
    {
        [HarmonyPatch(typeof(Player), nameof(Player.UseStamina))]
        private static class UseStamina_Patch
        {
            [UsedImplicitly]
            private static void Prefix(Player __instance, ref float v)
            {
                if (v <= 0f || __instance != Player.m_localPlayer)
                {
                    return;
                }

                var stamina = __instance.GetStamina();
                if (v <= stamina)
                {
                    return; // enough stamina; nothing to cover
                }

                var fraction = __instance.GetTotalActiveMagicEffectValue(MagicEffectType.RunningOnEmpty, 0.01f);
                if (fraction <= 0f)
                {
                    return;
                }

                var coverable = (v - stamina) * fraction;
                var healthPay = Mathf.Min(coverable, __instance.GetHealth() - 1f);
                if (healthPay > 0f)
                {
                    __instance.UseHealth(healthPay);
                    v -= healthPay; // that much of the cost is paid by health instead of stamina
                }
            }
        }
    }
}
