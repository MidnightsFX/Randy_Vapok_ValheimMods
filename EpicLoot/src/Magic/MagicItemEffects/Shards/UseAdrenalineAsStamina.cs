using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Yellow trinket shard: when a stamina cost exceeds current stamina, pay up to value% of the
    // shortfall from the adrenaline pool (1:1) instead, letting the stamina-gated action proceed. Uses
    // vanilla's adrenaline pool, so it is inert unless the player has a max-adrenaline source. Shard
    // values are authored as whole-number percents, hence the 0.01f.
    public static class UseAdrenalineAsStamina
    {
        [HarmonyPatch(typeof(Player), nameof(Player.UseStamina))]
        private static class UseStamina_Patch
        {
            [UsedImplicitly]
            private static void Prefix(Player __instance, ref float v)
            {
                if (v <= 0f || __instance != Player.m_localPlayer)
                {
                    return;
                }

                var stamina = __instance.GetStamina();
                if (v <= stamina)
                {
                    return; // enough stamina; nothing to cover
                }

                var fraction = __instance.GetTotalActiveMagicEffectValue(MagicEffectType.UseAdrenalineAsStamina, 0.01f);
                if (fraction <= 0f)
                {
                    return;
                }

                var pay = Mathf.Min((v - stamina) * fraction, __instance.GetAdrenaline());
                if (pay > 0f)
                {
                    __instance.AddAdrenaline(-pay);
                    v -= pay; // that much of the cost is paid from adrenaline instead of stamina
                }
            }
        }
    }
}
