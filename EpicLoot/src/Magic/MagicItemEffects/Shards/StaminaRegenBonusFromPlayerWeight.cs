using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Peach (Logistics) chest shard: +% stamina regen that scales with how loaded the player's pack is.
    // Adds to the stamina regen multiplier like ModifyPlayerRegen / DayStaminaRegen. Shard values are
    // authored as whole-number percents, hence the 0.01f.
    public static class StaminaRegenBonusFromPlayerWeight
    {
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyStaminaRegen))]
        private static class ModifyStaminaRegen_Patch
        {
            [UsedImplicitly]
            private static void Postfix(SEMan __instance, ref float staminaMultiplier)
            {
                var player = Player.m_localPlayer;
                if (__instance.m_character != player)
                {
                    return;
                }

                staminaMultiplier += player.GetTotalActiveMagicEffectValue(
                    MagicEffectType.StaminaRegenBonusFromPlayerWeight, 0.01f) * PenaltyScaling.WeightFactor(player);
            }
        }
    }
}
