using System;
using System.Collections.Generic;
using System.Linq;
using EpicLoot.LegendarySystem;
using EpicLoot.ShardStones;
using UnityEngine;

namespace EpicLoot.Compendium;

public class ItemTypeOption
{
    public string Name;
    public Func<ItemDrop.ItemData, bool> Matches;
}

/// <summary>
/// The item types a compendium page can be narrowed to, and the effects each can carry. An effect counts
/// for a type when any item of that type in the object database can roll it, socket a shardstone that
/// grants it, or become a legendary or set piece that grants it: the same checks loot uses.
/// </summary>
public static class ItemTypeFilter
{
    private static readonly ItemRarity[] Rarities =
    {
        ItemRarity.Magic, ItemRarity.Rare, ItemRarity.Epic, ItemRarity.Legendary, ItemRarity.Mythic, ItemRarity.Ancient,
    };

    public static readonly ItemTypeOption[] Options =
    {
        Slot("$mod_epicloot_itemtype_helmet", ItemDrop.ItemData.ItemType.Helmet),
        Slot("$mod_epicloot_itemtype_chest", ItemDrop.ItemData.ItemType.Chest),
        Slot("$mod_epicloot_itemtype_legs", ItemDrop.ItemData.ItemType.Legs),
        Slot("$mod_epicloot_itemtype_shoulder", ItemDrop.ItemData.ItemType.Shoulder),
        new ItemTypeOption
        {
            Name = "$mod_epicloot_itemtype_onehandedweapon",
            Matches = x => x.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon && !IsStaff(x),
        },
        new ItemTypeOption
        {
            Name = "$mod_epicloot_itemtype_twohandedweapon",
            Matches = x => (x.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon ||
                x.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft) && !IsStaff(x),
        },
        new ItemTypeOption
        {
            Name = "$mod_epicloot_itemtype_bow",
            Matches = x => x.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow && x.m_shared.m_skillType == Skills.SkillType.Bows,
        },
        new ItemTypeOption
        {
            Name = "$mod_epicloot_itemtype_crossbows",
            Matches = x => x.m_shared.m_skillType == Skills.SkillType.Crossbows,
        },
        new ItemTypeOption { Name = "$mod_epicloot_itemtype_staff", Matches = IsStaff },
        Slot("$mod_epicloot_itemtype_shield", ItemDrop.ItemData.ItemType.Shield),
        Slot("$mod_epicloot_itemtype_torch", ItemDrop.ItemData.ItemType.Torch),
        Slot("$mod_epicloot_itemtype_tool", ItemDrop.ItemData.ItemType.Tool),
        Slot("$mod_epicloot_itemtype_utility", ItemDrop.ItemData.ItemType.Utility),
        Slot("$mod_epicloot_itemtype_trinket", ItemDrop.ItemData.ItemType.Trinket),
    };

    private static readonly Dictionary<ItemTypeOption, HashSet<string>> Cache = new();

    public static void Invalidate() => Cache.Clear();

    public static HashSet<string> EffectsFor(ItemTypeOption option)
    {
        if (Cache.TryGetValue(option, out HashSet<string> cached))
        {
            return cached;
        }

        HashSet<string> effects = new();
        foreach (ItemDrop.ItemData item in GetItems(option))
        {
            foreach (ShardType color in Shards.ShardDefinitions.ShardEffects.Keys)
            {
                Add(effects, color == ShardType.None ? null : Shards.GetShardEffect(item, color)?.EffectType);
            }

            foreach (ItemRarity rarity in Rarities)
            {
                MagicItem magicItem = new MagicItem { Rarity = rarity };
                foreach (MagicItemEffectDefinition definition in MagicItemEffectDefinitions.GetAvailableEffects(item, magicItem))
                {
                    Add(effects, definition.Type);
                }

                IEnumerable<LegendaryInfo> legendaries = UniqueLegendaryHelper.GetAvailableUniques(item, magicItem, rarity)
                    .Concat(UniqueLegendaryHelper.GetAvailableSetPieces(item, magicItem, rarity));
                foreach (LegendaryInfo legendary in legendaries)
                {
                    AddGranted(effects, legendary);
                }
            }
        }

        Cache[option] = effects;
        return effects;
    }

    private static void AddGranted(HashSet<string> effects, LegendaryInfo legendary)
    {
        foreach (GuaranteedMagicEffect effect in legendary.GuaranteedMagicEffects ?? new List<GuaranteedMagicEffect>())
        {
            Add(effects, effect.Type);
        }

        if (string.IsNullOrEmpty(legendary.ID))
        {
            return;
        }

        if (UniqueLegendaryHelper.ItemsToSetMap.TryGetValue(legendary.ID, out LegendarySetInfo set) ||
            UniqueLegendaryHelper.MythicItemsToSetMap.TryGetValue(legendary.ID, out set))
        {
            foreach (SetBonusInfo bonus in set.SetBonuses ?? new List<SetBonusInfo>())
            {
                Add(effects, bonus.Effect?.Type);
            }
        }
    }

    private static void Add(HashSet<string> effects, string effectType)
    {
        if (!string.IsNullOrEmpty(effectType))
        {
            effects.Add(effectType);
        }
    }

    private static IEnumerable<ItemDrop.ItemData> GetItems(ItemTypeOption option)
    {
        if (ObjectDB.instance == null)
        {
            yield break;
        }

        foreach (GameObject prefab in ObjectDB.instance.m_items)
        {
            ItemDrop.ItemData data = prefab != null ? prefab.GetComponent<ItemDrop>()?.m_itemData : null;
            if (data?.m_shared == null || LootDenyList.IsDenied(prefab.name) || !option.Matches(data))
            {
                continue;
            }

            ItemDrop.ItemData item = data.Clone();
            item.m_dropPrefab = prefab;
            yield return item;
        }
    }

    private static ItemTypeOption Slot(string name, ItemDrop.ItemData.ItemType type) =>
        new ItemTypeOption { Name = name, Matches = x => x.m_shared.m_itemType == type };

    private static bool IsStaff(ItemDrop.ItemData item) =>
        item.m_shared.m_skillType == Skills.SkillType.BloodMagic || item.m_shared.m_skillType == Skills.SkillType.ElementalMagic;
}
