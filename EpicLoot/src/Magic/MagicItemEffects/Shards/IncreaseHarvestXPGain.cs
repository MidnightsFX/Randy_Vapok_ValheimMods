using HarmonyLib;
using JetBrains.Annotations;
using SkillType = Skills.SkillType;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to harvesting XP gain for woodcutting and pickaxes
    public static class IncreaseHarvestXPGain {
        [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
        private static class RaiseSkill_Patch {
            [UsedImplicitly]
            private static void Prefix(Skills __instance, SkillType skillType, ref float factor) {
                if (skillType != SkillType.WoodCutting && skillType != SkillType.Pickaxes) {
                    return;
                }

                var bonus = __instance.m_player.GetTotalActiveMagicEffectValue(
                    MagicEffectType.IncreaseHarvestXPGain, 0.01f);
                factor *= 1f + bonus;
            }
        }
    }
}
