using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // The Elder boss shard: when the local player is struck, the forest answers — ensnaring roots erupt and
    // slow every hostile creature around the player. Implemented as a cooldown-gated proc on the player's
    // RPC_Damage (damage taken, owner-side = local) that applies a strong slow to nearby enemies. It reuses
    // the mod's existing Slow component/RPC (see Slow.cs) rather than a bespoke root, so the ensnare is
    // multiplayer-safe and self-limiting (Slow refreshes for ~2s). The boss-tier shard value widens the reach.
    public static class ForestsAid
    {
        private const float Cooldown = 8f;      // seconds between eruptions
        private const float BaseRadius = 6f;    // ensnare radius before the shard value widens it
        private const float RadiusPerTier = 0.15f;
        private const float SlowMultiplier = 0.25f; // speed retained while ensnared (0.25 = 75% slow)

        private static float _nextTime;

        [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
        private static class RPC_Damage_Patch
        {
            [UsedImplicitly]
            private static void Postfix(Character __instance, HitData hit)
            {
                if (__instance != Player.m_localPlayer || hit == null || Time.time < _nextTime)
                {
                    return;
                }

                if (!Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.ForestsAid, out var value) || value <= 0f)
                {
                    return;
                }

                _nextTime = Time.time + Cooldown;
                Ensnare(Player.m_localPlayer, BaseRadius + value * RadiusPerTier);
            }
        }

        private static void Ensnare(Player player, float radius)
        {
            var center = player.transform.position;
            var radiusSqr = radius * radius;

            foreach (var character in Character.GetAllCharacters())
            {
                if (character == null || character.IsPlayer() || character.IsTamed() || character.IsDead()
                    || character.IsBoss())
                {
                    continue;
                }

                if ((character.transform.position - center).sqrMagnitude > radiusSqr)
                {
                    continue;
                }

                if (character.m_nview != null && character.m_nview.IsValid())
                {
                    character.m_nview.InvokeRPC(ZRoutedRpc.Everybody, Slow.RPCKey, SlowMultiplier);
                }
            }
        }
    }
}
