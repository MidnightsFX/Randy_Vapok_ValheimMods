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
    // so more incoming hits land as perfect dodges. Self-contained (a brief timed-immunity window);
    // the reward effects above still fire off the vanilla perfect-dodge trigger.
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

        // Prefix handler invoked by CharacterRpcDamageDispatch: while the immunity window is open, negate
        // incoming damage to the local player.
        public static void ModifyIncoming(Character __instance, HitData hit)
        {
            if (hit != null && __instance == Player.m_localPlayer && Time.time < _immuneUntil)
            {
                hit.m_damage.Modify(0f);
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

    // ---- Shoulder: a perfect dodge grants a brief burst of movement speed ---------------------------
    // Hangs off the same vanilla perfect-dodge trigger as the reward effects above, opening a short window
    // during which the local player's move speed is boosted. The boost is applied through
    // SEMan.ApplyStatusEffectSpeedMods (the hook the game's own speed status effects use). Shard values are
    // authored as whole-number percents, hence the 0.01f.
    public static class PerfectDodgeGivesSpeed
    {
        private const float SpeedWindow = 1f; // seconds of the speed buff granted on a perfect dodge
        private static float _speedUntil;

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

                if (__instance.GetTotalActiveMagicEffectValue(MagicEffectType.PerfectDodgeGivesSpeed, 0.01f) > 0f)
                {
                    _speedUntil = Time.time + SpeedWindow;
                }
            }
        }

        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ApplyStatusEffectSpeedMods))]
        private static class ApplyStatusEffectSpeedMods_Patch
        {
            [UsedImplicitly]
            private static void Postfix(SEMan __instance, ref float speed)
            {
                if (__instance.m_character != Player.m_localPlayer || Time.time >= _speedUntil)
                {
                    return;
                }

                var bonus = Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                    MagicEffectType.PerfectDodgeGivesSpeed, 0.01f);
                if (bonus != 0f)
                {
                    speed *= 1f + bonus;
                }
            }
        }
    }
}
