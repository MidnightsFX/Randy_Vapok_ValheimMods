namespace EpicLoot.MagicItemEffects.Shards
{
    // Percent-of-pool shard effect (Cyan head): add a percentage of the player's max eitr. Invoked from
    // IncreasePlayerBaseStats' Priority.Last GetTotalFoodValue postfix so it layers on top of the flat pool
    // bonuses -- i.e. the percentage applies to the full food-derived pool, matching "+% max eitr". Yields
    // nothing when the player has no eitr pool (percentage of zero), inherent to a percent-of-pool effect.
    public static class PercentEitr
    {
        public static void Apply(Player player, ref float eitr)
        {
            eitr += eitr * player.GetTotalActiveMagicEffectValue(MagicEffectType.PercentEitr, 0.01f);
        }
    }
}
