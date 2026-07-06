using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Eikthyr boss shard: the local player's attacks build "charges"; at max charge the next hit discharges a
    // lightning burst on the target. Charges are counted in a Character.Damage postfix (attacker == local
    // player, same layer ChainLightning uses) and the discharge reuses the vanilla "ChainLightning" AoE prefab
    // scaled by the shard's (boss-tier) value.
    //
    // Feedback guard: the discharge AoE is owned by the player, so its own hits route back through
    // Character.Damage as player hits. A short ignore window after each discharge prevents those hits (and the
    // chain) from re-building charges and cascading.
    public static class ShockingCharge
    {
        // Hits required to trigger a discharge, and lightning damage granted per point of shard value
        // (boss ramp is 5..25, so 50..250 lightning per discharge). Both tunable.
        private const int MaxCharges = 5;
        private const float LightningDamagePerTier = 10f;
        private const float DischargeIgnoreWindow = 0.3f;

        private static int _charges;
        private static float _ignoreUntil;

        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        private static class Character_Damage_Patch
        {
            [UsedImplicitly]
            private static void Postfix(Character __instance, HitData hit)
            {
                if (hit == null || hit.GetAttacker() != Player.m_localPlayer || Time.time < _ignoreUntil)
                {
                    return;
                }

                var value = Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.ShockingCharge, 1f);
                if (value <= 0f)
                {
                    _charges = 0;
                    return;
                }

                if (++_charges < MaxCharges)
                {
                    return;
                }

                _charges = 0;
                _ignoreUntil = Time.time + DischargeIgnoreWindow;
                Discharge(__instance, Player.m_localPlayer, value);
            }
        }

        private static void Discharge(Character target, Player player, float value)
        {
            var prefab = ZNetScene.instance?.GetPrefab("ChainLightning");
            if (prefab == null)
            {
                return;
            }

            var instance = Object.Instantiate(prefab, target.GetCenterPoint(), Quaternion.identity);
            var aoe = instance.GetComponent<Aoe>();
            if (aoe != null)
            {
                aoe.m_owner = player;
                aoe.m_damage.m_lightning = value * LightningDamagePerTier;
            }
        }
    }
}
