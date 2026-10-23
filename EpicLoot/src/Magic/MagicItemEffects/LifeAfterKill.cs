using EpicLoot.src.Magic.MagicItemEffects.Helpers;

namespace EpicLoot.MagicItemEffects
{
    // Heals the player a flat amount when a hit of theirs kills a creature.
    public static class LifeAfterKill
    {
        // A cleave or multishot lands several hits on the same victim in one frame, each of which reads as lethal
        // (the owner's health is already at zero, a remote victim's estimate has not moved), so only the first pays.
        private static ZDOID _lastKill = ZDOID.None;

        // Postfix handler invoked by CharacterDamageDispatch (on-hit reaction, attacker side).
        public static void OnDamageDealt(Character target, HitData hit, Character attacker)
        {
            var player = Player.m_localPlayer;
            if (hit == null || player == null || target == player || attacker != player
                || target.IsPlayer() || target.IsTamed())
            {
                return;
            }

            // The memoized total first: against a target another peer owns, the kill check estimates the whole hit.
            if (player.GetTotalActiveMagicEffectValue(MagicEffectType.LifeAfterKill) <= 0f
                || !MagicEffectsHelper.IsLethalHit(target, hit))
            {
                return;
            }

            ZDOID victim = target.m_nview.GetZDO().m_uid;
            if (victim == _lastKill)
            {
                return;
            }
            _lastKill = victim;

            ItemDrop.ItemData weapon = MagicEffectsHelper.GetActiveWeapon(player);
            if (weapon == null)
            {
                return;
            }

            float heal = MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
                player, weapon, MagicEffectType.LifeAfterKill);
            if (heal <= 0f)
            {
                return;
            }

            player.Heal(heal);
        }
    }
}
