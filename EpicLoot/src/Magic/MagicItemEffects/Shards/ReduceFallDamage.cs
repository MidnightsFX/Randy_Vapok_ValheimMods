using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Pink chest shard: subtract a flat amount from fall damage. Vanilla computes a raw fall damage in
    // Character.UpdateGroundContact and passes it through SEMan.ModifyFallDamage before applying it, so a
    // Postfix here is the same hook the game uses for feather-fall / status effects. Only players ever take
    // fall damage (the caller guards IsPlayer()); we further limit to the local player since magic effects
    // are only meaningful for it. Vanilla fall damage ramps 0-100 over a 4m-20m drop, so shard values are
    // authored on that same raw scale; the result is floored at 0 (the caller skips Damage() at <= 0).
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
                    MagicEffectType.ReduceFallDamage);
                if (reduction > 0f)
                {
                    damage = Mathf.Max(0f, damage - reduction);
                }
            }
        }
    }
}
