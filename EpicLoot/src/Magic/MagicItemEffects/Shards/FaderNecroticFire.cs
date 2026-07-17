using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Fader boss shard: a weapon's deadliest damage decays into necrotic fire. On the weapon's OUTGOING
    // damage, find the single highest damage type, strip it entirely, and add a rarity-scaled share of
    // that amount to BOTH fire and poison -- so the top type is removed and reborn as fire + poison. The
    // conversion factor comes from the shard's per-rarity value (Mythic == 1.0 = the full amount to each,
    // a net damage gain fitting a Mythic-only boss shard; lower rarities convert proportionally less).
    // Runs as a GetDamage postfix so the weapon tooltip reflects it too, gated to the local player's
    // equipped weapon (mirrors ConvertPhysicalDamageToLightning).
    public static class NecroticFire {
        // GetDamage postfix handler invoked by ModifyDamage (per-weapon modifier).
        public static void ModifyWeaponDamage(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result) {
            // Only when the local player has this weapon equipped (also gates the weapon tooltip).
            if (!ModifyDamage.RunGetDamagePatch(__instance)) {
                return;
            }

            // Conversion factor scales with shard rarity (config ramp: Mythic == 1.0, lower rarities
            // less) -- the fraction of the stripped amount reborn as EACH of fire and poison.
            float factor = Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.NecroticFire, 1f);
            if (factor <= 0f) {
                return;
            }

            float highest = StripHighestDamageType(ref __result);
            if (highest <= 0f) {
                return;
            }

            __result.m_fire += highest * factor;
            __result.m_poison += highest * factor;
        }

        // Zeroes the single highest of the weapon's combat damage types (physical + elemental) and returns
        // its magnitude. Tool damage (chop/pickaxe) and true damage are intentionally not candidates; ties
        // resolve to the first type in this order.
        private static float StripHighestDamageType(ref HitData.DamageTypes d) {
            float highest = Mathf.Max(d.m_blunt, d.m_slash, d.m_pierce, d.m_fire,
                d.m_frost, d.m_lightning, d.m_poison, d.m_spirit);
            if (highest <= 0f) {
                return 0f;
            }

            if (d.m_blunt == highest) d.m_blunt = 0f;
            else if (d.m_slash == highest) d.m_slash = 0f;
            else if (d.m_pierce == highest) d.m_pierce = 0f;
            else if (d.m_fire == highest) d.m_fire = 0f;
            else if (d.m_frost == highest) d.m_frost = 0f;
            else if (d.m_lightning == highest) d.m_lightning = 0f;
            else if (d.m_poison == highest) d.m_poison = 0f;
            else d.m_spirit = 0f;

            return highest;
        }
    }
}
