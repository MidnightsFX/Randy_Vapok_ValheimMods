using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Golden shoulder shard: when crafting/building, each required material has a value% chance not to be
    // consumed. Prefixes Player.ConsumeResources and swaps in a fresh requirements array whose skipped
    // entries carry a zero amount -- never mutating the caller's shared recipe Requirement objects. Shard
    // values are authored as whole-number percents, hence the 0.01f.
    public static class LuckyCraft
    {
        [HarmonyPatch(typeof(Player), nameof(Player.ConsumeResources))]
        private static class Player_ConsumeResources_Patch
        {
            [UsedImplicitly]
            private static void Prefix(Player __instance, ref Piece.Requirement[] requirements)
            {
                if (requirements == null || __instance != Player.m_localPlayer)
                {
                    return;
                }

                var chance = __instance.GetTotalActiveMagicEffectValue(MagicEffectType.LuckyCraft, 0.01f);
                if (chance <= 0f)
                {
                    return;
                }

                var replacement = new Piece.Requirement[requirements.Length];
                for (var i = 0; i < requirements.Length; i++)
                {
                    var req = requirements[i];
                    if (req?.m_resItem != null && Random.value < chance)
                    {
                        // Copy with a zeroed amount so this one material is skipped; the original recipe
                        // Requirement is left untouched.
                        replacement[i] = new Piece.Requirement
                        {
                            m_resItem = req.m_resItem,
                            m_amount = 0,
                            m_amountPerLevel = 0,
                            m_recover = req.m_recover
                        };
                    }
                    else
                    {
                        replacement[i] = req;
                    }
                }

                requirements = replacement;
            }
        }
    }
}
