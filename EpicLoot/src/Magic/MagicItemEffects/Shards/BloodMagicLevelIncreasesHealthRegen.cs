using HarmonyLib;
using JetBrains.Annotations;
using SkillType = Skills.SkillType;

namespace EpicLoot.MagicItemEffects.Shards
{
    // DarkPurple legs shard (Blood Magic): +% health regen that scales with the player's Blood Magic skill.
    // Hooks SEMan.ModifyHealthRegen (the same additive multiplier ModifyPlayerRegen / DayHealthRegen use). The
    // bonus is the shard value scaled by the skill factor (0-1), so it ramps from nothing at skill 0 to the
    // full shard value at skill 100 and stays bounded. Shard values are authored as whole-number percents,
    // hence the 0.01f.
    public static class BloodMagicLevelIncreasesHealthRegen
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

                var perFullSkill = Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                    MagicEffectType.BloodMagicLevelIncreasesHealthRegen, 0.01f);
                if (perFullSkill <= 0f)
                {
                    return;
                }

                var skills = Player.m_localPlayer.GetSkills();
                var skillFactor = skills != null ? skills.GetSkillFactor(SkillType.BloodMagic) : 0f;
                regenMultiplier += perFullSkill * skillFactor;
            }
        }
    }
}
