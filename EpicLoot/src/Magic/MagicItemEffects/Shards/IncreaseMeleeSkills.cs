using SkillType = Skills.SkillType;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Melee-skill bundle shard effect (DarkRed melee slot). The actual bonus is applied by the
    // centralized skill patch in AddSkillLevel.cs (Skills.GetSkillFactor), which also drives the
    // skills-dialog "+X" display -- this class just declares which skills the bundle covers.
    public static class IncreaseMeleeSkills
    {
        // Offensive melee weapon skills. Blocking is intentionally excluded (it has its own
        // AddBlockingSkill effect), as are gathering skills like WoodCutting.
        public static readonly SkillType[] MeleeSkills =
        {
            SkillType.Swords,
            SkillType.Knives,
            SkillType.Clubs,
            SkillType.Polearms,
            SkillType.Spears,
            SkillType.Axes,
            SkillType.Unarmed
        };
    }
}
