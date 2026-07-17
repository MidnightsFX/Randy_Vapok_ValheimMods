using EpicLoot.General;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // DarkBlue trinket shard: add bonus frost damage to the equipped weapon scaled by how full the
    // adrenaline pool is — up to value% of the weapon's damage at full adrenaline, proportionally less
    // below. Uses vanilla's adrenaline pool, so it is inert unless the player has a max-adrenaline
    // source. Shard values are authored as whole-number percents, hence the 0.01f.
    public static class AdrenalineIncreasesFrostDamage
    {
        // GetDamage postfix handler invoked by ModifyDamage (per-weapon modifier).
        public static void ModifyWeaponDamage(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result)
        {
            var player = Player.m_localPlayer;
            if (player == null || !player.IsItemEquiped(__instance))
            {
                return;
            }

            var maxAdrenaline = player.GetMaxAdrenaline();
            if (maxAdrenaline <= 0f)
            {
                return;
            }

            var fraction = player.GetTotalActiveMagicEffectValue(MagicEffectType.AdrenalineIncreasesFrostDamage, 0.01f);
            if (fraction <= 0f)
            {
                return;
            }

            var bonus = __result.EpicLootGetTotalDamage() * fraction * Mathf.Clamp01(player.GetAdrenaline() / maxAdrenaline);
            if (bonus > 0f)
            {
                __result.m_frost += bonus;
            }
        }
    }
}
