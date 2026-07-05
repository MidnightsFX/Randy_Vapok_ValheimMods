using HarmonyLib;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Percent-of-pool shard effect (Yellow head): add a percentage of the player's max stamina. Runs at
    // Priority.Last so it layers on top of the flat pool bonuses from IncreasePlayerBaseStats -- i.e.
    // the percentage applies to the full food-derived pool, matching "+% max stamina".
    public static class PercentStamina
    {
        [HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
        private static class Player_GetTotalFoodValue_Patch
        {
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(Player __instance, ref float stamina)
            {
                stamina += stamina * __instance.GetTotalActiveMagicEffectValue(MagicEffectType.PercentStamina, 0.01f);
            }
        }
    }
}
