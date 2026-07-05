using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Green (Movement) legs shard: +% max stamina scaled by the movement-speed penalty of the player's
    // gear. Added in the GetTotalFoodValue postfix (like IncreasePlayerBaseStats' IncreaseStamina) so it
    // feeds both the stamina pool and its HUD bar; the percent is of the food-derived max stamina. Shard
    // values are authored as whole-number percents, hence the 0.01f.
    public static class StaminaIncreaseForMovementPenalty
    {
        [HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
        private static class GetTotalFoodValue_Patch
        {
            [UsedImplicitly]
            private static void Postfix(Player __instance, ref float stamina)
            {
                if (__instance != Player.m_localPlayer)
                {
                    return;
                }

                stamina += stamina * __instance.GetTotalActiveMagicEffectValue(
                    MagicEffectType.StaminaIncreaseForMovementPenalty, 0.01f) * PenaltyScaling.MovementPenaltyFactor(__instance);
            }
        }
    }
}
