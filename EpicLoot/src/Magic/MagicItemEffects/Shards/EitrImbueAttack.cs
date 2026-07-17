namespace EpicLoot.MagicItemEffects.Shards
{
    // Offensive (Cyan/Eitr weapon slots) shard effect: add spirit damage equal to a share of the hit's
    // physical damage, paid for with eitr on each hit; if the pool can't cover the cost, no bonus and no
    // eitr is spent.
    public static class EitrImbueAttack
    {
        // Eitr paid per point of bonus spirit damage. 1:1 keeps "pay for the damage you get" intuitive
        // and easy to tune here.
        private const float EitrCostPerDamage = 1f;

        // Prefix handler invoked by CharacterDamageDispatch (attacker-side outgoing modifier).
        public static void ModifyOutgoingHit(HitData hit, Character attacker)
        {
            if (!(attacker is Player player) || player != Player.m_localPlayer)
            {
                return;
            }

            // The shard is socketed into the attacking weapon, so read the effect from that weapon
            // rather than player-wide -- the imbue only fires for the weapon that carries it.
            var magicItem = player.GetCurrentWeapon()?.GetMagicItem();
            if (magicItem == null ||
                !magicItem.HasEffect(MagicEffectType.EitrImbueAttack, includeSocketed: true))
            {
                return;
            }

            float fraction = magicItem.GetTotalEffectValue(MagicEffectType.EitrImbueAttack, 0.01f);
            float physical = hit.m_damage.m_blunt + hit.m_damage.m_slash + hit.m_damage.m_pierce;
            float bonus = physical * fraction;
            if (bonus <= 0f)
            {
                return;
            }

            // No bonus unless the pool can fully cover the cost.
            float cost = bonus * EitrCostPerDamage;
            if (player.GetEitr() < cost)
            {
                return;
            }

            player.UseEitr(cost);
            hit.m_damage.m_spirit += bonus;
        }
    }
}
