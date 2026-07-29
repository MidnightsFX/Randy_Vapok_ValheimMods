using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Magic-weapon shard effect (Red magic-weapon slot): heal a flat amount of health each time the
    // player's cumulative eitr spend with the imbued weapon crosses a threshold. The eitr-cost mirror of
    // HealthGainPerXDamageDone -- a steady trickle that scales with how much you cast rather than how
    // much damage you deal.
    public static class HealthOnEitrUse
    {
        // Eitr spent per heal trigger. Tunable; higher = a slower trickle. The effect value is the health
        // granted per trigger.
        private const float EitrPerTrigger = 100f;

        // Tooltip: "Heal {0} per {1} Eitr Spent" -- {1} is the EitrPerTrigger const so the shown threshold
        // stays in sync with the code rather than a baked-in literal.
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.HealthOnEitrUse,
                value => new object[] { value, EitrPerTrigger });
        }

        // Eitr the local player has spent with the effect active but not yet paid out as a heal. Carries
        // the sub-threshold remainder across casts.
        private static float _accumulatedEitr;

        [HarmonyPatch(typeof(Player), nameof(Player.UseEitr))]
        private static class UseEitr_Patch
        {
            [UsedImplicitly]
            private static void Postfix(Player __instance, float v)
            {
                if (v <= 0f || __instance != Player.m_localPlayer)
                {
                    return;
                }

                // The shard is socketed into the casting weapon, so read its per-weapon value.
                var weapon = MagicEffectsHelper.GetActiveWeapon(__instance);
                if (weapon == null || !weapon.IsMagic())
                {
                    return;
                }

                var healthPerTrigger = MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
                    __instance, weapon, MagicEffectType.HealthOnEitrUse);
                if (healthPerTrigger <= 0f)
                {
                    return;
                }

                _accumulatedEitr += v;
                if (_accumulatedEitr < EitrPerTrigger)
                {
                    return;
                }

                var triggers = (int)(_accumulatedEitr / EitrPerTrigger);
                _accumulatedEitr -= triggers * EitrPerTrigger;
                __instance.Heal(triggers * healthPerTrigger);
            }
        }
    }
}
