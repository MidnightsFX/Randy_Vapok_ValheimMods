using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // White trinket shard: +% health regen while it is daytime (EnvMan.IsDay()). Adds to the multiplier
    // like ModifyPlayerRegen. Shard values are authored as whole-number percents, hence the 0.01f.
    public static class DayHealthRegen
    {
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyHealthRegen))]
        private static class ModifyHealthRegen_Patch
        {
            [UsedImplicitly]
            private static void Postfix(SEMan __instance, ref float regenMultiplier)
            {
                if (__instance.m_character != Player.m_localPlayer || !EnvMan.IsDay())
                {
                    return;
                }

                regenMultiplier += Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                    MagicEffectType.DayHealthRegen, 0.01f);
            }
        }
    }
}
