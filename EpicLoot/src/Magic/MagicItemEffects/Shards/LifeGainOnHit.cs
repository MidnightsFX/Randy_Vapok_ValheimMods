using EpicLoot.General;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Weapon shard effect (Red weapon slots): heal a flat amount of health on every hit landed with the
    // imbued weapon. Distinct from LifeSteal (a % of the damage dealt) and from HealthGainPerXDamageDone
    // (a flat heal per cumulative damage threshold) -- this pays out the same amount per swing regardless
    // of how hard the hit landed, so it favors fast, multi-hit weapons over big single strikes.
    public static class LifeGainOnHit
    {
        // Postfix handler invoked by CharacterDamageDispatch (on-hit reaction, attacker side). Only fires for
        // hits on Characters -- destructibles (trees, rocks) never route through Character.Damage.
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

            var heal = MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
                player, weapon, MagicEffectType.LifeGainOnHit);
            if (heal <= 0f)
            {
                return;
            }

            // Don't pay out on whiffs -- a hit that landed for nothing (fully resisted, immune target)
            // shouldn't heal.
            if (hit.m_damage.EpicLootGetTotalDamage() <= 0f)
            {
                return;
            }

            player.Heal(heal);
        }
    }
}
