using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // LightBlue trinket shard: increase adrenaline gained by value% while a storm is blowing (high wind).
    // Hooks SEMan.ModifyAdrenaline, which vanilla applies to every positive adrenaline gain. Uses
    // vanilla's adrenaline pool, so it is inert unless the player has a max-adrenaline source. Shard
    // values are authored as whole-number percents, hence the 0.01f.
    public static class IncreaseAdrenalineGainDuringStorm
    {
        private const float StormWindThreshold = 0.8f;

        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyAdrenaline))]
        private static class ModifyAdrenaline_Patch
        {
            [UsedImplicitly]
            private static void Postfix(SEMan __instance, float baseValue, ref float use)
            {
                if (baseValue <= 0f || use <= 0f || __instance.m_character != Player.m_localPlayer)
                {
                    return;
                }

                if (EnvMan.instance == null || EnvMan.instance.GetWindIntensity() < StormWindThreshold)
                {
                    return;
                }

                var fraction = Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                    MagicEffectType.IncreaseAdrenalineGainDuringStorm, 0.01f);
                if (fraction > 0f)
                {
                    use *= 1f + fraction;
                }
            }
        }
    }
}
