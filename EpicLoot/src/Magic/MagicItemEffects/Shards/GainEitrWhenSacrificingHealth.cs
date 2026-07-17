using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // DarkPurple shoulder shard: gain Eitr equal to value% of the health the local player spends on
    // blood-magic costs (Character.UseHealth is only ever the attack blood cost). Mirrors
    // GainAdrenalineWhenSacrificingHealth but feeds the eitr pool; inert if the player has no eitr. Shard
    // values are authored as whole-number percents, hence the 0.01f.
    public static class GainEitrWhenSacrificingHealth
    {
        [HarmonyPatch(typeof(Character), nameof(Character.UseHealth))]
        private static class UseHealth_Patch
        {
            [UsedImplicitly]
            private static void Postfix(Character __instance, float hp)
            {
                var player = Player.m_localPlayer;
                if (hp <= 0f || __instance != player || player.GetMaxEitr() <= 0f)
                {
                    return;
                }

                var fraction = player.GetTotalActiveMagicEffectValue(
                    MagicEffectType.GainEitrWhenSacrificingHealth, 0.01f);
                if (fraction > 0f)
                {
                    player.AddEitr(hp * fraction);
                }
            }
        }
    }
}
