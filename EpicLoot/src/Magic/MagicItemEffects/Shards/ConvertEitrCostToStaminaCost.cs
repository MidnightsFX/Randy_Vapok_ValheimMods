using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Purple trinket shard: pay value% of every eitr cost from stamina instead of eitr. When the local
    // player spends eitr, the converted portion is refunded to the eitr pool and drained from stamina
    // instead. Shard values are authored as whole-number percents, hence the 0.01f.
    public static class ConvertEitrCostToStaminaCost
    {
        [HarmonyPatch(typeof(Player), nameof(Player.UseEitr))]
        private static class UseEitr_Patch
        {
            [UsedImplicitly]
            private static void Postfix(Player __instance, float v)
            {
                if (v <= 0f || __instance != Player.m_localPlayer)
                {
                    return;
                }

                var fraction = __instance.GetTotalActiveMagicEffectValue(MagicEffectType.ConvertEitrCostToStaminaCost, 0.01f);
                if (fraction <= 0f)
                {
                    return;
                }

                var converted = v * fraction;
                __instance.AddEitr(converted);     // refund the converted portion of the eitr cost...
                __instance.UseStamina(converted);  // ...and pay it from stamina instead.
            }
        }
    }
}
