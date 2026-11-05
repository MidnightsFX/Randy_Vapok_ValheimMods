using EpicLoot.General;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    public static class AddEitrLeech
    {
        // Postfix handler invoked by CharacterDamageDispatch (on-hit reaction).
        public static void OnDamageDealt(HitData hit, Character attacker, float healthBeforeHit)
        {
            if (attacker == null || attacker is not Player player)
            {
                return;
            }

            var weapon = MagicEffectsHelper.GetActiveWeapon(player);
            if (weapon == null || !weapon.IsMagic())
            {
                return;
            }

            if (MagicEffectsHelper.HasActiveMagicEffectOnWeapon(
                player, weapon, MagicEffectType.EitrLeech, out float eitrleechMultiplier, 0.01f))
            {
                // Overkill does not count: a huge hit on a nearly dead target pays for the health it took.
                float counted = Mathf.Min(hit.m_damage.EpicLootGetTotalDamage(), healthBeforeHit);
                player.AddEitr(counted * eitrleechMultiplier);
            }
        }
    }
}