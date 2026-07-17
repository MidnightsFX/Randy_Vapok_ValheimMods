using Jotunn.Managers;
using System;
using System.Linq;

namespace EpicLoot.ShardStones
{
    // Console-command backed helpers. These provide a fully functional, UI-independent way to use and
    // test the socket system (spawn shards, socket/unsocket the equipped item, inspect sockets).

    // This can likely be completely removed once the UI is fully implemented.
    public static class ShardDebug
    {
        public static void SpawnShard(Terminal context, string[] args)
        {
            if (Player.m_localPlayer == null)
            {
                return;
            }

            var rarity = ItemRarity.Epic;
            if (args.Length >= 2 && !Enum.TryParse(args[1], true, out rarity))
            {
                context.AddString($"> Unknown rarity '{args[1]}'. Use Magic/Rare/Epic/Legendary/Mythic.");
                return;
            }

            var color = ShardType.Yellow;
            if (args.Length >= 3 && !Enum.TryParse(args[2], true, out color))
            {
                context.AddString($"> Unknown shard color '{args[2]}'.");
                return;
            }

            var actualRarity = Shards.ClampToRaritySet(color, rarity);
            if (TrySpawnShard(context, color, actualRarity))
            {
                context.AddString($"> Spawned {color} Shard ({actualRarity}).");
            }
        }

        // Spawns one shard of every color (except the None error path) at the given rarity (default Epic).
        public static void SpawnAllShards(Terminal context, string[] args)
        {
            if (Player.m_localPlayer == null)
            {
                return;
            }

            var rarity = ItemRarity.Epic;
            if (args.Length >= 2 && !Enum.TryParse(args[1], true, out rarity))
            {
                context.AddString($"> Unknown rarity '{args[1]}'. Use Magic/Rare/Epic/Legendary/Mythic.");
                return;
            }

            var spawned = 0;
            foreach (ShardType color in Enum.GetValues(typeof(ShardType)))
            {
                if (color == ShardType.None)
                {
                    continue;
                }

                if (TrySpawnShard(context, color, Shards.ClampToRaritySet(color, rarity)))
                {
                    spawned++;
                }
            }

            context.AddString($"> Spawned {spawned} shard(s) ({rarity}).");
        }

        // Clones the requested shard prefab into the player's inventory. Returns false (and prints why) if
        // the prefab is missing or the inventory rejects it.
        private static bool TrySpawnShard(Terminal context, ShardType color, ItemRarity rarity)
        {
            // One prefab per (color, rarity) now (Shards.CreateAndLoadShardItems); the name encodes both.
            string prefabName = $"{color}_{rarity}_ShardStone";
            var shard = PrefabManager.Instance.GetPrefab(prefabName);
            ItemDrop id = shard != null ? shard.GetComponent<ItemDrop>() : null;
            if (id == null)
            {
                context.AddString($"> Failed to get shard prefab '{prefabName}'.");
                return false;
            }

            // Clone so we add a fresh item rather than the shared prefab's ItemData reference.
            var itemData = id.m_itemData.Clone();
            itemData.m_dropPrefab = shard;
            itemData.m_stack = 1;
            // The prefab already bakes this rarity/color; re-stamp defensively so the metadata is present
            // regardless of how the clone was produced.
            Shards.StampShard(itemData, rarity);

            bool status = Player.m_localPlayer.GetInventory().AddItem(itemData);
            if (!status)
            {
                context.AddString($"> Failed to add {color} shard to inventory.");
                return false;
            }
            return true;
        }

        public static void PrintSocketInfo(Terminal context)
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            foreach (var item in player.GetMagicEquipment())
            {
                if (!item.IsMagic(out var magicItem) || !magicItem.HasSockets())
                {
                    continue;
                }

                context.AddString($"> {item.m_shared.m_name}: {magicItem.GetUsedSocketCount()}/{magicItem.SocketCount} sockets");
                foreach (var socket in magicItem.Sockets)
                {
                    context.AddString($"    - {socket.Effect.EffectType} ({socket.Effect.EffectValue})");
                }
            }
        }

        public static void SocketFirstEligible(Terminal context)
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            var inventory = player.GetInventory();
            foreach (var equipment in player.GetMagicEquipment())
            {
                if (!equipment.IsMagic(out var magicItem) || !magicItem.HasOpenSocket())
                {
                    continue;
                }

                foreach (var input in inventory.GetAllItems().ToList())
                {
                    if (ShardSocketManager.CanSocket(equipment, input, out _) && ShardSocketManager.AddShard(equipment, input))
                    {
                        inventory.RemoveItem(input, 1);
                        context.AddString($"> Socketed {input.m_shared.m_name} into {equipment.m_shared.m_name}.");
                        return;
                    }
                }
            }

            context.AddString("> No eligible socket/runestone-shard combination found.");
        }

        public static void UnsocketFirst(Terminal context)
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            foreach (var equipment in player.GetMagicEquipment())
            {
                if (!equipment.IsMagic(out var magicItem) || magicItem.Sockets.Count == 0)
                {
                    continue;
                }

                var recovered = ShardSocketManager.RemoveShard(equipment, magicItem.Sockets.Count - 1);
                if (recovered != null)
                {
                    player.GetInventory().AddItem(recovered);
                    context.AddString($"> Removed socket from {equipment.m_shared.m_name}, returned {recovered.m_shared.m_name}.");
                    return;
                }
            }

            context.AddString("> No socketed equipment found.");
        }
    }
}
