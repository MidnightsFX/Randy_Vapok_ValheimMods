using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Skills;

namespace EpicLoot.MagicItemEffects.Shards 
{
    // uses SkillsAsSkills in AddSkillLevel
    public static class BlockAsDodgeAsBlock 
    {
        public static readonly SkillType[] type = // modify these
        {
            SkillType.Blocking,
            SkillType.Dodge
        };

        public static readonly SkillType[] asType = // from these
        {
            SkillType.Blocking,
            SkillType.Dodge
        };

        public static void RegisterDisplayValues() {


            MagicItem.RegisterDisplayValues(MagicEffectType.BlockAsDodgeAsBlock,
                value => {
                    var player = Player.m_localPlayer;
                    if (player == null) return new object[] { value, 0 };
                    var blockSkill = player.m_skills.GetSkillFactor(SkillType.Blocking);
                    var dodgeSkill = player.m_skills.GetSkillFactor(SkillType.Dodge);
                    var blockBonus = (int)(dodgeSkill * value);
                    var dodgeBonus = (int)(blockSkill * value);
                    return new object[] { value, blockBonus, dodgeBonus};
                });
        }
    }
}
