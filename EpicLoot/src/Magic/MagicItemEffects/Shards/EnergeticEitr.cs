namespace EpicLoot.MagicItemEffects.Shards
{
    // Yellow magic-weapon shard, the stamina mirror of HeartyEitr ("Energetic Eitr"): your vigor feeds your
    // magic -- add a percentage of the
    // player's max Stamina to their max Eitr. Invoked from IncreasePlayerBaseStats' Priority.Last
    // GetTotalFoodValue postfix, using the already-built max-stamina value so the bonus reflects stamina-pool
    // effects. Yields nothing when the player has no eitr pool (nothing to bolster), matching HeartyEitr.
    // Shard values are authored as whole-number percents, hence the 0.01f.
    public static class EnergeticEitr
    {
        public static void Apply(Player player, float maxStamina, ref float eitr)
        {
            if (eitr <= 0f)
            {
                return;
            }

            eitr += maxStamina * player.GetTotalActiveMagicEffectValue(MagicEffectType.EnergeticEitr, 0.01f);
        }
    }
}
