using HarmonyLib;
using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Orange (Fire) legs shard: while the player runs, they leave a trail of burning ground behind them.
    // Each patch of the trail scorches nearby enemies with fire (which also sets them alight via the
    // vanilla SE_Burning path) for a few seconds before it burns out. The shard value is the fire damage
    // one patch deals per tick, read at the default (flat) scale.
    //
    // Damage is applied locally by the trail's owner through the normal Character.Damage path (like
    // FrostAOE), so it networks correctly. The visual is a lightweight point-light glow per patch so the
    // effect needs no bundled asset; a dedicated fire VFX prefab can be swapped in here later.
    public static class Stampede
    {
        // Tuning knobs (placeholders; balance later).
        private const float MinMoveSpeed = 1.5f;      // horizontal speed required to lay a trail
        private const float SpawnInterval = 0.35f;    // seconds between dropping trail patches
        private const float PatchRadius = 2.5f;       // burn radius of each patch
        private const float PatchLifetime = 3f;       // how long a patch lingers
        private const float PatchTickInterval = 0.5f; // seconds between burn ticks within a patch

        private static float _spawnTimer;

        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        private static class Update_Patch
        {
            [UsedImplicitly]
            private static void Postfix(Player __instance)
            {
                if (__instance != Player.m_localPlayer)
                {
                    return;
                }

                _spawnTimer -= Time.deltaTime;

                if (__instance.IsDead() ||
                    !__instance.HasActiveMagicEffect(MagicEffectType.Stampede, out var tickDamage) ||
                    tickDamage <= 0f)
                {
                    return;
                }

                // Only lay a trail while actually moving along the ground.
                var velocity = __instance.GetVelocity();
                velocity.y = 0f;
                if (velocity.magnitude < MinMoveSpeed || !__instance.IsOnGround())
                {
                    return;
                }

                if (_spawnTimer > 0f)
                {
                    return;
                }
                _spawnTimer = SpawnInterval;

                var go = new GameObject("EpicLoot_StampedeFire") { transform = { position = __instance.transform.position } };
                go.AddComponent<StampedeFirePatch>()
                    .Init(__instance, tickDamage, PatchRadius, PatchLifetime, PatchTickInterval);
            }
        }
    }

    // A single burning patch of the Stampede trail. It sits where it was dropped, burns enemies inside its
    // radius on an interval, and removes itself when its lifetime runs out.
    public class StampedeFirePatch : MonoBehaviour
    {
        private Player _owner;
        private float _tickDamage;
        private float _radius;
        private float _lifetime;
        private float _lifeLeft;
        private float _tickInterval;
        private float _tickTimer;
        private Light _glow;
        private readonly List<Character> _inRange = new List<Character>();

        public void Init(Player owner, float tickDamage, float radius, float lifetime, float tickInterval)
        {
            _owner = owner;
            _tickDamage = tickDamage;
            _radius = radius;
            _lifetime = lifetime;
            _lifeLeft = lifetime;
            _tickInterval = tickInterval;

            _glow = gameObject.AddComponent<Light>();
            _glow.type = LightType.Point;
            _glow.color = new Color(1f, 0.5f, 0.15f);
            _glow.range = radius * 2f;
            _glow.shadows = LightShadows.None;
        }

        [UsedImplicitly]
        private void Update()
        {
            var dt = Time.deltaTime;
            _lifeLeft -= dt;
            if (_owner == null || _lifeLeft <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            if (_glow != null)
            {
                // Fade and flicker the glow as the patch burns down.
                _glow.intensity = Mathf.Clamp01(_lifeLeft / _lifetime) * (1.4f + Random.Range(-0.15f, 0.15f));
            }

            _tickTimer -= dt;
            if (_tickTimer > 0f)
            {
                return;
            }
            _tickTimer = _tickInterval;

            _inRange.Clear();
            Character.GetCharactersInRange(transform.position, _radius, _inRange);
            foreach (var character in _inRange)
            {
                if (character == null || character.IsDead() || character.IsPlayer() || character.IsTamed())
                {
                    continue;
                }

                var hit = new HitData();
                hit.m_point = character.transform.position;
                hit.m_dir = (character.transform.position - transform.position).normalized;
                hit.m_damage.m_fire = _tickDamage;
                hit.m_hitType = HitData.HitType.Burning;
                hit.SetAttacker(_owner);
                character.Damage(hit);
            }
        }
    }
}
