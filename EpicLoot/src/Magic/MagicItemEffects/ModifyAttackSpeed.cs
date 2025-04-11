﻿using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(Game), nameof(Game.Awake))]
    public static class ModifyAttackSpeed_ApplyAnimationHandler_Patch
    {
        internal static bool appliedAttackSpeed = false;
        public static double ModifyAttackSpeed(Character character, double speed)
        {
            if (character is Player player && player.InAttack() && player.m_currentAttack != null)
            {
                ModifyWithLowHealth.Apply(player, MagicEffectType.ModifyAttackSpeed, effect =>
                {
                    var value = player.GetTotalActiveMagicEffectValue(effect, 0.01f);
                    speed *= (1.0d + value);
                });
            }

            return speed;
        }

        [UsedImplicitly]
        private static void Postfix(Game __instance)
        {
            // EpicLoot.Log($"Applying ModifyAttackSpeed patch");
            if (appliedAttackSpeed == false)
            {
                AnimationSpeedManager.Add((character, speed) => ModifyAttackSpeed(character, speed));
                appliedAttackSpeed = true;
            }
            
        }
    }
}
