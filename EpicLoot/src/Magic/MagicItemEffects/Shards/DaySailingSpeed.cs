using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // White shoulder shard: +% ship sail propulsion while it is daytime (EnvMan.IsDay()) and the local
    // player is aboard. Scales the sail force the ship applies, so it stacks with wind and speed settings.
    // Mirrors SailingSpeed's GetSailForce hook plus a day gate. Shard values are whole-number percents.
    public static class DaySailingSpeed
    {
        [HarmonyPatch(typeof(Ship), nameof(Ship.GetSailForce))]
        private static class Ship_GetSailForce_Patch
        {
            [UsedImplicitly]
            private static void Postfix(Ship __instance, ref Vector3 __result)
            {
                var player = Player.m_localPlayer;
                if (player == null || !EnvMan.IsDay() || !__instance.m_players.Contains(player))
                {
                    return;
                }

                var bonus = player.GetTotalActiveMagicEffectValue(MagicEffectType.DaySailingSpeed, 0.01f);
                if (bonus > 0f)
                {
                    __result *= 1f + bonus;
                }
            }
        }
    }
}
