using EpicLoot.General;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;

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

        // Tooltip: "Heal {0} per {1} Damage Dealt" -- {1} is the DamagePerTrigger const so the shown
        // threshold stays in sync with the code rather than a baked-in literal.
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.HealthGainPerXDamageDone,
                value => new object[] { value, DamagePerTrigger });
        }

        // Cumulative damage the local player has dealt with the effect active but not yet paid out as a
        // heal. Carries the sub-threshold remainder across hits.
        private static float _accumulatedDamage;

        // Postfix handler invoked by CharacterDamageDispatch (on-hit reaction).
        public static void OnDamageDealt(HitData hit, Character attacker)
        {
            if (!(attacker is Player player) || player != Player.m_localPlayer)
            {
                return;
            }

            // The shard is socketed into the attacking weapon, so read its per-weapon value.
            var weapon = MagicEffectsHelper.GetActiveWeapon(player);
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
