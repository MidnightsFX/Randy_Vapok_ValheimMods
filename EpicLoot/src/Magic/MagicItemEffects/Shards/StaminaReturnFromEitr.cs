using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Yellow magic-weapon shard: whenever the local player spends eitr (casting a staff, channelling a
    // draw drain, etc.), restore stamina equal to value% of the eitr spent. Shard values are authored
    // as whole-number percents, hence the 0.01f.
    public static class StaminaReturnFromEitr
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

                var fraction = __instance.GetTotalActiveMagicEffectValue(MagicEffectType.StaminaReturnFromEitr, 0.01f);
                if (fraction > 0f)
                {
                    __instance.AddStamina(v * fraction);
                }
            }
        }
    }
}
