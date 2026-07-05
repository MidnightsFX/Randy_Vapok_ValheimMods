using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Peach (Logistics) weapon shard: +% weapon damage that scales with how loaded the player's pack is
    // (empty pack = nothing, at the carry cap = the full shard value). Applied per-weapon (mirrors
    // ModifyDamage / IncreaseDamageDuringDaytime) so an off-hand shard does not leak onto the main hand
    // when dual-wielding. Shard values are authored as whole-number percents, hence the 0.01f.
    public static class DamageBonusFromPlayerWeight
    {
        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDamage), typeof(int), typeof(float))]
        private static class GetDamage_Patch
        {
            [UsedImplicitly]
            private static void Postfix(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result)
            {
                var player = Player.m_localPlayer;
                if (player == null || !player.IsItemEquiped(__instance))
                {
                    return;
                }

                var pct = MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
                    player, __instance, MagicEffectType.DamageBonusFromPlayerWeight, 0.01f);
                var bonus = pct * PenaltyScaling.WeightFactor(player);
                if (bonus != 0f)
                {
                    __result.Modify(1f + bonus);
                }
            }
        }
    }
}
