using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // LightGreen trinket shard: add to health regen scaled by how full the adrenaline pool is — up to
    // value% extra regen at full adrenaline. Hooks SEMan.ModifyHealthRegen like ModifyPlayerRegen. Uses
    // vanilla's adrenaline pool, so it is inert unless the player has a max-adrenaline source. Shard
    // values are authored as whole-number percents, hence the 0.01f.
    public static class AdrenalineIncreasesHealthRegen
    {
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyHealthRegen))]
        private static class ModifyHealthRegen_Patch
        {
            [UsedImplicitly]
            private static void Postfix(SEMan __instance, ref float regenMultiplier)
            {
                if (__instance.m_character != Player.m_localPlayer)
                {
                    return;
                }

                var player = Player.m_localPlayer;
                var maxAdrenaline = player.GetMaxAdrenaline();
                if (maxAdrenaline <= 0f)
                {
                    return;
                }

                var fraction = player.GetTotalActiveMagicEffectValue(MagicEffectType.AdrenalineIncreasesHealthRegen, 0.01f);
                if (fraction > 0f)
                {
                    regenMultiplier += fraction * Mathf.Clamp01(player.GetAdrenaline() / maxAdrenaline);
                }
            }
        }
    }
}
