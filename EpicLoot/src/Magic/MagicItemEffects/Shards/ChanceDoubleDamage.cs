using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Golden (Fortune) weapon shard effect: a per-hit chance to deal double damage. The chance is read
    // from the attacking weapon (the shard is socketed there), so it only fires for that weapon.
    public static class ChanceDoubleDamage
    {
        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        private static class Character_Damage_Patch
        {
            private static void Prefix(HitData hit)
            {
                if (!(hit.GetAttacker() is Player player) || player != Player.m_localPlayer)
                {
                    return;
                }

                var magicItem = player.GetCurrentWeapon()?.GetMagicItem();
                if (magicItem == null ||
                    !magicItem.HasEffect(MagicEffectType.ChanceDoubleDamage, includeSocketed: true))
                {
                    return;
                }

                float chance = magicItem.GetTotalEffectValue(MagicEffectType.ChanceDoubleDamage, 0.01f);
                if (chance > 0f && Random.value < chance)
                {
                    hit.m_damage.Modify(2f);
                }
            }
        }
    }
}
