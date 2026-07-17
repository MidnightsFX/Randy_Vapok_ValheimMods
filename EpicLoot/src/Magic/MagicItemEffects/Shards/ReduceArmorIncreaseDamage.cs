using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // DarkRed shoulder shard (glass cannon): trade armor for offense -- reduce your armor by value% but
    // increase weapon damage by the same value%. The armor cut is a GetArmor postfix (mirrors DayArmor,
    // applied per equipped piece so the net is -value% total armor); the damage gain is a per-weapon
    // GetDamage modifier run from ModifyDamage.ApplyShardWeaponModifiers. Shard values are whole-number
    // percents, hence the 0.01f.
    public static class ReduceArmorIncreaseDamage
    {
        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetArmor), typeof(int), typeof(float))]
        private static class GetArmor_Patch
        {
            [UsedImplicitly]
            private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
            {
                var player = PlayerExtensions.GetPlayerWithEquippedItem(__instance);
                if (player == null)
                {
                    return;
                }

                var value = player.GetTotalActiveMagicEffectValue(MagicEffectType.ReduceArmorIncreaseDamage, 0.01f);
                if (value > 0f)
                {
                    __result *= Mathf.Max(0f, 1f - value);
                }
            }
        }

        // GetDamage postfix handler invoked by ModifyDamage (per-weapon modifier).
        public static void ModifyWeaponDamage(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result)
        {
            var player = Player.m_localPlayer;
            if (player == null || !player.IsItemEquiped(__instance))
            {
                return;
            }

            var value = player.GetTotalActiveMagicEffectValue(MagicEffectType.ReduceArmorIncreaseDamage, 0.01f);
            if (value > 0f)
            {
                __result.Modify(1f + value);
            }
        }
    }
}
