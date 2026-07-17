namespace EpicLoot.MagicItemEffects.Shards
{
    // Percent-of-pool shard effect (Yellow head): add a percentage of the player's max stamina. Invoked
    // from IncreasePlayerBaseStats' Priority.Last GetTotalFoodValue postfix so it layers on top of the flat
    // pool bonuses -- i.e. the percentage applies to the full food-derived pool, matching "+% max stamina".
    public static class PercentStamina
    {
        public static void Apply(Player player, ref float stamina)
        {
            stamina += stamina * player.GetTotalActiveMagicEffectValue(MagicEffectType.PercentStamina, 0.01f);
        }
    }
}
