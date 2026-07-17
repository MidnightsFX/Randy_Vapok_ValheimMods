using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Orange shoulder shard: +% movement speed while the player is on fire. Vanilla funnels every walk/run/
    // swim speed through SEMan.ApplyStatusEffectSpeedMods(ref speed, dir), so a Postfix there is the same
    // hook the game's own speed status effects use. It runs for every character, so we limit to the local
    // player and to when a burning status effect is actually active. Shard values are authored as
    // whole-number percents, hence the 0.01f.
    public static class BurningSpeed
    {
        private static bool IsBurning(SEMan seman)
        {
            var effects = seman.GetStatusEffects();
            for (var i = 0; i < effects.Count; i++)
            {
                if (effects[i] is SE_Burning)
                {
                    return true;
                }
            }
            return false;
        }

        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ApplyStatusEffectSpeedMods))]
        private static class ApplyStatusEffectSpeedMods_Patch
        {
            [UsedImplicitly]
            private static void Postfix(SEMan __instance, ref float speed)
            {
                if (__instance.m_character != Player.m_localPlayer || !IsBurning(__instance))
                {
                    return;
                }

                var bonus = Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.BurningSpeed, 0.01f);
                if (bonus != 0f)
                {
                    speed *= 1f + bonus;
                }
            }
        }
    }
}
