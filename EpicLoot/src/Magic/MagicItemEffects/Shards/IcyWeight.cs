using EpicLoot.General;

namespace EpicLoot.MagicItemEffects.Shards
{
    // DarkBlue shoulder shard ("Icy Weight"): add bonus frost damage to the equipped weapon scaled by the
    // movement-speed penalty of the player's gear -- a heavy, slow loadout hits colder. Up to value% of the
    // weapon's damage at a fully-committed heavy loadout, proportionally less below (mirrors
    // AdrenalineIncreasesFrostDamage, but weight-scaled). Applied per-weapon so an off-hand shard does not
    // leak onto the main hand. Shard values are authored as whole-number percents, hence the 0.01f.
    public static class IcyWeight
    {
        // GetDamage postfix handler invoked by ModifyDamage (per-weapon modifier).
        public static void ModifyWeaponDamage(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result)
        {
            var player = Player.m_localPlayer;
            if (player == null || !player.IsItemEquiped(__instance))
            {
                return;
            }

            var fraction = player.GetTotalActiveMagicEffectValue(MagicEffectType.IcyWeight, 0.01f)
                * PenaltyScaling.MovementPenaltyFactor(player);
            if (fraction <= 0f)
            {
                return;
            }

            var bonus = __result.EpicLootGetTotalDamage() * fraction;
            if (bonus > 0f)
            {
                __result.m_frost += bonus;
            }
        }
    }
}
