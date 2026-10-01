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

            if (!ReferenceEquals(__instance, GainMaxCarryWeightFromRested.RestedEffect)) {
                GainMaxCarryWeightFromRested.RestedEffect = __instance;
                GainMaxCarryWeightFromRested.RestedComfort = comfort;
            } else if (comfort > GainMaxCarryWeightFromRested.RestedComfort) {
                GainMaxCarryWeightFromRested.RestedComfort = comfort;
            }
        }

        [HarmonyPatch(typeof(StatusEffect), "Stop")]
        [HarmonyPrefix]
        private static void StopPrefix(StatusEffect __instance) {
            ClearIfTracked(__instance);
        }

        // SEMan.OnDestroy calls OnDestroy on its effects without calling Stop.
        [HarmonyPatch(typeof(StatusEffect), nameof(StatusEffect.OnDestroy))]
        [HarmonyPrefix]
        private static void OnDestroyPrefix(StatusEffect __instance) {
            ClearIfTracked(__instance);
        }

        private static void ClearIfTracked(StatusEffect effect) {
            if (!ReferenceEquals(effect, GainMaxCarryWeightFromRested.RestedEffect)) {
                return;
            }

            GainMaxCarryWeightFromRested.RestedEffect = null!;
            GainMaxCarryWeightFromRested.RestedComfort = 0;
        }
    }
}
