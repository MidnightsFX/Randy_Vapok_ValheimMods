using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot
{
    public static partial class MagicEffectType
    {
        public static string Sniper = nameof(Sniper);
    }
}

namespace EpicLoot.MagicItemEffects
{
    // Sniper (set items only, NoRoll). The wearer's bow shots deal +value% damage and fly SpeedPerValue x value
    // times as fast, but the bow takes DrawTimePerValue x value times as long to draw. The rarity raises all three.
    //
    // The draw is slowed by lengthening the weapon's draw duration for the length of GetAttackDrawPercentage, not
    // by dividing its result: vanilla clamps the result to 1, so a divided full draw would never complete. Every
    // reader of the draw (crosshair ring, the "drawpercent" animation, stamina drain, and the arrow's velocity,
    // accuracy and damage at release) goes through that method, so a half-drawn shot stays a half-drawn shot.
    // QuickDraw's postfix on the same method still shortens the longer draw.
    //
    // Speed and damage are applied per arrow in Projectile.Setup, on the firing client (the projectile's owner
    // simulates it). Crossbows, staffs and the grappling hook are left alone.
    public static class Sniper
    {
        // Both tunable in this effect's Config block in magiceffects.json, under these key names.
        public const float DefaultSpeedPerValue = 0.05f;    // arrow speed multiplier per point of value
        public const float DefaultDrawTimePerValue = 0.05f; // draw time multiplier per point of value

        private const string SpeedPerValueKey = "SpeedPerValue";
        private const string DrawTimePerValueKey = "DrawTimePerValue";

        private const string GrapplingHookName = "$item_graplinghook";

        // Player-editable, so each is kept non-negative; the multipliers below are floored at 1 on top of that.
        private static float SpeedPerValue => EffectConfig.GetClamped(MagicEffectType.Sniper, SpeedPerValueKey, DefaultSpeedPerValue, 0f, 1f);
        private static float DrawTimePerValue => EffectConfig.GetClamped(MagicEffectType.Sniper, DrawTimePerValueKey, DefaultDrawTimePerValue, 0f, 1f);

        // Tooltip: "... {1}x as fast and deal +{0}% damage, but ... {2}x as long to draw."
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.Sniper,
                value => new object[] { value, GetSpeedMultiplier(value), GetDrawTimeMultiplier(value) });
        }

        // Never below 1, so a bad edit can not slow the arrow or speed up the draw.
        private static float GetSpeedMultiplier(float value)
        {
            return Mathf.Max(1f, value * SpeedPerValue);
        }

        private static float GetDrawTimeMultiplier(float value)
        {
            return Mathf.Max(1f, value * DrawTimePerValue);
        }

        private static bool IsSniperBow(ItemDrop.ItemData item)
        {
            return item != null &&
                   item.m_shared.m_skillType == Skills.SkillType.Bows &&
                   item.m_shared.m_attack.m_bowDraw &&
                   item.m_shared.m_name != GrapplingHookName;
        }

        // Humanoid, not Player: this method runs for every humanoid (Draugr/Fuling archers), and Harmony passes
        // __instance through without a type check. Prefix and finalizer, because m_shared is the descriptor
        // shared by every copy of the bow, so the restore must run even when the original throws.
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GetAttackDrawPercentage))]
        private static class Humanoid_GetAttackDrawPercentage_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Humanoid __instance, ref float __state)
            {
                __state = -1f;
                if (__instance is not Player player || player != Player.m_localPlayer)
                {
                    return;
                }

                ItemDrop.ItemData weapon = player.GetCurrentWeapon();
                if (!IsSniperBow(weapon) || !player.HasActiveMagicEffect(MagicEffectType.Sniper, out float value))
                {
                    return;
                }

                __state = weapon.m_shared.m_attack.m_drawDurationMin;
                weapon.m_shared.m_attack.m_drawDurationMin *= GetDrawTimeMultiplier(value);
            }

            [HarmonyFinalizer]
            private static void Finalizer(Humanoid __instance, float __state)
            {
                if (__state < 0f)
                {
                    return;
                }

                ItemDrop.ItemData weapon = __instance.GetCurrentWeapon();
                if (weapon != null)
                {
                    weapon.m_shared.m_attack.m_drawDurationMin = __state;
                }
            }
        }

        [HarmonyPatch(typeof(Projectile), nameof(Projectile.Setup))]
        private static class Projectile_Setup_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Character owner, ref Vector3 velocity, HitData hitData, ItemDrop.ItemData item)
            {
                if (owner == null || owner != Player.m_localPlayer || !IsSniperBow(item) ||
                    !Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.Sniper, out float value))
                {
                    return;
                }

                velocity *= GetSpeedMultiplier(value);
                hitData?.m_damage.Modify(1f + value / 100f);
            }
        }
    }
}
