using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Purple chest shard: pay value% of a blood-magic health cost from eitr first (capped by available
    // eitr), reducing the health actually spent. Character.UseHealth is only ever called for attack
    // blood costs, so this never touches damage taken. Shard values are authored as whole-number
    // percents, hence the 0.01f.
    public static class ConsumeEitrFirstForBloodCosts
    {
        [HarmonyPatch(typeof(Character), nameof(Character.UseHealth))]
        private static class UseHealth_Patch
        {
            [UsedImplicitly]
            private static void Prefix(Character __instance, ref float hp)
            {
                if (hp <= 0f || __instance != Player.m_localPlayer)
                {
                    return;
                }

                var player = Player.m_localPlayer;
                var fraction = player.GetTotalActiveMagicEffectValue(MagicEffectType.ConsumeEitrFirstForBloodCosts, 0.01f);
                if (fraction <= 0f)
                {
                    return;
                }

                var covered = Mathf.Min(hp * fraction, player.GetEitr());
                if (covered > 0f)
                {
                    player.UseEitr(covered);
                    hp -= covered;
                }
            }
        }
    }
}
