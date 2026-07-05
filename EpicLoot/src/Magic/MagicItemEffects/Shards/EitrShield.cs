using EpicLoot.General;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Cyan utility shard: absorb value% of an incoming hit by spending eitr (1 eitr per point absorbed),
    // capped by the eitr available. Mirrors ModifyResistance by scaling the pre-armour hit on the local
    // player. Shard values are authored as whole-number percents, hence the 0.01f.
    public static class EitrShield
    {
        [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
        private static class RPC_Damage_Patch
        {
            [UsedImplicitly]
            private static void Prefix(Character __instance, HitData hit)
            {
                if (hit == null || __instance != Player.m_localPlayer)
                {
                    return;
                }

                var player = Player.m_localPlayer;
                var fraction = player.GetTotalActiveMagicEffectValue(MagicEffectType.EitrShield, 0.01f);
                if (fraction <= 0f)
                {
                    return;
                }

                var eitr = player.GetEitr();
                if (eitr <= 0f)
                {
                    return;
                }

                var total = hit.m_damage.EpicLootGetTotalDamageAgainstPlayer();
                if (total <= 0f)
                {
                    return;
                }

                var absorb = Mathf.Min(total * fraction, eitr);
                if (absorb <= 0f)
                {
                    return;
                }

                player.UseEitr(absorb);
                hit.m_damage.Modify(1f - absorb / total);
            }
        }
    }
}
