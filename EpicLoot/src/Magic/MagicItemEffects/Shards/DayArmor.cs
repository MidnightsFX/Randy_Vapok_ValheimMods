using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // White chest shard: +% armor while it is daytime (EnvMan.IsDay()). GetArmor is summed per equipped
    // piece by GetBodyArmor, so applying the (player-wide) day value to each piece yields a net
    // "+X% total armor", matching how set-bonus armor is applied globally in ModifyArmor. Shard values
    // are authored as whole-number percents, hence the 0.01f.
    public static class DayArmor
    {
        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetArmor), typeof(int), typeof(float))]
        private static class GetArmor_Patch
        {
            [UsedImplicitly]
            private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
            {
                var player = PlayerExtensions.GetPlayerWithEquippedItem(__instance);
                if (player == null || !EnvMan.IsDay())
                {
                    return;
                }

                var bonus = player.GetTotalActiveMagicEffectValue(MagicEffectType.DayArmor, 0.01f);
                if (bonus != 0f)
                {
                    __result *= 1f + bonus;
                }
            }
        }
    }
}
