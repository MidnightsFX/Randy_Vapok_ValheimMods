using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Black weapon shard: +% weapon damage while it is night (EnvMan.IsNight()). Applied per-weapon
    // (mirrors ModifyDamage's ForWeapon effects) so an off-hand shard does not leak onto the main
    // hand when dual-wielding. Shard values are authored as whole-number percents, hence the 0.01f.
    public static class IncreaseDamageDuringNighttime
    {
        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDamage), typeof(int), typeof(float))]
        private static class GetDamage_Patch
        {
            [UsedImplicitly]
            private static void Postfix(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result)
            {
                var player = Player.m_localPlayer;
                if (player == null || !EnvMan.IsNight() || !player.IsItemEquiped(__instance))
                {
                    return;
                }

                var bonus = MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
                    player, __instance, MagicEffectType.IncreaseDamageDuringNighttime, 0.01f);
                if (bonus != 0f)
                {
                    __result.Modify(1f + bonus);
                }
            }
        }
    }
}
