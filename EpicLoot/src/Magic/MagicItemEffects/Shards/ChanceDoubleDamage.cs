using EpicLoot.General;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Golden (Fortune) weapon shard effect: a per-hit chance to deal double damage. The chance is read
    // from the attacking weapon (the shard is socketed there), so it only fires for that weapon.
    public static class ChanceDoubleDamage
    {
        // Prefix handler invoked by CharacterDamageDispatch (attacker-side outgoing modifier).
        public static void ModifyOutgoingHit(HitData hit, Character attacker)
        {
            if (!(attacker is Player player) || player != Player.m_localPlayer)
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
                DamageText.instance.ShowText(DamageText.TextType.Weak, hit.m_point, $"+{hit.m_damage.EpicLootGetTotalDamage()}", true);
                hit.m_damage.Modify(2f);
            }
        }
    }
}
