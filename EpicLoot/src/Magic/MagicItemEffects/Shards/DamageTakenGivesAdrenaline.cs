using EpicLoot.General;
using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Red trinket shard: gain adrenaline equal to value% of the damage the local player takes. Hooks
    // RPC_Damage (the local player is the victim), so it reads damage after other on-damage effects.
    // Uses vanilla's adrenaline pool, so it is inert unless the player has a max-adrenaline source.
    // Shard values are authored as whole-number percents, hence the 0.01f.
    public static class DamageTakenGivesAdrenaline
    {
        [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
        private static class RPC_Damage_Patch
        {
            [UsedImplicitly]
            private static void Postfix(Character __instance, HitData hit)
            {
                if (hit == null || __instance != Player.m_localPlayer)
                {
                    return;
                }

                var fraction = Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                    MagicEffectType.DamageTakenGivesAdrenaline, 0.01f);
                if (fraction <= 0f)
                {
                    return;
                }

                var damage = hit.m_damage.EpicLootGetTotalDamageAgainstPlayer();
                if (damage > 0f)
                {
                    Player.m_localPlayer.AddAdrenaline(damage * fraction);
                }
            }
        }
    }
}
