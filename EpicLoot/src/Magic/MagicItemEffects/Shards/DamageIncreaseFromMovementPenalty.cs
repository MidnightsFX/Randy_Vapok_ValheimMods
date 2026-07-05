using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Green (Movement) weapon shard: +% weapon damage that scales with the movement-speed penalty of the
    // player's equipped gear -- the heavier and slower the loadout, the harder it hits. Applied per-weapon
    // (mirrors IncreaseDamageDuringDaytime) so an off-hand shard does not leak onto the main hand when
    // dual-wielding. Shard values are authored as whole-number percents, hence the 0.01f.
    public static class DamageIncreaseFromMovementPenalty
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
                    player, __instance, MagicEffectType.DamageIncreaseFromMovementPenalty, 0.01f);
                var bonus = pct * PenaltyScaling.MovementPenaltyFactor(player);
                if (bonus != 0f)
                {
                    __result.Modify(1f + bonus);
                }
            }
        }
    }
}
