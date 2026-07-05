using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Cyan trinket shard: gain adrenaline equal to value% of any eitr the local player spends. Uses
    // vanilla's adrenaline pool, so it is inert unless the player has a max-adrenaline source. Shard
    // values are authored as whole-number percents, hence the 0.01f.
    public static class EitrUseGivesAdrenaline
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

                var fraction = __instance.GetTotalActiveMagicEffectValue(MagicEffectType.EitrUseGivesAdrenaline, 0.01f);
                if (fraction > 0f)
                {
                    __instance.AddAdrenaline(v * fraction);
                }
            }
        }
    }
}
