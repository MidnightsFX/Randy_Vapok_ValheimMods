using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Peach (Logistics) trinket shard effect: increase a ship's sail propulsion while the local player is
    // aboard. Scales the sail force the ship applies, so it stacks with wind and speed settings.
    public static class SailingSpeed
    {
        [HarmonyPatch(typeof(Ship), nameof(Ship.GetSailForce))]
        private static class Ship_GetSailForce_Patch
        {
            private static void Postfix(Ship __instance, ref Vector3 __result)
            {
                var player = Player.m_localPlayer;
                if (player == null || !__instance.m_players.Contains(player))
                {
                    return;
                }

                float bonus = player.GetTotalActiveMagicEffectValue(MagicEffectType.SailingSpeed, 0.01f);
                if (bonus > 0f)
                {
                    __result *= 1f + bonus;
                }
            }
        }
    }
}
