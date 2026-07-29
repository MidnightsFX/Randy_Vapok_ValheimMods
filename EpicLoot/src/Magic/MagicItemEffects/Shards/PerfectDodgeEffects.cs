using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Perfect-dodge shard effects (Pink). Vanilla already fires Player.RPC_HitWhileDodging when the local
    // player is struck inside a dodge's invincibility window (a "perfect dodge"; see Player.HitWhileDodging).
    // The reward effects hang off that vanilla trigger, so they fire exactly when the game considers a dodge
    // "perfect". PerfectDodge (the trinket proc) makes those perfect dodges reliable by keeping the roll's
    // invincibility alive for the whole animation, and DecreaseDodgeCost reuses the existing
    // dodge-stamina hook (see ModifyDodgeStamina.cs). Shard values are authored as whole-number percents.

    // ---- Shared trigger for the reward effects ------------------------------------------------------
    // Vanilla latches m_beenHitWhileDodging so only the first avoided hit of a roll counts as the perfect
    // dodge (Player.RPC_HitWhileDodging); the latch is cleared when the roll ends (Player.UpdateDodge).
    // A Harmony postfix still runs when the original early-returns on that latch, and the attacker raises
    // the RPC once per collider per hit (Attack's hit loop and hit-list pass, plus Projectile) -- so a
    // single roll through a volley or a wide sweep invokes it many times. Gating on the false->true
    // transition is what keeps the rewards to one per roll, matching vanilla's own stamina/adrenaline.
    [HarmonyPatch(typeof(Player), nameof(Player.RPC_HitWhileDodging))]
    internal static class SharedPerfectDodgeRewardPatch
    {
        [HarmonyPrefix]
        [UsedImplicitly]
        private static void Prefix(Player __instance, out bool __state)
        {
            __state = __instance.m_beenHitWhileDodging;
        }

        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(Player __instance, bool __state)
        {
            // Already latched (a later hit in the same roll), or vanilla bailed out on !IsOwner().
            if (__state || !__instance.m_beenHitWhileDodging)
            {
                return;
            }

            if (__instance != Player.m_localPlayer)
            {
                return;
            }

            PerfectDodgeGivesHealth.OnPerfectDodge(__instance);
            PerfectDodgeGivesStamina.OnPerfectDodge(__instance);
            PerfectDodgeGivesEitr.OnPerfectDodge(__instance);
            PerfectDodgeGivesSpeed.OnPerfectDodge(__instance);
        }
    }

    // ---- Rewards on a perfect dodge: restore a % of the matching max pool -------------------------
    public static class PerfectDodgeGivesHealth
    {
        public static void OnPerfectDodge(Player player)
        {
            var fraction = player.GetTotalActiveMagicEffectValue(MagicEffectType.PerfectDodgeGivesHealth, 0.01f);
            if (fraction > 0f)
            {
                player.Heal(player.GetMaxHealth() * fraction);
            }
        }
    }

    public static class PerfectDodgeGivesStamina
    {
        public static void OnPerfectDodge(Player player)
        {
            var fraction = player.GetTotalActiveMagicEffectValue(MagicEffectType.PerfectDodgeGivesStamina, 0.01f);
            if (fraction > 0f)
            {
                player.AddStamina(player.GetMaxStamina() * fraction);
            }
        }
    }

    public static class PerfectDodgeGivesEitr
    {
        public static void OnPerfectDodge(Player player)
        {
            var fraction = player.GetTotalActiveMagicEffectValue(MagicEffectType.PerfectDodgeGivesEitr, 0.01f);
            if (fraction > 0f && player.GetMaxEitr() > 0f)
            {
                player.AddEitr(player.GetMaxEitr() * fraction);
            }
        }
    }

    // ---- Trinket proc: a chance, on a dodge roll, to keep the roll's invincibility alive -----------
    // Vanilla's i-frames start when the roll begins and are cut short by the DodgeMortal animation event
    // (Player.OnDodgeMortal clears m_dodgeInvincible). On a proc we simply re-assert m_dodgeInvincible each
    // FixedUpdate for the rest of the roll, before vanilla's UpdateDodge recomputes and replicates the
    // invincibility flag. That means vanilla does everything else for us: it writes ZDOVars.s_dodgeinv so
    // remote attackers see it too, the attacker-side checks in Attack/Projectile skip the hit outright, and
    // the perfect-dodge trigger (and therefore the reward effects above) fires. When the dodge animation
    // ends, vanilla recomputes the flag as false and clears the ZDO on its own -- no manual teardown.
    public static class PerfectDodge
    {
        private static bool _wasInDodge;
        private static bool _procActiveThisRoll;

        [HarmonyPatch(typeof(Player), nameof(Player.UpdateDodge))]
        private static class UpdateDodge_Patch
        {
            // Runs before vanilla computes `inDodgeAnim && m_dodgeInvincible`, so re-asserting the flag
            // here keeps the i-frames (and their replication) alive for the remainder of the roll.
            [UsedImplicitly]
            private static void Prefix(Player __instance)
            {
                if (__instance == Player.m_localPlayer && _procActiveThisRoll)
                {
                    __instance.m_dodgeInvincible = true;
                }
            }

            // Rising-edge on the dodge animation rolls the proc (mirrors RollCleanse's dodge detection).
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

                if (!inDodge)
                {
                    // The roll is over (or never started) -- never let the proc leak past it.
                    _procActiveThisRoll = false;
                }

                if (!rollStarted)
                {
                    return;
                }

                var chance = __instance.GetTotalActiveMagicEffectValue(MagicEffectType.PerfectDodge, 0.01f);
                _procActiveThisRoll = chance > 0f && Random.value < chance;
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
    // Hangs off the same vanilla perfect-dodge trigger as the reward effects above, granting the "Dodge
    // Agility" buff (SE_DodgeAgility) for BuffDuration seconds. Going through a real status effect rather
    // than a bare speed patch means the player gets a HUD icon and tooltip, and the speed itself is applied
    // through vanilla's own StatusEffect.ModifySpeed path. Shard values are authored as whole-number
    // percents, hence the 0.01f.
    public static class PerfectDodgeGivesSpeed
    {
        private const float BuffDuration = 1f; // seconds the speed buff lasts after a perfect dodge

        private const string BuffName = "EL_DodgeAgility";
        private static readonly int BuffHash = BuffName.GetStableHashCode();
        private static SE_DodgeAgility _buffPrototype;
        private static bool _iconMissingLogged;

        public static void OnPerfectDodge(Player player)
        {
            var bonus = player.GetTotalActiveMagicEffectValue(MagicEffectType.PerfectDodgeGivesSpeed, 0.01f);
            if (bonus <= 0f)
            {
                return;
            }

            var prototype = GetOrCreatePrototype();
            if (prototype == null)
            {
                return;
            }

            var seMan = player.GetSEMan();

            // Re-proc while the buff is still up: restamp the bonus (the shard set may have changed) and
            // refresh the countdown rather than letting the old, shorter timer run out.
            if (seMan.GetStatusEffect(BuffHash) is SE_DodgeAgility existing)
            {
                existing.SpeedBonus = bonus;
                existing.ResetTime();
                return;
            }

            // Seed the prototype so the clone SEMan takes carries the current rarity's bonus.
            prototype.SpeedBonus = bonus;
            seMan.AddStatusEffect(prototype);
        }

        // Lazily builds the buff prototype. Runs on a perfect dodge, so the asset bundle is loaded. A null
        // icon would render as an invisible HUD entry (SEMan only surfaces effects with an icon), so if the
        // sprite lookup fails we log once and leave the prototype null.
        private static SE_DodgeAgility GetOrCreatePrototype()
        {
            if (_buffPrototype != null)
            {
                return _buffPrototype;
            }

            // The Pink (Dodge) shardstone's own icon -- same sprite the shard items use (see Shards.cs).
            var icon = EpicAssets.AssetBundle?.LoadAsset<Sprite>("Assets/EpicLoot/Sprites/Shardstones/Pink.png");
            if (icon == null)
            {
                if (!_iconMissingLogged)
                {
                    EpicLoot.LogWarning("PerfectDodgeGivesSpeed: could not load the Pink shardstone sprite; Dodge Agility will not display.");
                    _iconMissingLogged = true;
                }
                return null;
            }

            var se = ScriptableObject.CreateInstance<SE_DodgeAgility>();
            se.name = BuffName;
            se.m_name = "$mod_epicloot_se_dodgeagility";
            se.m_icon = icon;
            se.m_ttl = BuffDuration;
            _buffPrototype = se;
            return _buffPrototype;
        }
    }
}
