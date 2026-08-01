using HarmonyLib;

namespace EpicLoot.src.Magic.MagicItemEffects.Shards 
{
    [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]

    public class IncreasedXPGainFromBlockDayNight 
    {
        private static void Prefix(Skills __instance, ref float factor) 
        {
            if (EnvMan.IsDay()) 
            {
                var dayEffectBonus = 1f + __instance.m_player.GetTotalActiveMagicEffectValue(MagicEffectType.DayBlocker, .01f);
                factor *= dayEffectBonus;
            }
            if (EnvMan.IsNight()) 
            {
                var nightEffectBonus = 1f + __instance.m_player.GetTotalActiveMagicEffectValue(MagicEffectType.NightBlocker, .01f);
                factor *= nightEffectBonus;
            }
        }
    }
}
