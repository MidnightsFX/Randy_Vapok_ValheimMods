namespace EpicLoot.MagicItemEffects.Shards
{
    // DarkRed trinket shard: when the local player's adrenaline fills ("activates"), shave a percentage off
    // the REMAINING cooldown of their Forsaken Power. The reduction is multiplicative on the current value,
    // so it is worth the most right after the power is spent and tapers as the cooldown runs down.
    //
    // Driven by SharedPlayerAddAdrenalinePatch, which owns the fill/pop detection and the local-player /
    // has-an-adrenaline-pool guards. Purely local-player state -- Player.m_guardianPowerCooldown is
    // per-character and saved on the player -- so no RPC is needed, and the HUD cooldown ring picks the
    // change up on its next frame via Player.GetGuardianPowerHUD.
    //
    // Distinct from the rollable DecreaseForsakenCooldown effect, which applies once at activation against
    // the FULL cooldown. Both write the same field and stack multiplicatively.
    public static class AdrenalineCharge
    {
        // Below this the remaining cooldown is not worth tracking; vanilla's per-frame decrement in
        // Player.UpdateGuardianPower would clear it within a second anyway. Cosmetic.
        private const float CooldownFloor = 1f;

        public static void OnAdrenalineActivated(Player player)
        {
            if (player.m_guardianSE == null || player.m_guardianPowerCooldown <= 0f)
            {
                return; // no forsaken power equipped, or it is already off cooldown
            }

            var fraction = player.GetTotalActiveMagicEffectValue(MagicEffectType.AdrenalineCharge, 0.01f);
            if (fraction <= 0f)
            {
                return;
            }

            player.m_guardianPowerCooldown *= 1f - fraction;
            if (player.m_guardianPowerCooldown < CooldownFloor)
            {
                player.m_guardianPowerCooldown = 0f;
            }
        }
    }
}
