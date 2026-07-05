using System.Collections.Generic;
using EpicLoot.Config;
using EpicLoot.Crafting;
using Jotunn.Managers;
using UnityEngine;

namespace EpicLoot.ShardStones
{
    // Core socketing logic, independent of any UI. Socketed effects are stored on the equipment's
    // MagicItem (MagicItem.Sockets) and applied through the normal effect pipeline.
    public static class ShardSocketManager
    {
        // Resolves the effect a socketable input yields when placed into the given equipment.
        // Returns true when the input is a valid socketable; on true, `effect` may still be null for an
        // inert shard (one with no defined effect for the equipment's item type), while `color` and
        // `rarity` describe the source shard (color is None for runestones).
        public static bool ResolveSocketedEffect(ItemDrop.ItemData equipment, ItemDrop.ItemData input,
            out MagicItemEffect effect, out ShardColor color, out ItemRarity rarity)
        {
            effect = null;
            color = ShardColor.None;
            rarity = ItemRarity.Magic;

            if (equipment == null || input == null)
            {
                return false;
            }

            if (Shards.IsShard(input))
            {
                color = Shards.GetShardColor(input);
                if (color == ShardColor.None)
                {
                    return false; // malformed shard
                }
                rarity = Shards.GetShardRarity(input);

                // The shard's effect depends on the host item's type. A missing mapping is a valid,
                // inert placement (effect stays null).
                var shardEffect = Shards.GetShardEffect(equipment, color);
                if (shardEffect != null && shardEffect.ValuesPerRarity.TryGetValue(rarity, out var value))
                {
                    effect = new MagicItemEffect(shardEffect.EffectType, value);
                }
                return true;
            }

            if (input.IsRunestone() && input.IsMagic(out var magicItem) && magicItem.Effects.Count == 1)
            {
                var source = magicItem.Effects[0];
                effect = new MagicItemEffect(source.EffectType, source.EffectValue);
                rarity = magicItem.Rarity;
                return true;
            }

            return false;
        }

        // Whether the given runestone/shard can be socketed into the given equipment.
        public static bool CanSocket(ItemDrop.ItemData equipment, ItemDrop.ItemData input, out string reason)
        {
            reason = null;

            if (equipment == null || !equipment.IsMagic(out var equipMagicItem))
            {
                reason = "$mod_epicloot_socket_notmagic";
                return false;
            }

            if (!equipMagicItem.HasOpenSocket())
            {
                reason = "$mod_epicloot_socket_nofreeslot";
                return false;
            }

            if (!ResolveSocketedEffect(equipment, input, out var effect, out var color, out _))
            {
                reason = "$mod_epicloot_socket_invalidinput";
                return false;
            }

            // Exclusive-category (e.g. boss) shards: at most one per item, and at most one across
            // worn gear. The cross-equipped rule is only enforced when the target is currently worn;
            // an unequipped item may freely receive the shard (the equip-time guard closes the loop).
            if (!CheckExclusiveCategory(equipment, color, SocketedColors(equipMagicItem.Sockets), out reason))
            {
                return false;
            }

            // A shard with no defined effect for this item type may still be socketed; it sits inert.
            if (effect == null)
            {
                return true;
            }

            var def = MagicItemEffectDefinitions.Get(effect.EffectType);
            if (def == null)
            {
                reason = "$mod_epicloot_socket_invalidinput";
                return false;
            }

            // Reuse the rune-roll legality rules: the effect must be allowed on this equipment type
            // and must not violate exclusivity with the item's rolled effects.
            if (!def.Requirements.CheckRequirements(equipment, equipMagicItem, effect.EffectType,
                    checklootroll: false, checkaugmentroll: false, checkruneroll: true))
            {
                reason = "$mod_epicloot_socket_notallowed";
                return false;
            }

            if (!ELConfig.AllowDuplicateSocketedEffects.Value &&
                equipMagicItem.Sockets.Exists(s => s.Effect != null && s.Effect.EffectType == effect.EffectType))
            {
                reason = "$mod_epicloot_socket_duplicate";
                return false;
            }

            return true;
        }

