using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Golden (Fortune) chest shard effect: spend coins on each hit to boost that hit's damage by the
    // effect's percentage. If the player can't cover the cost, no coins are spent and no bonus is given.
    public static class SpendCoinsToIncreaseDamage
    {
        // Coins spent per boosted hit. Tunable; the prefab name matches how CoinHoarder locates coins.
        private const int CoinPercentPerHit = 5;
        private const string CoinsPrefab = "Coins";

        // Tooltip: "Spend {1} Coins for +{0}% Damage" -- {1} surfaces the per-hit coin cost from the const.
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.SpendCoinsToIncreaseDamage,
                value => new object[] { value, value, CoinPercentPerHit });
        }

        // Prefix handler invoked by CharacterDamageDispatch (attacker-side outgoing modifier).
        public static void ModifyOutgoingHit(HitData hit, Character attacker)
        {
            if (!(attacker is Player player) || player != Player.m_localPlayer)
            {
                return;
            }

            // Socketed on chest armor, so this is a player-wide effect.
            float bonus = player.GetTotalActiveMagicEffectValue(MagicEffectType.SpendCoinsToIncreaseDamage);
            if (bonus <= 0f)
            {
                return;
            }

            List<ItemDrop.ItemData> coins = player.GetInventory().GetAllItems()
                .Where(i => i.m_dropPrefab != null && i.m_dropPrefab.name == CoinsPrefab).ToList();
            int sum = coins.Sum(c => c.m_stack);
            int cost = Mathf.RoundToInt(bonus + (sum * CoinPercentPerHit * 0.01f));
            if (sum < cost)
            {
                return;
            }

            // Remove by the coin item's own display name (robust to world-level gating).
            player.GetInventory().RemoveItem(coins[0].m_shared.m_name, cost, -1, false);
            hit.m_damage.Modify(1f + (bonus * 0.01f));
        }
    }
}
