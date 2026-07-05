using EpicLoot.General;
using HarmonyLib;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Weapon shard effect (LightGreen weapon slots): heal a flat amount of health each time the player's
    // cumulative damage dealt with the imbued weapon crosses a threshold. Distinct from LifeSteal (which
    // heals a % of every hit) -- this is a steady flat trickle that scales with how much you fight.
    public static class HealthGainPerXDamageDone
    {
        // Damage dealt per heal trigger. Tunable; higher = a slower trickle. The effect value is the
        // health granted per trigger.
        private const float DamagePerTrigger = 200f;

        // Cumulative damage the local player has dealt with the effect active but not yet paid out as a
        // heal. Carries the sub-threshold remainder across hits.
        private static float _accumulatedDamage;

        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        private static class Character_Damage_Patch
        {
            private static void Postfix(HitData hit)
            {
                if (!(hit.GetAttacker() is Player player) || player != Player.m_localPlayer)
                {
                    return;
                }

                // The shard is socketed into the attacking weapon, so read its per-weapon value.
                var weapon = Attack_Patch.ActiveAttack?.m_weapon ?? player.GetCurrentWeapon();
                if (weapon == null || !weapon.IsMagic())
                {
                    return;
                }

                float healthPerTrigger = MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
                    player, weapon, MagicEffectType.HealthGainPerXDamageDone);
                if (healthPerTrigger <= 0f)
                {
                    return;
                }

                _accumulatedDamage += hit.m_damage.EpicLootGetTotalDamage();
                if (_accumulatedDamage < DamagePerTrigger)
                {
                    return;
                }

                int triggers = (int)(_accumulatedDamage / DamagePerTrigger);
                _accumulatedDamage -= triggers * DamagePerTrigger;
                player.Heal(triggers * healthPerTrigger);
            }
        }
    }
}
