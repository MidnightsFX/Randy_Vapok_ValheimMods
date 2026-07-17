namespace EpicLoot.MagicItemEffects.Shards
{
    // Peach (Logistics) head shard: converts a slice of the player's max health into extra max stamina.
    // Invoked from IncreasePlayerBaseStats' GetTotalFoodValue postfix (like its IncreaseStamina) so it feeds
    // both the stamina pool and its HUD bar, using the food-derived max health (hp) as the base. Shard
    // values are authored as whole-number percents (of max health), hence the 0.01f.
    public static class GainMaxStaminaBasedOnPlayerMaxHealth
    {
        public static void Apply(Player player, float hp, ref float stamina)
        {
            if (player != Player.m_localPlayer)
            {
                return;
            }

            stamina += hp * player.GetTotalActiveMagicEffectValue(
                MagicEffectType.GainMaxStaminaBasedOnPlayerMaxHealth, 0.01f);
        }
    }
}
