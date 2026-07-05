using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Gather shard (weapon slots): +% harvest damage — scales the chop (trees) and pickaxe (rock/ore)
    // damage of the equipped tool so felling and mining go faster. Every other damage type is left
    // untouched, so this never affects creature combat; on a weapon with no chop/pickaxe it is inert.
    // Applied per-weapon like ModifyDamage's ForWeapon effects. Shard values are authored as
    // whole-number percents, hence the 0.01f.
    public static class IncreaseHarvestDamage
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

                var bonus = MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
                    player, __instance, MagicEffectType.IncreaseHarvestDamage, 0.01f);
                if (bonus != 0f)
                {
                    __result.m_chop *= 1f + bonus;
                    __result.m_pickaxe *= 1f + bonus;
                }
            }
        }
    }
}
