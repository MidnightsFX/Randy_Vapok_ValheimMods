using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    public static class BonemassPoison {
        private const float Cooldown = 15f;
        private const float CorpseRadius = 2f;
        private const float PoisonPerTier = 20f; // 20 poison damage per point of shard value (5..25 -> 100..500)
        private const string ExplosionFx = "vfx_BombBlob_explode_poison";

        private static bool _fxMissingLogged;

        // Cooldown HUD indicator (Bonemass trophy icon with a radial recharge sweep). Built lazily on the first
        // proc -- see GetOrCreateCooldownIndicator -- so ObjectDB is loaded when the trophy is queried. Its
        // presence on the player is also the cooldown gate (checked via CooldownHash below).
        private const string CooldownName = "EL_BonemassPoisonCooldown";
        private static readonly int CooldownHash = CooldownName.GetStableHashCode();
        private static StatusEffect _cooldownIndicator;
        private static bool _cooldownMissingLogged;

        // Detonates a poison cloud on enemies the local player kills. Mirrors vanilla's own last-hit attribution
        // (Character.OnDeath, m_lastHit.GetAttacker() == local player).
        [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
        private static class OnDeath_Patch {
            [UsedImplicitly]
            private static void Postfix(Character __instance) {
                var player = Player.m_localPlayer;
                if (player == null || __instance == player
                    || __instance.m_lastHit?.GetAttacker() != player) {
                    return;
                }

                // The visible cooldown status effect is the gate: while it's present the shard stays inert.
                if (player.GetSEMan().HaveStatusEffect(CooldownHash)) {
                    return;
                }

                if (!player.HasActiveMagicEffect(MagicEffectType.CorpseRot, out var value) || value <= 0f) {
                    return;
                }

                var center = __instance.GetCenterPoint();
                SpawnExplosionFx(__instance.transform.position);
                DamageInRadius.DamageEnemiesInRadius(player, center, CorpseRadius,
                    new HitData.DamageTypes { m_poison = value * PoisonPerTier });
                ShowCooldown(player);
            }
        }

        private static void SpawnExplosionFx(Vector3 position) {
            var prefab = ZNetScene.instance?.GetPrefab(ExplosionFx);
            if (prefab == null) {
                if (!_fxMissingLogged) {
                    EpicLoot.LogWarning($"BonemassPoison: could not find '{ExplosionFx}' prefab; poison burst visual will not display.");
                    _fxMissingLogged = true;
                }
                return;
            }

            Object.Instantiate(prefab, position, Quaternion.identity);
        }

        // Adds the recharge indicator to the player; its lifetime (m_ttl = Cooldown) is the cooldown.
        // Activation is gated on the effect's absence, so it's never already present here.
        private static void ShowCooldown(Player player) {
            var indicator = GetOrCreateCooldownIndicator();
            if (indicator != null) {
                player.GetSEMan().AddStatusEffect(indicator, true);
            }
        }

        // Lazily builds the cooldown indicator prototype. Runs on a proc, so ObjectDB is loaded and the Bonemass
        // trophy icon is available. A null icon would render as an invisible HUD entry, so if the trophy lookup
        // fails we log once and leave _cooldownIndicator null.
        private static StatusEffect GetOrCreateCooldownIndicator() {
            if (_cooldownIndicator != null) {
                return _cooldownIndicator;
            }

            var icon = ObjectDB.instance?.GetItemPrefab("TrophyBonemass")?
                .GetComponent<ItemDrop>()?.m_itemData.GetIcon();
            if (icon == null) {
                if (!_cooldownMissingLogged) {
                    EpicLoot.LogWarning("BonemassPoison: could not find 'TrophyBonemass' icon; cooldown indicator will not display.");
                    _cooldownMissingLogged = true;
                }
                return null;
            }

            var se = ScriptableObject.CreateInstance<StatusEffect>();
            se.name = CooldownName;
            se.m_name = "$mod_epicloot_se_corpserot";
            se.m_icon = icon;
            se.m_ttl = Cooldown;
            se.m_cooldownIcon = true;
            _cooldownIndicator = se;
            return _cooldownIndicator;
        }
    }
}
