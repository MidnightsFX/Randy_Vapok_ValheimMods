using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Perfect-dodge shard effects (Pink). Vanilla already fires Player.RPC_HitWhileDodging when the local
    // player is struck inside a dodge's invincibility window (a "perfect dodge"; see Player.HitWhileDodging).
    // The reward effects hang off that vanilla trigger, so they fire exactly when the game considers a dodge
    // "perfect". PerfectDodge (the trinket proc) makes those perfect dodges reliable by granting a brief
    // extra damage-immunity window on a successful roll, and DecreaseDodgeCost reuses the existing
    // dodge-stamina hook (see ModifyDodgeStamina.cs). Shard values are authored as whole-number percents.

    // ---- Rewards on a perfect dodge: restore a % of the matching max pool -------------------------
    public static class PerfectDodgeGivesHealth
    {
        [HarmonyPatch(typeof(Player), "RPC_HitWhileDodging")]
        private static class RPC_HitWhileDodging_Patch
        {
            [UsedImplicitly]
            private static void Postfix(Player __instance)
            {
                if (__instance != Player.m_localPlayer)
                {
                    return;
                }

                var fraction = __instance.GetTotalActiveMagicEffectValue(MagicEffectType.PerfectDodgeGivesHealth, 0.01f);
                if (fraction > 0f)
                {
                    __instance.Heal(__instance.GetMaxHealth() * fraction);
                }
            }
        }
    }

    public static class PerfectDodgeGivesStamina
    {
        [HarmonyPatch(typeof(Player), "RPC_HitWhileDodging")]
        private static class RPC_HitWhileDodging_Patch
        {
            [UsedImplicitly]
            private static void Postfix(Player __instance)
            {
                if (__instance != Player.m_localPlayer)
                {
                    return;
                }

                var fraction = __instance.GetTotalActiveMagicEffectValue(MagicEffectType.PerfectDodgeGivesStamina, 0.01f);
                if (fraction > 0f)
                {
                    __instance.AddStamina(__instance.GetMaxStamina() * fraction);
                }
            }
        }
    }

    public static class PerfectDodgeGivesEitr
    {
        [HarmonyPatch(typeof(Player), "RPC_HitWhileDodging")]
        private static class RPC_HitWhileDodging_Patch
        {
            [UsedImplicitly]
            private static void Postfix(Player __instance)
            {
                if (__instance != Player.m_localPlayer)
                {
                    return;
                }

                var fraction = __instance.GetTotalActiveMagicEffectValue(MagicEffectType.PerfectDodgeGivesEitr, 0.01f);
                if (fraction > 0f && __instance.GetMaxEitr() > 0f)
                {
                    __instance.AddEitr(__instance.GetMaxEitr() * fraction);
                }
            }
        }
    }

    // ---- Trinket proc: a chance, on a dodge roll, to open a brief damage-immunity window -----------
    // so more incoming hits land as perfect dodges. Self-contained (mirrors LastFire's timed-immunity
    // pattern); the reward effects above still fire off the vanilla perfect-dodge trigger.
    public static class PerfectDodge
    {
        private const float ImmunityWindow = 0.5f; // seconds of immunity granted on a successful proc

        private static bool _wasInDodge;
        private static float _immuneUntil;

        // Rising-edge on the dodge animation rolls the proc (mirrors RollCleanse's dodge detection).
        [HarmonyPatch(typeof(Player), nameof(Player.UpdateDodge))]
        private static class UpdateDodge_Patch
        {
            [UsedImplicitly]
            private static void Postfix(Player __instance)
            {
                if (__instance != Player.m_localPlayer)
                {
                    return;
                }

                var inDodge = __instance.m_inDodge;
                var rollStarted = inDodge && !_wasInDodge;
                _wasInDodge = inDodge;

                if (!rollStarted)
                {
                    return;
                }

                var chance = __instance.GetTotalActiveMagicEffectValue(MagicEffectType.PerfectDodge, 0.01f);
                if (chance > 0f && Random.value < chance)
                {
                    _immuneUntil = Time.time + ImmunityWindow;
                }
            }
        }

        // While the window is open, negate incoming damage to the local player.
        [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
        private static class RPC_Damage_Patch
        {
            [UsedImplicitly]
            private static void Prefix(Character __instance, HitData hit)
            {
                if (hit != null && __instance == Player.m_localPlayer && Time.time < _immuneUntil)
                {
                    hit.m_damage.Modify(0f);
                }
            }
        }
    }

    // ---- Head: reduce dodge-roll stamina cost (mirrors ModifyDodgeStaminaUse) ----------------------
    public static class DecreaseDodgeCost
    {
        [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentDodgeStaminaModifier))]
        private static class GetEquipmentDodgeStaminaModifier_Patch
        {
            [UsedImplicitly]
            private static void Postfix(Player __instance, ref float __result)
            {
                if (__instance == null)
                {
                    return;
                }

                __result -= __instance.GetTotalActiveMagicEffectValue(MagicEffectType.DecreaseDodgeCost, 0.01f);
            }
        }
    }
}
