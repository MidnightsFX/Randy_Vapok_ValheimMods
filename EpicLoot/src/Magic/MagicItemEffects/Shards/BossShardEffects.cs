using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Shared helper for the boss "signature" shard effects. Each boss shard (one per Valheim boss) lives in
    // its own file next to this one — ShockingCharge (Eikthyr), ForestsAid (The Elder), PoisonCoating
    // (Bonemass), IcyRetribution (Moder), MeteorSummoner (Yagluth), EitrSiphon (The Queen) and LastFire
    // (Fader). The novas/eruptions among them share one primitive: a self-contained radial damage burst that
    // needs no bespoke attack prefab, so an effect keeps working regardless of which vanilla FX prefabs exist.
    public static class BossShardEffects
    {
        // Applies `damage` to every hostile character within `radius` of `center`, crediting `attacker` for
        // the hit. Iterates the live character list (same approach as ForestsAid) and skips the attacker's own
        // allies (players and tames) and the dead; bosses are fair game. Poison/fire/frost portions become the
        // usual stacking damage-over-time, and each victim plays its own elemental hit effect for feedback.
        public static void DamageEnemiesInRadius(Player attacker, Vector3 center, float radius,
            HitData.DamageTypes damage)
        {
            if (attacker == null)
            {
                return;
            }

            var radiusSqr = radius * radius;
            foreach (var character in Character.GetAllCharacters())
            {
                if (character == null || character.IsPlayer() || character.IsTamed() || character.IsDead())
                {
                    continue;
                }

                var characterCenter = character.GetCenterPoint();
                if ((characterCenter - center).sqrMagnitude > radiusSqr)
                {
                    continue;
                }

                var hit = new HitData
                {
                    m_point = characterCenter,
                    m_dir = (character.transform.position - center).normalized,
                    m_damage = damage,
                    m_ranged = true,
                };
                hit.SetAttacker(attacker);
                character.Damage(hit);
            }
        }
    }
}
