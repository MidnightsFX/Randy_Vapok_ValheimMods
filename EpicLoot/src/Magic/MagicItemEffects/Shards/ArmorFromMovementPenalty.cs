using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Green shoulder shard: +% armor scaled by the movement-speed penalty of the player's gear -- a heavy,
    // slow loadout is also better protected. GetArmor is summed per equipped piece by GetBodyArmor, so
    // applying the (player-wide) value to each piece yields a net "+X% total armor" (mirrors DayArmor).
    // Shard values are authored as whole-number percents, hence the 0.01f.
    public static class ArmorFromMovementPenalty
    {
        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetArmor), typeof(int), typeof(float))]
        private static class GetArmor_Patch
        {
            [UsedImplicitly]
            private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
            {
                var player = PlayerExtensions.GetPlayerWithEquippedItem(__instance);
                if (player == null)
                {
                    return;
                }

                var bonus = player.GetTotalActiveMagicEffectValue(MagicEffectType.ArmorFromMovementPenalty, 0.01f)
                    * PenaltyScaling.MovementPenaltyFactor(player);
                if (bonus != 0f)
                {
                    __result *= 1f + bonus;
                }
            }
        }
    }
}
