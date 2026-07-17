using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // LightGreen trinket shard: add to health regen scaled by how full the adrenaline pool is — up to
    // value% extra regen at full adrenaline. Invoked from ModifyPlayerRegen's ModifyHealthRegen postfix.
    // Uses vanilla's adrenaline pool, so it is inert unless the player has a max-adrenaline source. Shard
    // values are authored as whole-number percents, hence the 0.01f.
    public static class AdrenalineIncreasesHealthRegen
    {
        public static void Apply(SEMan seman, ref float regenMultiplier)
        {
            if (seman.m_character != Player.m_localPlayer)
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
