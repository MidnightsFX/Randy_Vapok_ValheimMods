using HarmonyLib;

namespace EpicLoot.Magic.MagicItemEffects
{
    [HarmonyPatch]
    internal static class DartingThoughts
    {
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyEitrRegen))]
        public static class ModifyEitrRegen_SEMan_DartingThoughts_Patch
        {
            public static void Postfix(SEMan __instance, ref float eitrMultiplier)
            {
                if (__instance.m_character.IsPlayer() && Player.m_localPlayer != null &&
                    Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.DartingThoughts, out float dartThoughtsValue, 0.01f))
                {
                    // "+X% Eitr Regen": the multiplier already starts at 1, so add only the rolled
                    // fraction (the old line re-added the base AND doubled the roll: 10% -> 2.2x).
                    eitrMultiplier += dartThoughtsValue;
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
        public static class ModifyMaxEitr
        {
            public static void Postfix(Player __instance, ref float eitr)
            {
                if (__instance.HasActiveMagicEffect(MagicEffectType.DartingThoughts, out float dartThoughtsValue, 0.01f))
                {
                    eitr *= (1 - (dartThoughtsValue/2));
                    
                    if (eitr < 0)
                    {
                        eitr = 0;
                    }
                }
            }
        }
    }
}
