using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Golden (Fortune) legs shard effect: a chance, when the local player picks up a caught fish, to reel
    // in a bonus catch of the same size -- a "lucky haul".
    public static class LuckWhileFishing
    {
        [HarmonyPatch(typeof(Fish), nameof(Fish.Pickup))]
        private static class Fish_Pickup_Patch
        {
            private static void Postfix(Fish __instance, Humanoid character, bool __result)
            {
                if (!__result || character != Player.m_localPlayer || __instance.m_pickupItem == null)
                {
                    return;
                }

                float chance = Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                    MagicEffectType.LuckWhileFishing, 0.01f);
                if (chance > 0f && Random.value < chance)
                {
                    Player.m_localPlayer.PickupPrefab(__instance.m_pickupItem, __instance.m_pickupItemStackSize);
                }
            }
        }
    }
}
