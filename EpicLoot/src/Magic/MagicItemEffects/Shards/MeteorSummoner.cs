using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Yagluth boss shard: when the local player strikes an enemy that survives the blow, a meteor is called
    // down on it -- the vanilla 'projectile_meteor' is launched from 20 m up and a random 5..15 m to the side
    // of the target, then flown straight into it at a fixed speed. Hooked on Character.Damage (attacker ==
    // local player), which always runs on the attacker's machine. The meteor carries a rarity-scaled fire
    // hit, and after each summon a Yagluth-trophy cooldown indicator (radial recharge sweep, like
    // ModerIcyRetribution) is shown for the length of the cooldown.
    //
    // The shard value encodes rarity (Magic 5 -> Mythic 25 in steps of 5); damage and cooldown both scale
    // linearly with it, anchored so a Legendary shard (value 20) lands 250 damage on a 120 s (2 min)
    // cooldown. Yagluth shards only roll Legendary today, so that is the effective tuning.
    public static class MeteorSummoner
    {
        private const string MeteorPrefab = "projectile_meteor";
        private const float SpawnHeight = 20f;       // metres above the target the meteor launches from
        private const float MinDistance = 5f;        // min horizontal offset of the launch point from the target
        private const float MaxDistance = 15f;       // max horizontal offset of the launch point from the target
        private const float ProjectileSpeed = 20f;   // metres/second the meteor travels toward the target
        private const float ExplosionRadius = 4f;     // min AOE radius on impact so a moving target is still caught

        private const float DamagePerValue = 12.5f;  // 20 * 12.5 = 250 fire at Legendary
        private const float CooldownPerValue = 6f;   // 20 * 6    = 120 s at Legendary

        private static float _nextTime;
        private static bool _meteorMissingLogged;

        // Cooldown HUD indicator (Yagluth trophy icon with a radial recharge sweep). Built lazily on the first
        // proc -- see GetOrCreateCooldownIndicator -- so ObjectDB is loaded when the trophy is queried. Its
        // duration is set per-proc so it always mirrors the rarity-scaled cooldown.
        private const string CooldownName = "EL_MeteorSummonerCooldown";
        private static StatusEffect _cooldownIndicator;
        private static bool _cooldownMissingLogged;

        // Postfix handler invoked by CharacterDamageDispatch (on-hit reaction).
        public static void OnDamageDealt(Character __instance, HitData hit, Character attacker)
        {
            var player = Player.m_localPlayer;
            if (hit == null || player == null || __instance == player
                || attacker != player || Time.time < _nextTime || __instance.IsDead())
            {
                return;
            }

            var value = player.GetTotalActiveMagicEffectValue(MagicEffectType.MeteorSummoner, 1f);
            if (value <= 0f)
            {
                return;
            }

            var cooldown = value * CooldownPerValue;
            _nextTime = Time.time + cooldown;

            SummonMeteor(player, __instance, value * DamagePerValue);
            ShowCooldown(player, cooldown);
        }

        // Launches the meteor from a random point on a ring MinDistance..MaxDistance around the target, lifted
        // SpawnHeight up, and flies it straight into the target's centre at ProjectileSpeed carrying `fireDamage`.
        private static void SummonMeteor(Player player, Character target, float fireDamage)
        {
            var prefab = ZNetScene.instance?.GetPrefab(MeteorPrefab);
            if (prefab == null)
            {
                if (!_meteorMissingLogged)
                {
                    EpicLoot.LogWarning($"MeteorSummoner: could not find '{MeteorPrefab}' prefab; meteor will not spawn.");
                    _meteorMissingLogged = true;
                }
                return;
            }

            var targetPos = target.GetCenterPoint();

            var angle = Random.Range(0f, Mathf.PI * 2f);
            var horizontalDistance = Random.Range(MinDistance, MaxDistance);
            var spawnPos = targetPos + new Vector3(
                Mathf.Cos(angle) * horizontalDistance,
                SpawnHeight,
                Mathf.Sin(angle) * horizontalDistance);

            var velocity = (targetPos - spawnPos).normalized * ProjectileSpeed;

            var meteor = Object.Instantiate(prefab, spawnPos, Quaternion.LookRotation(velocity));
            var projectile = meteor.GetComponent<Projectile>();
            if (projectile == null)
            {
                return;
            }

            // Straight-line shot: zero gravity so the fixed speed carries the meteor into the target instead
            // of arcing short, and disable the owner raytest (meant for player weapons fired from the chest --
            // it would false-hit from the player's position the instant this spawns 20 m away).
            projectile.m_gravity = 0f;
            projectile.m_doOwnerRaytest = false;

            var hitData = new HitData { m_damage = { m_fire = fireDamage } };
            hitData.SetAttacker(player);
            projectile.Setup(player, velocity, -1f, hitData, null, null);

            // projectile_meteor is Yagluth's meteor: it carries m_onlySpawnedProjectilesDealDamage with a
            // ground-only AOE spawn, so Setup ZEROES m_damage above and defers all damage to a sub-object that
            // only spawns on a terrain hit. Striking the character directly therefore lands nothing. Re-apply
            // our fire hit after Setup and make the meteor itself deal it, so hitting the target does damage
            // regardless of the prefab's spawn-on-hit / ground-only rules. A guaranteed AOE radius ensures a
            // moving target still gets caught when the meteor lands beside it instead of dead-on.
            projectile.m_damage = hitData.m_damage;
            projectile.m_onlySpawnedProjectilesDealDamage = false;
            projectile.m_aoe = Mathf.Max(projectile.m_aoe, ExplosionRadius);
        }

        // Adds (or restarts) the recharge indicator on the player for the current cooldown. Setting m_ttl on
        // the prototype before AddStatusEffect makes the clone SEMan takes carry the rarity-scaled duration;
        // the previous cooldown has always elapsed by the time we can proc again, so a fresh clone is added.
        private static void ShowCooldown(Player player, float cooldown)
        {
            var indicator = GetOrCreateCooldownIndicator();
            if (indicator == null)
            {
                return;
            }

            indicator.m_ttl = cooldown;
            player.GetSEMan().AddStatusEffect(indicator, true);
        }

        // Lazily builds the cooldown indicator prototype. Runs on a proc, so ObjectDB is loaded and the
        // Yagluth (goblin king) trophy icon is available. A null icon would render as an invisible HUD entry,
        // so if the trophy lookup fails we log once and leave _cooldownIndicator null.
        private static StatusEffect GetOrCreateCooldownIndicator()
        {
            if (_cooldownIndicator != null)
            {
                return _cooldownIndicator;
            }

            var icon = ObjectDB.instance?.GetItemPrefab("TrophyGoblinKing")?
                .GetComponent<ItemDrop>()?.m_itemData.GetIcon();
            if (icon == null)
            {
                if (!_cooldownMissingLogged)
                {
                    EpicLoot.LogWarning("MeteorSummoner: could not find 'TrophyGoblinKing' icon; cooldown indicator will not display.");
                    _cooldownMissingLogged = true;
                }
                return null;
            }

            var se = ScriptableObject.CreateInstance<StatusEffect>();
            se.name = CooldownName;
            se.m_name = "$mod_epicloot_se_meteorsummoner";
            se.m_icon = icon;
            se.m_cooldownIcon = true;
            _cooldownIndicator = se;
            return _cooldownIndicator;
        }
    }
}
