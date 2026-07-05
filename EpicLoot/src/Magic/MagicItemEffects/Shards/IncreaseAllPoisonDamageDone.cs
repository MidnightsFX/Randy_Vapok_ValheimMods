using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // DarkGreen utility shard: +% to ALL poison damage the local player deals. Character.Damage carries the
    // outgoing hit (the same layer Executioner hooks) before it is sent to the target, where the poison
    // portion is extracted and turned into an SE_Poison DoT via AddPoisonDamage. Scaling hit.m_damage.m_poison
    // here therefore boosts both direct poison and the resulting DoT total. It does NOT double-dip on the DoT
    // ticks: SE_Poison ticks call Character.ApplyDamage directly (bypassing Character.Damage) with no attacker.
    // Player-wide effect (socketed into a utility item), so we read the summed player value. Shard values are
    // authored as whole-number percents, hence the 0.01f.
    public static class IncreaseAllPoisonDamageDone
    {
        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        private static class Character_Damage_Patch
        {
            [UsedImplicitly]
            private static void Prefix(HitData hit)
            {
                if (hit == null || hit.m_damage.m_poison <= 0f || hit.GetAttacker() != Player.m_localPlayer)
                {
                    return;
                }

                var bonus = Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                    MagicEffectType.IncreaseAllPoisonDamageDone, 0.01f);
                if (bonus > 0f)
                {
                    hit.m_damage.m_poison *= 1f + bonus;
                }
            }
        }
    }
}
