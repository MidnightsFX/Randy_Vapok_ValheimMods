using HarmonyLib;

namespace EpicLoot.src.Magic.MagicItemEffects.Shards 
{
    public static class IncreasedXPGainFromBlockDayNight 
    {
        [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
        public class DayNightBlockXP_Patch 
        {
            [HarmonyPrefix]
            public static void GainDayNightXPOnBlock(Skills.SkillType skillType, ref float factor) 
            {
                var player = Player.m_localPlayer;
                var skill = player.GetSkills().GetSkill(Skills.SkillType.Blocking);

                // not exactly a fan guarding by SkillType.Blocking as its not on block but just at night all blocking skill increased + X%
                // will change to on block only if future interactions appear

                if (skillType == Skills.SkillType.Blocking && EnvMan.IsDay())  
                {
                    var dayEffectBonus = 1f + player.GetTotalActiveMagicEffectValue(MagicEffectType.DayBlocker, .01f);
                    factor *= dayEffectBonus;
                    Jotunn.Logger.LogWarning($"[Dayblocker] Base {dayEffectBonus}, total {skill.m_accumulator:F3}");
                }
                if (skillType == Skills.SkillType.Blocking && EnvMan.IsNight()) 
                {
                    var nightEffectBonus = 1f + player.GetTotalActiveMagicEffectValue(MagicEffectType.NightBlocker, .01f);
                    factor *= nightEffectBonus;
                    Jotunn.Logger.LogWarning($"[Nightblocker] {nightEffectBonus}, total {skill.m_accumulator:F3}");
                }
            }
        }
    }
}
