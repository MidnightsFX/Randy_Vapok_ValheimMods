using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Black shoulder shard: +% max carry weight during the night (EnvMan.IsNight()). The percent is of the
    // unmodified base carry weight (baseLimit) so it does not compound with other carry-weight effects like
    // AddCarryWeight. Mirrors CarryWeightForMovementPenalty's ModifyMaxCarryWeight hook. Shard values are
    // authored as whole-number percents, hence the 0.01f.
    public static class NightCarryWeight
    {
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyMaxCarryWeight))]
        private static class ModifyMaxCarryWeight_Patch
        {
            [UsedImplicitly]
            private static void Postfix(SEMan __instance, float baseLimit, ref float limit)
            {
                var player = Player.m_localPlayer;
                if (__instance.m_character != player || !EnvMan.IsNight())
                {
                    return;
                }

                limit += baseLimit * player.GetTotalActiveMagicEffectValue(MagicEffectType.NightCarryWeight, 0.01f);
            }
        }
    }
}
