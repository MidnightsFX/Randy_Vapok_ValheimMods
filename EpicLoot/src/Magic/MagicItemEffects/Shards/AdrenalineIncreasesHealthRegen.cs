using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // LightGreen trinket shard: when the local player's adrenaline fills ("activates"), grant the "Adrenaline
    // Surge" buff (SE_AdrenalineSurge) -- a flat boost to health regen for a short window. The fill is detected
    // by SharedPlayerAddAdrenalinePatch, which also enforces the local-player and has-an-adrenaline-pool
    // guards, so this is inert unless the player has a max-adrenaline source (matches the other adrenaline
    // shards).
    //
    // The shard value is authored as a whole-number percent and drives BOTH numbers: it is the regen bonus
    // (hence the 0.01f), and multiplied by SecondsPerPercent it is the buff's duration -- so a Mythic (40)
    // gives +40% health regen for 20s. One rarity ramp, two scaling axes.
    //
    // The buff does not stack: re-filling while it is up restamps the bonus and duration (the shard set may
    // have changed) and refreshes the countdown on the single instance.
    public static class AdrenalineIncreasesHealthRegen
    {
        // Seconds of buff granted per 1 point of shard value. Overridable via the effect's Config block
        // ("SecondsPerPercent", see ShardEffectDefinitions).
        public const float DefaultSecondsPerPercent = 0.5f;

        private const string BuffName = "EL_AdrenalineSurge";
        private static readonly int BuffHash = BuffName.GetStableHashCode();
        private static SE_AdrenalineSurge _buffPrototype;
        private static bool _iconMissingLogged;

        // Tooltip: "Adrenaline Surge: +{0}% Health Regen for {1}s" -- {1} surfaces the derived duration. Pure,
        // as the provider contract requires (MagicItem.RegisterDisplayValues): it only reads the effect config.
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.AdrenalineIncreasesHealthRegen,
                value => new object[] { value, value * GetSecondsPerPercent() });
        }

        // Called by SharedPlayerAddAdrenalinePatch, which owns the Player.AddAdrenaline patch and the
        // fill/pop detection (including the local-player and no-adrenaline-source guards).
        public static void OnAdrenalineActivated(Player player)
        {
            var percent = player.GetTotalActiveMagicEffectValue(MagicEffectType.AdrenalineIncreasesHealthRegen);
            if (percent <= 0f)
            {
                return;
            }

            var bonus = percent * 0.01f;
            var duration = percent * GetSecondsPerPercent();

            var seMan = player.GetSEMan();

            // Re-proc while the buff is still up: restamp the bonus and duration, then refresh the countdown
            // rather than letting the old timer run out. Unlike the fixed-duration shard buffs, m_ttl has to be
            // restamped too -- it is derived from the value, which changes with the socketed shard set.
            if (seMan.GetStatusEffect(BuffHash) is SE_AdrenalineSurge existing)
            {
                existing.RegenBonus = bonus;
                existing.m_ttl = duration;
                existing.ResetTime();
                return;
            }

            var prototype = GetOrCreatePrototype();
            if (prototype == null)
            {
                return;
            }

            // Seed the prototype so the clone SEMan takes carries the current rarity's bonus and duration.
            prototype.RegenBonus = bonus;
            prototype.m_ttl = duration;
            seMan.AddStatusEffect(prototype);
        }

        // Seconds per point of value come from the effect's Config block ("SecondsPerPercent", see
        // ShardEffectDefinitions), defaulting to DefaultSecondsPerPercent when unset. Clamped above zero so a
        // misconfiguration can't produce a zero-length buff.
        private static float GetSecondsPerPercent()
        {
            var cfg = MagicItemEffectDefinitions.GetEffectConfig(MagicEffectType.AdrenalineIncreasesHealthRegen);
            if (cfg != null && cfg.TryGetValue("SecondsPerPercent", out var raw) && raw > 0f)
            {
                return raw;
            }
            return DefaultSecondsPerPercent;
        }

        // Lazily builds the buff prototype. Runs on an adrenaline fill, so the asset bundle is loaded. A null
        // icon would render as an invisible HUD entry (SEMan only surfaces effects with an icon), so if the
        // sprite lookup fails we log once and leave the prototype null.
        private static SE_AdrenalineSurge GetOrCreatePrototype()
        {
            if (_buffPrototype != null)
            {
                return _buffPrototype;
            }

            // The LightGreen shardstone's own icon -- same sprite the shard items use (see Shards.cs).
            var icon = EpicAssets.AssetBundle?.LoadAsset<Sprite>("Assets/EpicLoot/Sprites/Shardstones/LightGreen.png");
            if (icon == null)
            {
                if (!_iconMissingLogged)
                {
                    EpicLoot.LogWarning("AdrenalineIncreasesHealthRegen: could not load the LightGreen shardstone sprite; Adrenaline Surge will not display.");
                    _iconMissingLogged = true;
                }
                return null;
            }

            var se = ScriptableObject.CreateInstance<SE_AdrenalineSurge>();
            se.name = BuffName;
            se.m_name = "$mod_epicloot_se_adrenalinesurge";
            se.m_icon = icon;
            _buffPrototype = se;
            return _buffPrototype;
        }
    }
}
