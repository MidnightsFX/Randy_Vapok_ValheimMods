using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Black trinket shard: when the local player's adrenaline fills up ("activates"), summon tamed bats to
    // fight alongside them for a short time. Detected by watching AddAdrenaline reach max -- in vanilla that
    // either caps the pool at max (gear without a full-adrenaline SE) or "pops" and resets it to 0 (gear with
    // one); both are caught. Uses vanilla's adrenaline pool, so it is inert unless the player has a
    // max-adrenaline source (matches the other adrenaline shards). The summon is rate-limited by a cooldown.
    //
    // The bats are instances of our own registered clone of the vanilla 'Bat' (see RegisterTamedBatPrefab):
    // the clone bakes faction = Players and m_tamed = true into the PREFAB, so a summoned bat stays friendly
    // even after a zone unload/reload loses its runtime follow target. A TimedDestruction baked on the clone
    // drives reload-safe cleanup (only the owner destroys it, and the countdown restarts on load so bats never
    // linger forever). Count scales with the shard's rarity value: number summoned per proc rises by one each
    // rarity (Magic 1 -> Mythic 5), and at most 2x that number may be alive at once.
    public static class SummonBatWhenActivatingAdrenaline
    {
        private const string BatSourcePrefab = "Bat";              // vanilla creature we clone
        private const string TamedBatPrefab = "EL_TamedBat";       // our registered player-faction/tamed clone
        private const float BatLifetime = 30f;                     // seconds a summoned bat lives (TimedDestruction)
        private const float SummonCooldown = 10f;
        private const float SpawnRadius = 2f;                      // ring radius bats spawn on around the player

        private static float _lastSummonTime = -999f;

        // Disabled, DontDestroyOnLoad container + cached template for the clone (same approach as
        // StrikeCausesLightning): cloning under a disabled parent keeps the template from Awaking while
        // preserving activeSelf == true, which remote ZNetScene.CreateObject needs to spawn a live instance.
        private static GameObject _batContainer;
        private static GameObject _batTemplate;
        private static bool _sourceMissingLogged;
        private static bool _prefabMissingLogged;

        // Summoned bats this session, tracked so we can enforce the max-concurrent cap. Reloaded bats are not
        // in this list, but TimedDestruction reaps them, so concurrency stays bounded regardless.
        private static readonly List<GameObject> _activeBats = new List<GameObject>();

        // Captured across the AddAdrenaline call: the pool before the change, and whether this call was a gain
        // (v > 0). Only a gain can fill the pool, so guarding on it rules out the per-frame degen decrements.
        private struct AdrenalineChange
        {
            public float Before;
            public bool WasGain;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.AddAdrenaline))]
        private static class AddAdrenaline_Patch
        {
            [UsedImplicitly]
            private static void Prefix(Player __instance, float v, out AdrenalineChange __state)
            {
                __state = new AdrenalineChange { Before = __instance.GetAdrenaline(), WasGain = v > 0f };
            }

            [UsedImplicitly]
            private static void Postfix(Player __instance, AdrenalineChange __state)
            {
                if (__instance != Player.m_localPlayer)
                {
                    return;
                }

                var max = __instance.GetMaxAdrenaline();
                if (max <= 0f)
                {
                    return; // no adrenaline source -> inert (matches the other adrenaline shards)
                }

                var after = __instance.GetAdrenaline();

                // Adrenaline "activated" this call if a gain pushed it to full. Two vanilla outcomes:
                //   - gear WITHOUT a full-adrenaline SE caps the pool at max        -> after >= max
                //   - gear WITH a full-adrenaline SE pops and resets the pool to 0  -> a substantial pool
                //     dropped to ~0. Guarding on WasGain rules out the per-frame degen decrements, which are
                //     the only other way the pool falls.
                var cappedToMax = __state.Before < max && after >= max;
                var poppedToZero = __state.WasGain && __state.Before > 1f && after <= 0.01f;
                if (!cappedToMax && !poppedToZero)
                {
                    return;
                }

                if (Time.time - _lastSummonTime < SummonCooldown)
                {
                    return;
                }

                var value = __instance.GetTotalActiveMagicEffectValue(MagicEffectType.SummonBatWhenActivatingAdrenaline);
                if (value <= 0f)
                {
                    return;
                }

                _lastSummonTime = Time.time;
                SummonBats(__instance, value);
            }
        }

        // Registers our tamed-bat clone into the CURRENT ZNetScene. Wired to PrefabManager.OnPrefabsRegistered,
        // which fires as a ZNetScene.Awake postfix on every client each world load -- so every client has the
        // prefab registered before it can receive a synced bat ZDO. Injects straight into ZNetScene
        // (m_prefabs + m_namedPrefabs) rather than relying on Jotunn's registration pass, whose ordering left
        // clones out of ZNetScene (see StrikeCausesLightning). Idempotent: the clone is built once (cached
        // across worlds) and re-injected into each fresh ZNetScene.
        public static void RegisterTamedBatPrefab()
        {
            var zns = ZNetScene.instance;
            if (zns == null || zns.GetPrefab(TamedBatPrefab) != null)
            {
                return;
            }

            var template = GetOrBuildTemplate(zns);
            if (template == null)
            {
                return;
            }

            if (!zns.m_prefabs.Contains(template))
            {
                zns.m_prefabs.Add(template);
            }
            zns.m_namedPrefabs[TamedBatPrefab.GetStableHashCode()] = template;
            EpicLoot.Log($"SummonBatWhenActivatingAdrenaline: registered '{TamedBatPrefab}' with ZNetScene.");
        }

        // Builds our player-faction, tamed, self-despawning clone of the vanilla Bat once and caches it. The
        // clone is instantiated under a DISABLED container so none of its components Awake on the template, yet
        // it keeps activeSelf == true (remote clients spawn it via ZNetScene.CreateObject, which never calls
        // SetActive). Faction/tamed are baked into the prefab so instances stay friendly across reload; a
        // TimedDestruction drives reload-safe cleanup.
        private static GameObject GetOrBuildTemplate(ZNetScene zns)
        {
            if (_batTemplate != null)
            {
                return _batTemplate;
            }

            var source = zns.GetPrefab(BatSourcePrefab);
            if (source == null)
            {
                if (!_sourceMissingLogged)
                {
                    EpicLoot.LogWarning($"SummonBatWhenActivatingAdrenaline: could not find '{BatSourcePrefab}' prefab; bats will not summon.");
                    _sourceMissingLogged = true;
                }
                return null;
            }

            if (_batContainer == null)
            {
                _batContainer = new GameObject("EL_TamedBatContainer");
                _batContainer.SetActive(false);
                Object.DontDestroyOnLoad(_batContainer);
            }

            var template = Object.Instantiate(source, _batContainer.transform);
            template.name = TamedBatPrefab;

            // Bake friendliness into the prefab: GetFaction() reads this field (never the ZDO), and m_tamed
            // defaults from it when the ZDO has no s_tamed -- so a reloaded instance is player-aligned even
            // after its runtime follow target is lost.
            var character = template.GetComponent<Character>();
            if (character != null)
            {
                character.m_faction = Character.Faction.Players;
                character.m_tamed = true;
            }

            // Reload-safe lifetime: only the owner destroys (removing the ZDO); the countdown restarts on each
            // load, so a persisted bat is reaped within BatLifetime instead of lingering forever.
            var timed = template.GetComponent<TimedDestruction>() ?? template.AddComponent<TimedDestruction>();
            timed.m_timeout = BatLifetime;
            timed.m_triggerOnAwake = true;

            _batTemplate = template;
            return _batTemplate;
        }

        // Spawns rarity-scaled bats around the player and enforces the max-concurrent cap. Number summoned rises
        // by one each rarity (effect value 2..6 for Magic..Mythic -> 1..5), and at most 2x that may be alive.
        private static void SummonBats(Player player, float value)
        {
            if (ZNetScene.instance == null)
            {
                return;
            }

            var prefab = ZNetScene.instance.GetPrefab(TamedBatPrefab);
            if (prefab == null)
            {
                if (!_prefabMissingLogged)
                {
                    EpicLoot.LogWarning($"SummonBatWhenActivatingAdrenaline: '{TamedBatPrefab}' prefab not registered; bats will not summon.");
                    _prefabMissingLogged = true;
                }
                return;
            }

            var summonCount = Mathf.Max(1, Mathf.RoundToInt(value) - 1);
            var maxBats = summonCount * 2;

            // Drop bats that have already despawned/been destroyed before counting toward the cap.
            _activeBats.RemoveAll(bat => bat == null);

            var basePos = player.transform.position + Vector3.up;
            for (var i = 0; i < summonCount; i++)
            {
                var angle = Mathf.PI * 2f * i / summonCount;
                var spawnPos = basePos + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * SpawnRadius;

                var bat = Object.Instantiate(prefab, spawnPos, Quaternion.identity);
                // Faction/tamed/lifetime are baked into the prefab; only the runtime follow target is per-instance.
                bat.GetComponent<MonsterAI>()?.SetFollowTarget(player.gameObject);
                _activeBats.Add(bat);
            }

            // Enforce the cap by despawning the oldest bats first.
            while (_activeBats.Count > maxBats)
            {
                var oldest = _activeBats[0];
                _activeBats.RemoveAt(0);
                if (oldest != null)
                {
                    ZNetScene.instance.Destroy(oldest);
                }
            }
        }
    }
}
