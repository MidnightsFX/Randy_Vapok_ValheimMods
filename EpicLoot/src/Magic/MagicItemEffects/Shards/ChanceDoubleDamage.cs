using EpicLoot.General;
using Jotunn.Managers;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a chance to double the damage of an attack.
    public static class ChanceDoubleDamage {
        // Prefix handler invoked by CharacterDamageDispatch (attacker-side outgoing modifier).
        static GameObject effect = null;
        public static void ModifyOutgoingHit(HitData hit, Character attacker) {
            if (!(attacker is Player player) || player != Player.m_localPlayer) {
                return;
            }

            var magicItem = player.GetCurrentWeapon()?.GetMagicItem();
            if (magicItem == null ||
                !magicItem.HasEffect(MagicEffectType.ChanceDoubleDamage, includeSocketed: true)) {
                return;
            }

            float chance = magicItem.GetTotalEffectValue(MagicEffectType.ChanceDoubleDamage, 0.01f);
            if (chance > 0f && Random.value < chance) {
                if (effect == null) {
                    effect = PrefabManager.Instance.GetPrefab("sfx_archery_target_hit");
                }
                if (effect != null) {
                    GameObject.Instantiate(effect, hit.m_point, Quaternion.identity);
                }
                DamageText.instance.ShowText(DamageText.TextType.Bonus, hit.m_point, $"+{Mathf.RoundToInt(hit.m_damage.EpicLootGetTotalDamage())}", true);
                hit.m_damage.Modify(2f);
            }
        }
    }
}
