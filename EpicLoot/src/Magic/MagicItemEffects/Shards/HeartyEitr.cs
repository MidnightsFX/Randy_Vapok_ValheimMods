namespace EpicLoot.MagicItemEffects.Shards
{
    // Cyan shoulder shard ("Hearty Eitr"): your vitality feeds your magic -- add a percentage of the
    // player's max Health to their max Eitr. Invoked from IncreasePlayerBaseStats' Priority.Last
    // GetTotalFoodValue postfix, using the already-built max-health value so the bonus reflects health-pool
    // effects. Yields nothing when the player has no eitr pool (nothing to bolster), matching PercentEitr.
    // Shard values are authored as whole-number percents, hence the 0.01f.
    public static class HeartyEitr
    {
        public static void Apply(Player player, float maxHealth, ref float eitr)
        {
            if (eitr <= 0f)
            {
                return;
            }

            eitr += maxHealth * player.GetTotalActiveMagicEffectValue(MagicEffectType.HeartyEitr, 0.01f);
        }
    }
}
