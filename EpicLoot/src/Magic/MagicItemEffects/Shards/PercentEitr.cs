using HarmonyLib;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Percent-of-pool shard effect (Cyan head): add a percentage of the player's max eitr. Runs at
    // Priority.Last so it layers on top of the flat pool bonuses from IncreasePlayerBaseStats -- i.e.
    // the percentage applies to the full food-derived pool, matching "+% max eitr". Yields nothing when
    // the player has no eitr pool (percentage of zero), which is inherent to a percent-of-pool effect.
    public static class PercentEitr
    {
        [HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
        private static class Player_GetTotalFoodValue_Patch
        {
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(Player __instance, ref float eitr)
            {
                eitr += eitr * __instance.GetTotalActiveMagicEffectValue(MagicEffectType.PercentEitr, 0.01f);
            }
        }
    }
}