        // Whether `input` may occupy a socket in `equipment` while sharing it with `coResident` (the other
        // socketables that will remain). Mirrors CanSocket's legality rules but takes the co-resident set
        // explicitly and does no free-slot check, so it fits swap validation: dragging a socketed item out
        // onto an inventory item makes vanilla push that inventory item into the vacated socket, and the
        // pushed item must obey the same duplicate/requirement rules as a fresh drop -- measured against the
        // sockets that survive once the dragged item leaves.
        public static bool CanCoexist(ItemDrop.ItemData equipment, ItemDrop.ItemData input,
            IEnumerable<ItemDrop.ItemData> coResident, out string reason)
        {
            reason = null;

            if (equipment == null || !equipment.IsMagic(out var equipMagicItem))
            {
                reason = "$mod_epicloot_socket_notmagic";
                return false;
            }

            if (!ResolveSocketedEffect(equipment, input, out var effect, out var color, out _))
            {
                reason = "$mod_epicloot_socket_invalidinput";
                return false;
            }

            // Exclusive-category (e.g. boss) shards obey the same one-per-item / one-across-worn-gear
            // rule on the swap path, measured against the co-resident shards that survive the swap.
            var coResidentColors = new List<ShardColor>();
            foreach (var other in coResident)
            {
                coResidentColors.Add(Shards.GetShardColor(other));
            }
            if (!CheckExclusiveCategory(equipment, color, coResidentColors, out reason))
            {
                return false;
            }

            // An inert shard (no effect for this item type) may always sit in a socket.
            if (effect == null)
            {
                return true;
            }

            var def = MagicItemEffectDefinitions.Get(effect.EffectType);
            if (def == null)
            {
                reason = "$mod_epicloot_socket_invalidinput";
                return false;
            }

            if (!def.Requirements.CheckRequirements(equipment, equipMagicItem, effect.EffectType,
                    checklootroll: false, checkaugmentroll: false, checkruneroll: true))
            {
                reason = "$mod_epicloot_socket_notallowed";
                return false;
            }

            if (!ELConfig.AllowDuplicateSocketedEffects.Value)
            {
                foreach (var other in coResident)
                {
                    if (other != null &&
                        ResolveSocketedEffect(equipment, other, out var otherEffect, out _, out _) &&
                        otherEffect != null && otherEffect.EffectType == effect.EffectType)
                    {
                        reason = "$mod_epicloot_socket_duplicate";
                        return false;
                    }
                }
            }

            return true;
        }

        // Sockets the input's effect into the equipment. Returns true on success.
        public static bool AddShard(ItemDrop.ItemData equipment, ItemDrop.ItemData input)
        {
            if (!CanSocket(equipment, input, out _))
            {
                return false;
            }

            var equipMagicItem = equipment.GetMagicItem();
            ResolveSocketedEffect(equipment, input, out var effect, out var color, out var sourceRarity);
            var sourcePrefab = GetSourcePrefabName(input);

            equipMagicItem.Sockets.Add(new SocketedEffect(effect, sourcePrefab, sourceRarity) { ShardColor = color });
            equipment.SaveMagicItem(equipMagicItem);
            ResetCache();
            return true;
        }

        // Removes the socket at the given index and returns a reconstructed runestone/shard item that
        // the caller should give back to the player. Returns null if the index is invalid.
        public static ItemDrop.ItemData RemoveShard(ItemDrop.ItemData equipment, int socketIndex)
        {
            if (equipment == null || !equipment.IsMagic(out var equipMagicItem))
            {
                return null;
            }

            if (socketIndex < 0 || socketIndex >= equipMagicItem.Sockets.Count)
            {
                return null;
            }

            var socketed = equipMagicItem.Sockets[socketIndex];
            equipMagicItem.Sockets.RemoveAt(socketIndex);
            equipment.SaveMagicItem(equipMagicItem);
            ResetCache();
            return ReconstructShardItem(socketed);
        }

