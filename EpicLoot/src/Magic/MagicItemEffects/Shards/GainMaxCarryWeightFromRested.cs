using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Peach (Weight) legs shard: +flat max carry weight while the player is Rested, scaled by the Rested
    // comfort level (Player.GetComfortLevel()). The shard value is the carry weight granted per comfort
    // level, so the total bonus is value * comfortLevel -- a well-furnished base (higher comfort) hauls
    // more than a field rest. Gated on the vanilla Rested status effect so the bonus vanishes when the
    // buff expires. Mirrors NightCarryWeight's ModifyMaxCarryWeight hook; the value is a flat amount
    // (not a percent of baseLimit), matching AddCarryWeight.
    public static class GainMaxCarryWeightFromRested
    {
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyMaxCarryWeight))]
        private static class ModifyMaxCarryWeight_Patch
        {
            [UsedImplicitly]
            private static void Postfix(SEMan __instance, ref float limit)
            {
                var player = Player.m_localPlayer;
                if (__instance.m_character != player || !__instance.HaveStatusEffect(SEMan.s_statusEffectRested))
                {
                    return;
                }

                var comfortLevel = player.GetComfortLevel();
                if (comfortLevel <= 0)
                {
                    return;
                }

                limit += player.GetTotalActiveMagicEffectValue(MagicEffectType.GainMaxCarryWeightFromRested) * comfortLevel;
            }
        }
    }
}
