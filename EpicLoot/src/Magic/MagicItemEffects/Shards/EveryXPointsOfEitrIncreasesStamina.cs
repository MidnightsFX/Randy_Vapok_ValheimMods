using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Purple legs shard: raise max stamina by value% of max eitr. Hooks the same GetTotalFoodValue
    // out-params that set the player's max pools (see IncreasePlayerBaseStats); runs at low priority so
    // the eitr it reads already includes any flat IncreaseEitr bonus. Shard values are authored as
    // whole-number percents, hence the 0.01f.
    public static class EveryXPointsOfEitrIncreasesStamina
    {
        [HarmonyPatch(typeof(Player), "GetTotalFoodValue")]
        private static class GetTotalFoodValue_Patch
        {
            [HarmonyPriority(Priority.Low)]
            [UsedImplicitly]
            private static void Postfix(Player __instance, ref float stamina, ref float eitr)
            {
                var fraction = __instance.GetTotalActiveMagicEffectValue(MagicEffectType.EveryXPointsOfEitrIncreasesStamina, 0.01f);
                if (fraction > 0f)
                {
                    stamina += eitr * fraction;
                }
            }
        }
    }
}
