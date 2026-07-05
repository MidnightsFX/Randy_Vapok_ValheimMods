using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Peach (Logistics) head shard: converts a slice of the player's max health into extra max stamina.
    // Added in the GetTotalFoodValue postfix (like IncreasePlayerBaseStats' IncreaseStamina) so it feeds
    // both the stamina pool and its HUD bar, using the food-derived max health (hp) as the base. Shard
    // values are authored as whole-number percents (of max health), hence the 0.01f.
    public static class GainMaxStaminaBasedOnPlayerMaxHealth
    {
        [HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
        private static class GetTotalFoodValue_Patch
        {
            [UsedImplicitly]
            private static void Postfix(Player __instance, ref float hp, ref float stamina)
            {
                if (__instance != Player.m_localPlayer)
                {
                    return;
                }

                stamina += hp * __instance.GetTotalActiveMagicEffectValue(
                    MagicEffectType.GainMaxStaminaBasedOnPlayerMaxHealth, 0.01f);
            }
        }
    }
}
