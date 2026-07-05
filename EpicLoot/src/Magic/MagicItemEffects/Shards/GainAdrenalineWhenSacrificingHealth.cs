using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // DarkPurple trinket shard: gain adrenaline equal to value% of the health the local player spends on
    // blood-magic costs (Character.UseHealth is only ever the attack blood cost). Runs after
    // ConsumeEitrFirstForBloodCosts, so it is based on the health actually paid. Uses vanilla's
    // adrenaline pool, so it is inert unless the player has a max-adrenaline source. Shard values are
    // authored as whole-number percents, hence the 0.01f.
    public static class GainAdrenalineWhenSacrificingHealth
    {
        [HarmonyPatch(typeof(Character), nameof(Character.UseHealth))]
        private static class UseHealth_Patch
        {
            [UsedImplicitly]
            private static void Postfix(Character __instance, float hp)
            {
                if (hp <= 0f || __instance != Player.m_localPlayer)
                {
                    return;
                }

                var fraction = Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                    MagicEffectType.GainAdrenalineWhenSacrificingHealth, 0.01f);
                if (fraction > 0f)
                {
                    Player.m_localPlayer.AddAdrenaline(hp * fraction);
                }
            }
        }
    }
}
