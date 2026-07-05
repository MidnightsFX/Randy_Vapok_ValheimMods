using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // DarkRed chest shard (Wrath/Berserk): each of the local player's hits has a % chance to "crit",
    // multiplying the whole hit's damage. Rolled in a Character.Damage prefix — the outgoing hit before it is
    // sent to the target (runs once, on the attacker's client; Character.Damage forwards to RPC_Damage on the
    // owner). DoT ticks (fire/poison) go through ApplyDamage with no attacker, so they never roll a crit.
    // Player-wide effect (chest armor), so we read the summed player value. The shard value is the crit chance
    // authored as a whole-number percent, hence the 0.01f.
    public static class ChanceToCritOnHit
    {
        // Damage multiplier applied on a successful crit. Tunable; 2 = double damage on crit. This is distinct
        // in intent from ChanceDoubleDamage (a Fortune-shard proc) even though the default multiplier matches.
        private const float CritDamageMultiplier = 2f;

        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        private static class Character_Damage_Patch
        {
            [UsedImplicitly]
            private static void Prefix(HitData hit)
            {
                if (hit == null || hit.GetAttacker() != Player.m_localPlayer)
                {
                    return;
                }

                var chance = Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                    MagicEffectType.ChanceToCritOnHit, 0.01f);
                if (chance > 0f && Random.value < chance)
                {
                    hit.m_damage.Modify(CritDamageMultiplier);
                }
            }
        }
    }
}
