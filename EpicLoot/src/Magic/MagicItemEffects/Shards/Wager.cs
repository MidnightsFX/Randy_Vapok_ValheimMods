using System.Collections.Generic;
using EpicLoot.General;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Wager -- Golden (Fortune) head shard effect: stake coins on every swing. The shard value IS the stake
    // (20/40/60/80/100 coins), and buys DamagePerCoin (0.5) flat damage per coin staked, added to the hit
    // before armour. Fail to cover the stake and nothing happens -- no coins spent, no bonus.
    //
    // If the boosted hit kills the target, the whole stake is refunded: a won bet costs nothing, a lost one
    // costs the full stake. That makes it strongest as a finisher and punishing as a chip-damage tax.
    //
    //   stake  = effect value (coins)
    //   bonus  = stake * DamagePerCoin   (flat damage, distributed across the hit's damage types)
    //   on kill: stake refunded
    //
    // Socketed on the helmet, so the stake is player-wide rather than read from the attacking weapon.
    public static class Wager
    {
        // Config default, registered in ShardEffectDefinitions.EffectConfigs.
        public const float DefaultDamagePerCoin = 0.5f;

        private const string DamagePerCoinKey = "DamagePerCoin";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float>
        {
            { DamagePerCoinKey, DefaultDamagePerCoin },
        };

        // The stake riding on the hit currently being processed, so the postfix knows what to refund. The
        // HitData is kept alongside it so a nested Character.Damage (one hit triggering another) can only
        // ever cancel the refund, never pay it out against the wrong hit.
        private static HitData stakedHit;
        private static int stakedCoins;

        // Tooltip: {0} is the stake (the raw rolled value), {1} the damage it buys.
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.Wager,
                value => new object[] { value, value * GetDamagePerCoin() });
        }

        // Prefix handler invoked by CharacterDamageDispatch (attacker-side outgoing modifier).
        public static void ModifyOutgoingHit(Character __instance, HitData hit, Character attacker)
        {
            stakedHit = null;
            stakedCoins = 0;

            if (hit == null || !(attacker is Player player) || player != Player.m_localPlayer)
            {
                return;
            }

            // Don't burn coins on friendlies.
            if (__instance == null || __instance == player || __instance.IsTamed())
            {
                return;
            }

            int stake = Mathf.RoundToInt(player.GetTotalActiveMagicEffectValue(MagicEffectType.Wager));
            if (stake <= 0)
            {
                return;
            }

            float total = hit.m_damage.EpicLootGetTotalDamage();
            if (total <= 0f)
            {
                return;
            }

            List<ItemDrop.ItemData> coins = CoinPurse.GetCoinStacks(player);
            if (CoinPurse.GetTotalCoins(coins) < stake)
            {
                return; // can't cover the stake -- no bonus
            }

            float bonus = stake * GetDamagePerCoin();
            if (bonus <= 0f)
            {
                return;
            }

            CoinPurse.Spend(player, coins, stake);

            // Flat add, applied as a scale so it splits across the hit's damage types in their existing
            // proportions (and so resistances still apply the way they would to the base hit).
            hit.m_damage.Modify((total + bonus) / total);

            stakedHit = hit;
            stakedCoins = stake;
        }

        // Postfix handler invoked by CharacterDamageDispatch (on-hit reaction). Kill detection mirrors
        // StaminaOnKill -- the hit must have dropped the target to (or below) zero health.
        public static void OnDamageDealt(Character __instance, HitData hit)
        {
            var owedFor = stakedHit;
            var refund = stakedCoins;
            stakedHit = null;
            stakedCoins = 0;

            if (refund <= 0 || owedFor == null || !ReferenceEquals(owedFor, hit) || __instance == null)
            {
                return;
            }

            if (__instance.GetHealth() > 0f)
            {
                return; // the bet was lost -- the stake is gone
            }

            CoinPurse.Refund(Player.m_localPlayer, refund);
        }

        private static float GetDamagePerCoin()
        {
            var config = MagicItemEffectDefinitions.GetEffectConfig(MagicEffectType.Wager);
            if (config != null && config.TryGetValue(DamagePerCoinKey, out var value))
            {
                return Mathf.Max(0f, value);
            }
            return DefaultDamagePerCoin;
        }
    }
}
