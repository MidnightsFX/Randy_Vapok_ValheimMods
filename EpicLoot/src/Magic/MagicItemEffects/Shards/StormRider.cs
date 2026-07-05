using System;
using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // LightBlue legs shard: +% movement speed while a storm is raging. Vanilla funnels every walk/run/swim
    // speed through SEMan.ApplyStatusEffectSpeedMods(ref speed, dir), so a Postfix there is the same hook the
    // game's own speed-modifying status effects use. It runs for every character, so we limit to the local
    // player. A "storm" is any of the game's storm environments (thunderstorm at sea, mountain blizzards).
    // Shard values are authored as whole-number percents, hence the 0.01f.
    public static class StormRider
    {
        // Vanilla EnvSetup names whose weather counts as a storm. Compared case-insensitively against the
        // current environment name (EnvSetup.m_name).
        private static readonly HashSet<string> StormEnvironments = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ThunderStorm",
            "SnowStorm",
            "Twilight_SnowStorm",
        };

        public static bool IsStorm()
        {
            var env = EnvMan.instance?.GetCurrentEnvironment();
            return env != null && StormEnvironments.Contains(env.m_name);
        }

        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ApplyStatusEffectSpeedMods))]
        private static class ApplyStatusEffectSpeedMods_Patch
        {
            [UsedImplicitly]
            private static void Postfix(SEMan __instance, ref float speed)
            {
                if (__instance.m_character != Player.m_localPlayer || !IsStorm())
                {
                    return;
                }

                var bonus = Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                    MagicEffectType.StormRider, 0.01f);
                if (bonus != 0f)
                {
                    speed *= 1f + bonus;
                }
            }
        }
    }
}
