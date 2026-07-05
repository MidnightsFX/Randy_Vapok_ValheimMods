using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // White head shard: widen the map-discovery radius while it is daytime (EnvMan.IsDay()). Mirrors
    // ModifyDiscoveryRadius. Shard values are authored as whole-number percents, hence the 0.01f.
    public static class DayDiscovery
    {
        [HarmonyPatch(typeof(Minimap), nameof(Minimap.Explore), typeof(Vector3), typeof(float))]
        private static class Explore_Patch
        {
            [UsedImplicitly]
            private static void Prefix(ref float radius)
            {
                if (Player.m_localPlayer == null || !EnvMan.IsDay())
                {
                    return;
                }

                var bonus = Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.DayDiscovery, 0.01f);
                if (bonus != 0f)
                {
                    radius *= 1f + bonus;
                }
            }
        }
    }
}
