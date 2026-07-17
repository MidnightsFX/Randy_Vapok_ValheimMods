using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Moder boss shard: when the local player is struck the cold answers back — a frost nova detonates
    // around the player, dealing frost damage (and its usual chilling slow via AddFrostDamage) to every
    // nearby enemy. Hooked on the player's RPC_Damage (damage taken, owner-side = local). The boss-tier
    // shard value (15..25) scales the nova's frost damage.
    //
    // The effect is purely cooldown-based (no proc chance): every hit that lands while the cooldown is off
    // triggers a nova, then starts a cooldown shown as the "Moder's Chill" HUD status effect. While that
    // status effect is present the shard stays inert. The cooldown scales with the shard's rarity (140s at
    // Epic, +20s per rarity above it). Damage-over-time (poison/burning) ticks through Character.ApplyDamage
    // rather than RPC_Damage, so it can't trigger this.
    public static class ModerIcyRetribution {
        private const float NovaRadius = 8f;
        private const float FrostPerTier = 8f;   // frost damage per point of value (15..25 -> 120..200)
        private const float BaseCooldown = 140f;      // cooldown at Epic (the shard's rarity floor)
        private const float CooldownPerRarity = 20f;  // added per rarity above Epic

        // Visual: our own trimmed copy of the fenring's ice nova (see GetOrCreateNovaTemplate). We clone the
        // vanilla prefab once and shorten it rather than instantiating the shared prefab, so the fenring's
        // full-length nova is left untouched.
        private const string NovaFx = "fx_fenring_icenova";
        private const float SfxDelayReduction = 1.2f;   // trim from each SFX's trigger delay
        private static GameObject _novaTemplate;
        private static bool _novaMissingLogged;

        // Cooldown HUD indicator (Moder trophy icon with a radial recharge sweep). Built lazily on the first
        // proc -- see GetOrCreateCooldownIndicator -- so ObjectDB is loaded when the trophy is queried. Its
        // presence on the player is also the cooldown gate (checked via CooldownHash below).
        private const string CooldownName = "EL_IcyRetributionCooldown";
        private static readonly int CooldownHash = CooldownName.GetStableHashCode();
        private static StatusEffect _cooldownIndicator;
        private static bool _cooldownMissingLogged;

        // Postfix handler invoked by CharacterRpcDamageDispatch (on-damage-taken reaction).
        public static void OnDamageTaken(Character __instance, HitData hit) {
            var player = Player.m_localPlayer;
            if (__instance != player || hit == null) {
                return;
            }

            // The visible cooldown status effect is the gate: while it's present the shard stays inert.
            if (player.GetSEMan().HaveStatusEffect(CooldownHash)) {
                return;
            }

            var value = player.GetTotalActiveMagicEffectValue(MagicEffectType.IcyRetribution, 1f);
            if (value <= 0f) {
                return;
            }

            SpawnNova(player.transform.position);
            DamageInRadius.DamageEnemiesInRadius(player, player.GetCenterPoint(), NovaRadius,
                new HitData.DamageTypes { m_frost = value * FrostPerTier });
            ShowCooldown(player, GetCooldown(player));
        }

        // Cooldown length scales with the highest rarity among the equipped IcyRetribution shards: 140s at
        // Epic, +20s for each rarity above it (Legendary 160s, Mythic 180s).
        private static float GetCooldown(Player player) {
            var stepsAboveEpic = Mathf.Max(0, (int)GetEffectRarity(player) - (int)ItemRarity.Epic);
            return BaseCooldown + stepsAboveEpic * CooldownPerRarity;
        }

        // Highest source rarity among the socketed IcyRetribution effects on the player's equipped magic
        // items. Defaults to Epic (the shard's rarity floor) if none is found.
        private static ItemRarity GetEffectRarity(Player player) {
            var rarity = ItemRarity.Epic;
            var found = false;
            foreach (var item in player.GetMagicEquipment()) {
                if (!item.IsMagic(out var magicItem)) {
                    continue;
                }
                foreach (var socket in magicItem.Sockets) {
                    if (socket?.Effect == null || socket.Effect.EffectType != MagicEffectType.IcyRetribution) {
                        continue;
                    }
                    if (!found || socket.SourceRarity > rarity) {
                        rarity = socket.SourceRarity;
                        found = true;
                    }
                }
            }
            return rarity;
        }

        // Spawns a fresh copy of the trimmed nova at the player's feet. The template is built inactive, so the
        // instance starts inactive too and only begins playing once we activate it.
        private static void SpawnNova(Vector3 position) {
            var template = GetOrCreateNovaTemplate();
            if (template == null) {
                return;
            }

            var instance = Object.Instantiate(template, position, Quaternion.identity);
            instance.SetActive(true);
        }

        // Lazily builds our shortened, standalone copy of the fenring ice nova. Runs on a proc, so ZNetScene is
        // loaded and the source prefab is available. We clone the vanilla prefab while it is deactivated (so the
        // clone's components never Awake), trim it, and keep it inactive across scene loads as a reusable
        // template; a null source is logged once and leaves _novaTemplate null.
        private static GameObject GetOrCreateNovaTemplate() {
            if (_novaTemplate != null) {
                return _novaTemplate;
            }

            var source = ZNetScene.instance?.GetPrefab(NovaFx);
            if (source == null) {
                if (!_novaMissingLogged) {
                    EpicLoot.LogWarning($"ModerIcyRetribution: could not find '{NovaFx}' prefab; frost nova visual will not display.");
                    _novaMissingLogged = true;
                }
                return null;
            }

            // Deactivate the source across the clone so no component (particle systems, ZSFX) wakes up on the
            // template; restore the source afterwards so the shared prefab is left exactly as it was.
            var wasActive = source.activeSelf;
            source.SetActive(false);
            var template = Object.Instantiate(source);
            source.SetActive(wasActive);

            template.name = "EL_ModerIcyRetributionNova";
            Object.DontDestroyOnLoad(template);

            TrimParticleSystems(template);
            TrimSfx(template);

            _novaTemplate = template;
            return _novaTemplate;
        }

        // Every particle system on the nova runs its emission bursts three cycles; collapse each to a single
        // cycle and clear the start delay so our copy fires once, immediately.
        private static void TrimParticleSystems(GameObject root) {
            foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true)) {
                var main = ps.main;
                main.startDelay = 0f;

                var emission = ps.emission;
                var burstCount = emission.burstCount;
                if (burstCount <= 0) {
                    continue;
                }

                var bursts = new ParticleSystem.Burst[burstCount];
                emission.GetBursts(bursts);
                for (var i = 0; i < bursts.Length; i++) {
                    bursts[i].cycleCount = 1;
                }
                emission.SetBursts(bursts);
            }
        }

        // The nova carries three ZSFX sources, each staggered to line up with the original three-cycle visual.
        // Pull SfxDelayReduction seconds off every source's trigger delay so the audio tracks the shortened FX
        // (clamped at 0 so nothing ends up with a negative delay).
        private static void TrimSfx(GameObject root) {
            bool found = false;
            foreach (var sfx in root.GetComponentsInChildren<ZSFX>(true)) {
                if (found) {
                    GameObject.Destroy(sfx.gameObject);
                    continue;
                }
                sfx.m_minDelay = Mathf.Max(0f, sfx.m_minDelay - SfxDelayReduction);
                sfx.m_maxDelay = Mathf.Max(0f, sfx.m_maxDelay - SfxDelayReduction);
                found = true;

            }
        }

        // Adds the recharge indicator to the player with the rarity-scaled cooldown as its lifetime. We set
        // m_ttl on the shared prototype before adding; AddStatusEffect clones it (MemberwiseClone), so the
        // added instance carries this cooldown. Activation is gated on the effect's absence, so it's never
        // already present here.
        private static void ShowCooldown(Player player, float cooldown) {
            var indicator = GetOrCreateCooldownIndicator();
            if (indicator != null) {
                indicator.m_ttl = cooldown;
                player.GetSEMan().AddStatusEffect(indicator, true);
            }
        }

        // Lazily builds the cooldown indicator prototype. Runs on a proc, so ObjectDB is loaded and the Moder
        // (dragon) trophy icon is available. A null icon would render as an invisible HUD entry, so if the
        // trophy lookup fails we log once and leave _cooldownIndicator null.
        private static StatusEffect GetOrCreateCooldownIndicator() {
            if (_cooldownIndicator != null) {
                return _cooldownIndicator;
            }

            var icon = ObjectDB.instance?.GetItemPrefab("TrophyDragon")?
                .GetComponent<ItemDrop>()?.m_itemData.GetIcon();
            if (icon == null) {
                if (!_cooldownMissingLogged) {
                    EpicLoot.LogWarning("ModerIcyRetribution: could not find 'TrophyDragon' icon; cooldown indicator will not display.");
                    _cooldownMissingLogged = true;
                }
                return null;
            }

            var se = ScriptableObject.CreateInstance<StatusEffect>();
            se.name = CooldownName;
            se.m_name = "$mod_epicloot_se_icyretribution";
            se.m_icon = icon;
            se.m_ttl = BaseCooldown;   // overwritten per-proc by ShowCooldown with the rarity-scaled cooldown
            se.m_cooldownIcon = true;
            _cooldownIndicator = se;
            return _cooldownIndicator;
        }
    }
}
