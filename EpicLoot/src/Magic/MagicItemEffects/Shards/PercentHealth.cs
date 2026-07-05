using HarmonyLib;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Percent-of-pool shard effect (Red legs): add a percentage of the player's max health. Runs at
    // Priority.Last so it layers on top of the flat pool bonuses from IncreasePlayerBaseStats -- i.e.
    // the percentage applies to the full food-derived pool, matching "+% max health".
    public static class PercentHealth
    {
        [HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
        private static class Player_GetTotalFoodValue_Patch
        {
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(Player __instance, ref float hp)
            {
                hp += hp * __instance.GetTotalActiveMagicEffectValue(MagicEffectType.PercentHealth, 0.01f);
            }
        }
    }
}
