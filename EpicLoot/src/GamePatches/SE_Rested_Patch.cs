using EpicLoot.MagicItemEffects.Shards;
using HarmonyLib;

namespace EpicLoot {
    [HarmonyPatch]
    internal static class SE_Rested_Patch {

        [HarmonyPatch(typeof(SE_Rested), "UpdateTTL")]
        [HarmonyPostfix]
        private static void UpdateTTLPostfix(SE_Rested __instance) {
            if (!(__instance.m_character is Player player) ||
                player != Player.m_localPlayer) {
                return;
            }

            var comfort = player.GetComfortLevel();

            if (comfort > GainMaxCarryWeightFromRested.RestedComfort) {
                GainMaxCarryWeightFromRested.RestedComfort = comfort;
            }
        }

        [HarmonyPatch(typeof(StatusEffect), "Stop")]
        [HarmonyPrefix]
        private static void StopPrefix(StatusEffect __instance) {
            if (__instance is SE_Rested &&
                __instance.m_character == Player.m_localPlayer) {
                GainMaxCarryWeightFromRested.RestedComfort = 0;
            }
        }
    }
}
