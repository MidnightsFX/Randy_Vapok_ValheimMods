using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Pink chest shard: scale down fall damage. Vanilla computes a raw fall damage in
    // Character.UpdateGroundContact and passes it through SEMan.ModifyFallDamage before applying it, so a
    // Postfix here is the same hook the game uses for feather-fall / status effects. Only players ever take
    // fall damage (the caller guards IsPlayer()); we further limit to the local player since magic effects
    // are only meaningful for it. Shard values are authored as whole-number percents, hence the 0.01f, and
    // the reduction is clamped to 0-100%.
    public static class ReduceFallDamage
    {
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyFallDamage))]
        private static class ModifyFallDamage_Patch
        {
            [UsedImplicitly]
            private static void Postfix(SEMan __instance, ref float damage)
            {
                if (__instance.m_character != Player.m_localPlayer || damage <= 0f)
                {
                    return;
                }

                var reduction = Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                    MagicEffectType.ReduceFallDamage, 0.01f);
                if (reduction > 0f)
                {
                    damage *= 1f - Mathf.Clamp01(reduction);
                }
            }
        }
    }
}
