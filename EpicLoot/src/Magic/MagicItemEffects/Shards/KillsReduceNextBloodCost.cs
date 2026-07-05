using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // DarkPurple head shard (Blood Magic): kills bank a discount on the health ("blood") cost of the local
    // player's next attack. Each kill by the local player adds the shard value to a running reduction (capped
    // at MaxReduction); the reduction is applied and consumed the next time an attack pays health.
    //
    // Kill detection mirrors vanilla's own last-hit attribution in Character.OnDeath
    // (m_lastHit.GetAttacker() == localPlayer). Consumption hooks Character.UseHealth, which vanilla only ever
    // calls to pay an attack's health cost (Attack.GetAttackHealth), so it stacks with ModifyAttackHealthUse
    // (which has already reduced the amount passed in) and fires exactly once per blood attack. Shard values
    // are authored as whole-number percents, hence the 0.01f.
    public static class KillsReduceNextBloodCost
    {
        // Cap on the banked reduction. 1 = kills can bank up to a fully-free next blood attack. Tunable.
        private const float MaxReduction = 1f;

        // Banked fraction (0-1) to shave off the local player's next attack health cost.
        private static float _bankedReduction;

        [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
        private static class OnDeath_Patch
        {
            [UsedImplicitly]
            private static void Postfix(Character __instance)
            {
                if (Player.m_localPlayer == null || __instance == Player.m_localPlayer
                    || __instance.m_lastHit?.GetAttacker() != Player.m_localPlayer)
                {
                    return;
                }

                var perKill = Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                    MagicEffectType.KillsReduceNextBloodCost, 0.01f);
                if (perKill > 0f)
                {
                    _bankedReduction = Mathf.Min(MaxReduction, _bankedReduction + perKill);
                }
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.UseHealth))]
        private static class UseHealth_Patch
        {
            [UsedImplicitly]
            private static void Prefix(Character __instance, ref float hp)
            {
                if (__instance != Player.m_localPlayer || hp <= 0f || _bankedReduction <= 0f)
                {
                    return;
                }

                hp *= 1f - Mathf.Clamp01(_bankedReduction);
                _bankedReduction = 0f;
            }
        }
    }
}
