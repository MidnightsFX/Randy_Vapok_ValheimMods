using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Yellow trinket shard: when a stamina cost exceeds current stamina, convert adrenaline into stamina
    // to cover the shortfall, spending up to the entire adrenaline pool. The shard value is the
    // conversion efficiency (20/40/60/80/100% by rarity), so 1 adrenaline pays for `efficiency` stamina
    // and covering S stamina costs S/efficiency adrenaline. Uses vanilla's adrenaline pool, so it is
    // inert unless the player has a max-adrenaline source. Shard values are authored as whole-number
    // percents, hence the 0.01f.
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

                // Capped at 1:1 -- 100% is a perfect conversion, so stacked sources cannot make a point of
                // adrenaline worth more than a point of stamina.
                var efficiency = Mathf.Min(
                    __instance.GetTotalActiveMagicEffectValue(MagicEffectType.UseAdrenalineAsStamina, 0.01f), 1f);
                if (efficiency <= 0f)
                {
                    return;
                }

                var covered = Mathf.Min(v - stamina, __instance.GetAdrenaline() * efficiency);
                if (covered > 0f)
                {
                    __instance.AddAdrenaline(-(covered / efficiency));
                    v -= covered; // that much of the cost is paid from adrenaline instead of stamina
                }
            }
        }
    }
}
