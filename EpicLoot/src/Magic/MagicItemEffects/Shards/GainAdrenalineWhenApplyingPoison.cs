using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // DarkGreen trinket shard: dealing poison damage builds adrenaline — gain adrenaline equal to value%
    // of the poison damage the local player deals. Hooks Character.Damage (runs on the attacker's client)
    // and resolves the attacker from the hit. Uses vanilla's adrenaline pool, so it is inert unless the
    // player has a max-adrenaline source. Shard values are authored as whole-number percents.
    public static class GainAdrenalineWhenApplyingPoison
    {
        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        private static class Damage_Patch
        {
            [UsedImplicitly]
            private static void Postfix(HitData hit)
            {
                if (hit == null || hit.m_damage.m_poison <= 0f || hit.GetAttacker() != Player.m_localPlayer)
                {
                    return;
                }

                var fraction = Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                    MagicEffectType.GainAdrenalineWhenApplyingPoison, 0.01f);
                if (fraction > 0f)
                {
                    Player.m_localPlayer.AddAdrenaline(hit.m_damage.m_poison * fraction);
                }
            }
        }
    }
}
