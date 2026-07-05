using HarmonyLib;
using JetBrains.Annotations;
using SkillType = Skills.SkillType;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Gather shard (utility slot): +% skill XP from the two gathering skills — WoodCutting (chopping)
    // and Pickaxes (mining) — the counterparts to IncreaseHarvestDamage's chop/pickaxe boost. Mirrors
    // QuickLearner but only scales those skills. Shard values are authored as whole-number percents,
    // hence the 0.01f.
    public static class IncreaseHarvestXPGain
    {
        [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
        private static class RaiseSkill_Patch
        {
            [UsedImplicitly]
            private static void Prefix(Skills __instance, SkillType skillType, ref float factor)
            {
                if (skillType != SkillType.WoodCutting && skillType != SkillType.Pickaxes)
                {
                    return;
                }

                var bonus = __instance.m_player.GetTotalActiveMagicEffectValue(
                    MagicEffectType.IncreaseHarvestXPGain, 0.01f);
                factor *= 1f + bonus;
            }
        }
    }
}