        // Rebuilds the original Runestone/Shard item from a stored socket. Runestones carry their fixed
        // effect back; shards are rebuilt effect-less (a shard's effect is derived from the host item
        // type, so a loose shard has no baked effect), which also covers inert shard sockets.
        public static ItemDrop.ItemData ReconstructShardItem(SocketedEffect socketed)
        {
            if (socketed == null || string.IsNullOrEmpty(socketed.SourcePrefab))
            {
                return null;
            }

            var prefab = PrefabManager.Instance.GetPrefab(socketed.SourcePrefab);
            if (prefab == null)
            {
                EpicLoot.LogErrorForce($"Could not reconstruct socketed item, missing prefab '{socketed.SourcePrefab}'");
                return null;
            }

            var baseData = prefab.GetComponent<ItemDrop>();
            if (baseData == null)
            {
                return null;
            }

            var item = baseData.m_itemData.Clone();
            item.m_dropPrefab = prefab;
            item.m_stack = 1;

            var magicItem = new MagicItem { Rarity = socketed.SourceRarity };
            if (socketed.ShardColor == ShardColor.None && socketed.Effect != null)
            {
                magicItem.Effects.Add(new MagicItemEffect(socketed.Effect.EffectType, socketed.Effect.EffectValue));
            }
            item.SaveMagicItem(magicItem);
            return item;
        }

        // The socketable input is always an EtchedRunestone or a Shard of the given rarity. Deriving
        // the prefab name from type + rarity is robust regardless of m_dropPrefab being set.
        public static string GetSourcePrefabName(ItemDrop.ItemData input)
        {
            if (input.IsRunestone())
            {
                return $"EtchedRunestone{input.GetMagicItem().Rarity}";
            }

            string[] shardData = input.m_shared.m_ammoType.Split('|');
            if (shardData.Length == 3)
            {
                return $"{shardData[1]}_{shardData[0]}_ShardStone";
            }

            return "";
        }

        private static void ResetCache()
        {
            if (Player.m_localPlayer != null)
            {
                EquipmentEffectCache.Reset(Player.m_localPlayer);
            }
        }

        // Enforces exclusive-category rules for socketing `inputColor` into `equipment`:
        //   1. Item-local: no two shards of an exclusive category may share the same item
        //      (measured against `itemLocalColors`, the shards already occupying that item).
        //   2. Cross-equipped: only one shard of an exclusive category across worn gear -- enforced
        //      only when `equipment` is currently worn (an unequipped item is caught at equip time).
        // Non-exclusive inputs (regular shards, runestones) always pass.
        private static bool CheckExclusiveCategory(ItemDrop.ItemData equipment, ShardColor inputColor,
            IEnumerable<ShardColor> itemLocalColors, out string reason)
        {
            reason = null;

            if (inputColor == ShardColor.None)
            {
                return true;
            }

            var category = Shards.GetCategory(inputColor);
            if (!Shards.IsExclusive(category))
            {
                return true;
            }

            foreach (var color in itemLocalColors)
            {
                if (color != ShardColor.None && Shards.GetCategory(color) == category)
                {
                    reason = "$mod_epicloot_socket_bosslimit";
                    return false;
                }
            }

            var player = Player.m_localPlayer;
            if (player != null && player.IsItemEquiped(equipment) &&
                IsExclusiveCategoryEquipped(player, category, equipment))
            {
                reason = "$mod_epicloot_socket_bosslimit";
                return false;
            }

            return true;
        }

        // The shard colors currently occupying an item's sockets (None for runestone sockets).
        private static IEnumerable<ShardColor> SocketedColors(IEnumerable<SocketedEffect> sockets)
        {
            var colors = new List<ShardColor>();
            foreach (var socket in sockets)
            {
                colors.Add(socket != null ? socket.ShardColor : ShardColor.None);
            }
            return colors;
        }

        // True when any equipped magic item other than `excluding` already holds a shard of `category`.
        public static bool IsExclusiveCategoryEquipped(Player player, ShardCategory category, ItemDrop.ItemData excluding)
        {
            foreach (var equipped in player.GetMagicEquipment())
            {
                if (equipped == excluding || !equipped.IsMagic(out var magicItem))
                {
                    continue;
                }

                if (ItemHasCategory(magicItem, category))
                {
                    return true;
                }
            }
            return false;
        }

        // True when any of the item's sockets holds a shard belonging to `category`.
        public static bool ItemHasCategory(MagicItem magicItem, ShardCategory category)
        {
            foreach (var socket in magicItem.Sockets)
            {
                if (socket != null && socket.ShardColor != ShardColor.None &&
                    Shards.GetCategory(socket.ShardColor) == category)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
