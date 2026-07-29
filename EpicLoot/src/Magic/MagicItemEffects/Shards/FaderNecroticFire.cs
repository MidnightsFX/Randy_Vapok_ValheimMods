using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Fader boss shard: a weapon's physical bite rots away into necrotic fire. On the weapon's OUTGOING
    // damage, strip ALL physical damage (blunt + slash + pierce) and add a rarity-scaled share of that
    // pool to BOTH fire and poison -- so the weapon's physical damage is removed and reborn as fire +
    // poison. The conversion factor comes from the shard's per-rarity value (Mythic == 1.0 = the full
    // physical amount to each, a net damage gain fitting a Mythic-only boss shard; lower rarities convert
    // proportionally less). Runs as a GetDamage postfix so the weapon tooltip reflects it too, gated to
    // the local player's equipped weapon (mirrors ConvertPhysicalDamageToLightning).
    public static class NecroticFire {
        // GetDamage postfix handler invoked by ModifyDamage (per-weapon modifier).
        public static void ModifyWeaponDamage(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result) {
            // Only when the local player has this weapon equipped (also gates the weapon tooltip).
            if (!ModifyDamage.RunGetDamagePatch(__instance)) {
                return;
            }

            // Conversion factor scales with shard rarity (config ramp: Mythic == 1.0, lower rarities
            // less) -- the fraction of the stripped physical pool reborn as EACH of fire and poison.
            float factor = Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.NecroticFire, 1f);
            if (factor <= 0f) {
                return;
            }

            float physical = __result.m_blunt + __result.m_slash + __result.m_pierce;
            if (physical <= 0f) {
                return;
            }

            // All physical is consumed regardless of factor; the factor only sets the payout.
            DamageConversionHelper.RemovePhysicalShare(ref __result, 1f);

            __result.m_fire += physical * factor;
            __result.m_poison += physical * factor;
        }
    }
}
