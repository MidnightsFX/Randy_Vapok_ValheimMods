using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Green (Movement) chest shard: +% max carry weight scaled by the movement-speed penalty of the
    // player's gear -- a heavy loadout that slows you down also lets you haul more. The percent is of the
    // unmodified base carry weight (baseLimit) so it does not compound with other carry-weight effects
    // like AddCarryWeight. Shard values are authored as whole-number percents, hence the 0.01f.
    public static class CarryWeightForMovementPenalty
    {
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyMaxCarryWeight))]
        private static class ModifyMaxCarryWeight_Patch
        {
            [UsedImplicitly]
            private static void Postfix(SEMan __instance, float baseLimit, ref float limit)
            {
                var player = Player.m_localPlayer;
                if (__instance.m_character != player)
                {
                    return;
                }

                limit += baseLimit * player.GetTotalActiveMagicEffectValue(
                    MagicEffectType.CarryWeightForMovementPenalty, 0.01f) * PenaltyScaling.MovementPenaltyFactor(player);
            }
        }
    }
}
