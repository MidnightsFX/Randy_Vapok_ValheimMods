using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Grey trinket shard: gain a flat pulse of adrenaline (the effect value) each time the local player
    // strikes a harvestable — trees and rock/ore. Mirrors the hook set of IncreaseTreeDrop /
    // IncreaseMiningDrop. Uses vanilla's adrenaline pool, so it is inert unless the player has a
    // max-adrenaline source. The effect value is used as a flat adrenaline amount (no percent scaling).
    public static class GainAdrenalineFromHarvesting
    {
        private static void OnHarvestHit(HitData hit)
        {
            if (hit == null || hit.GetAttacker() != Player.m_localPlayer)
            {
                return;
            }

            var value = Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.GainAdrenalineFromHarvesting);
            if (value > 0f)
            {
                Player.m_localPlayer.AddAdrenaline(value);
            }
        }

        [HarmonyPatch(typeof(TreeBase), nameof(TreeBase.Damage))]
        private static class TreeBase_Damage_Patch
        {
            [UsedImplicitly]
            private static void Prefix(HitData hit) => OnHarvestHit(hit);
        }

        [HarmonyPatch(typeof(TreeLog), nameof(TreeLog.Damage))]
        private static class TreeLog_Damage_Patch
        {
            [UsedImplicitly]
            private static void Prefix(HitData hit) => OnHarvestHit(hit);
        }

        [HarmonyPatch(typeof(MineRock), nameof(MineRock.Damage))]
        private static class MineRock_Damage_Patch
        {
            [UsedImplicitly]
            private static void Prefix(HitData hit) => OnHarvestHit(hit);
        }

        [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.Damage))]
        private static class MineRock5_Damage_Patch
        {
            [UsedImplicitly]
            private static void Prefix(HitData hit) => OnHarvestHit(hit);
        }
    }
}
