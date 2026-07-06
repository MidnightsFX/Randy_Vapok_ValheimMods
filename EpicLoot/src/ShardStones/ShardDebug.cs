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

            // Prefab names are built in Shards.CreateAndLoadShardItems as "{Color}_{Rarity}_ShardStone".
            string prefabName = $"{color}_{rarity}_ShardStone";
            var shard = PrefabManager.Instance.GetPrefab(prefabName);
            ItemDrop id = shard != null ? shard.GetComponent<ItemDrop>() : null;
            if (id == null)
            {
                context.AddString($"> Failed to get shard prefab '{prefabName}'.");
                return;
            }

            // Clone so we add a fresh item rather than the shared prefab's ItemData reference.
            var itemData = id.m_itemData.Clone();
            itemData.m_dropPrefab = shard;
            itemData.m_stack = 1;

            bool status = Player.m_localPlayer.GetInventory().AddItem(itemData);
            if (!status)
            {
                context.AddString($"> Failed to add shard to inventory.");
                return;
            }
            context.AddString($"> Spawned {color} Shard ({rarity}).");
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
