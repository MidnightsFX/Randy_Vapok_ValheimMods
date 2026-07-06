using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Bonemass boss shard: the local player's hits are coated in venom, and in return the wearer shrugs off
    // much of the poison thrown back at them. Outgoing poison is injected in a Character.Damage prefix so it
    // rides along with the serialized hit to the target, where vanilla folds it into a single stacking Poison
    // DoT (Character.AddPoisonDamage). Incoming poison is trimmed in the local player's RPC_Damage prefix
    // (mirrors EitrShield). The boss-tier shard value (5..25) scales both the venom and the resistance.
    //
    // No feedback loop: the Poison DoT ticks through Character.ApplyDamage, not Character.Damage/RPC_Damage,
    // so neither patch below re-fires on the poison it applies.
    public static class PoisonCoating
    {
        private const float PoisonPerTier = 3f;    // poison added per hit, per point of value (5..25 -> 15..75)
        private const float ResistPerTier = 0.03f; // incoming poison reduced per point of value (5..25 -> 15%..75%)

        // Coats the local player's outgoing hits with stacking poison.
        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        private static class Damage_Patch
        {
            [UsedImplicitly]
            private static void Prefix(Character __instance, HitData hit)
            {
                if (hit == null || __instance == Player.m_localPlayer || hit.GetAttacker() != Player.m_localPlayer)
                {
                    return;
                }

                var value = Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.PoisonCoating, 1f);
                if (value > 0f)
                {
                    hit.m_damage.m_poison += value * PoisonPerTier;
                }
            }
        }

        // Grants the wearer poison resistance by trimming the poison portion of incoming hits.
        [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
        private static class RPC_Damage_Patch
        {
            [UsedImplicitly]
            private static void Prefix(Character __instance, HitData hit)
            {
                if (hit == null || __instance != Player.m_localPlayer || hit.m_damage.m_poison <= 0f)
                {
                    return;
                }

                var value = Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.PoisonCoating, 1f);
                if (value > 0f)
                {
                    hit.m_damage.m_poison *= Mathf.Min(0.5f, 1f - Mathf.Clamp01(value * ResistPerTier));
                }
            }
        }
    }
}
