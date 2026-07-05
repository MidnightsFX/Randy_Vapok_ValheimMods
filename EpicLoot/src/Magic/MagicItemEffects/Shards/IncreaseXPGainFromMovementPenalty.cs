using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Green (Movement) head shard: +% skill XP (every skill) scaled by the movement-speed penalty of the
    // player's gear -- a heavy, slow loadout learns faster. Mirrors QuickLearner but gated on the penalty.
    // Shard values are authored as whole-number percents, hence the 0.01f.
    public static class IncreaseXPGainFromMovementPenalty
    {
        [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
        private static class RaiseSkill_Patch
        {
            [UsedImplicitly]
            private static void Prefix(Skills __instance, ref float factor)
            {
                var player = __instance.m_player;
                if (player == null)
                {
                    return;
                }

                var bonus = player.GetTotalActiveMagicEffectValue(
                    MagicEffectType.IncreaseXPGainFromMovementPenalty, 0.01f) * PenaltyScaling.MovementPenaltyFactor(player);
                factor *= 1f + bonus;
            }
        }
    }
}
