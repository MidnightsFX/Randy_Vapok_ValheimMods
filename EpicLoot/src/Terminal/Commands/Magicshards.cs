using EpicLoot.ShardStones;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace EpicLoot;

public static partial class TerminalManager {
    private static void SpawnMagicShards(Terminal.ConsoleEventArgs args) {
        if (Player.m_localPlayer == null) {
            return;
        }

        var rarity = ItemRarity.Epic;
        if (args.Length >= 2 && !Enum.TryParse(args[1], true, out rarity)) {
            args.Context.AddString($"> Unknown rarity '{args[1]}'. Use Magic/Rare/Epic/Legendary/Mythic.");
            return;
        }

        foreach (ShardType color in Enum.GetValues(typeof(ShardType))) {
            if (color == ShardType.None) {
                continue;
            }

            string assetName = $"{color}_{rarity}_ShardStone";
            GameObject itemPrefab = PrefabManager.Instance.GetPrefab(assetName);
            Transform transform = Player.m_localPlayer.transform;
            ItemDrop itemDrop = UnityEngine.Object.Instantiate(itemPrefab,
                transform.position + transform.forward * 2f + Vector3.up,
                Quaternion.identity).GetComponent<ItemDrop>();
        }
    }
}