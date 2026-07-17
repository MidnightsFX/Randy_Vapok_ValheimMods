namespace EpicLoot.MagicItemEffects.Shards
{
    // Percent-of-pool shard effect (Red legs): add a percentage of the player's max health. Invoked from
    // IncreasePlayerBaseStats' Priority.Last GetTotalFoodValue postfix so it layers on top of the flat pool
    // bonuses -- i.e. the percentage applies to the full food-derived pool, matching "+% max health".
    public static class PercentHealth
    {
        public static void Apply(Player player, ref float hp)
        {
            hp += hp * player.GetTotalActiveMagicEffectValue(MagicEffectType.PercentHealth, 0.01f);
        }
    }
}
