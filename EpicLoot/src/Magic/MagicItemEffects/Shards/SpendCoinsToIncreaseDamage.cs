using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Golden (Fortune) chest shard effect: spend coins on each hit to boost that hit's damage by the
    // effect's percentage. If the player can't cover the cost, no coins are spent and no bonus is given.
    public static class SpendCoinsToIncreaseDamage
    {
        // Coins spent per boosted hit. Tunable; the prefab name matches how CoinHoarder locates coins.
        private const int CoinsPerHit = 5;
        private const string CoinsPrefab = "Coins";

        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        private static class Character_Damage_Patch
        {
            private static void Prefix(HitData hit)
            {
                if (!(hit.GetAttacker() is Player player) || player != Player.m_localPlayer)
                {
                    return;
                }

                // Socketed on chest armor, so this is a player-wide effect.
                float bonus = player.GetTotalActiveMagicEffectValue(MagicEffectType.SpendCoinsToIncreaseDamage, 0.01f);
                if (bonus <= 0f)
                {
                    return;
                }

                List<ItemDrop.ItemData> coins = player.GetInventory().GetAllItems()
                    .Where(i => i.m_dropPrefab != null && i.m_dropPrefab.name == CoinsPrefab).ToList();
                if (coins.Sum(c => c.m_stack) < CoinsPerHit)
                {
                    return;
                }

                // Remove by the coin item's own display name (robust to world-level gating).
                player.GetInventory().RemoveItem(coins[0].m_shared.m_name, CoinsPerHit, -1, false);
                hit.m_damage.Modify(1f + bonus);
            }
        }
    }
}
