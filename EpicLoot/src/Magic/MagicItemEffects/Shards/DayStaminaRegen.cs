using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // White legs shard: +% stamina regen while it is daytime (EnvMan.IsDay()). Adds to the multiplier
    // like ModifyPlayerRegen. Shard values are authored as whole-number percents, hence the 0.01f.
    public static class DayStaminaRegen
    {
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyStaminaRegen))]
        private static class ModifyStaminaRegen_Patch
        {
            [UsedImplicitly]
            private static void Postfix(SEMan __instance, ref float staminaMultiplier)
            {
                if (__instance.m_character != Player.m_localPlayer || !EnvMan.IsDay())
                {
                    return;
                }

                staminaMultiplier += Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                    MagicEffectType.DayStaminaRegen, 0.01f);
            }
        }
    }
}
