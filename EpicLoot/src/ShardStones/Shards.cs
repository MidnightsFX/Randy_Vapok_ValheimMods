using EpicLoot.Crafting;
using EpicLoot.Data;
using EpicLoot.GatedItemType;
using HarmonyLib;
using JetBrains.Annotations;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using static ItemDrop;

namespace EpicLoot.ShardStones {
    // A Shard's color determines which set of magical effects it has along with its icon
    public enum ShardType {
        // Core Shards
        Red = 0,  // Vitality
        Yellow = 1, // Stamina
        Cyan = 2,  // Eitr
        // Standard Shards
        Black = 3, // Night time effects
        Green = 4, // Movement
        Orange = 5, // Fire
        Pink = 6, // Dodge
        Purple = 7, // Eitr
        White = 8, // Daytime
        Grey = 9,
        // not shift the shards below it -- ShardType is serialized by ordinal on socketed gear.
        // Dark shards
        DarkGreen = 30,
        DarkPurple = 31,
        DarkRed = 32,
        DarkBlue = 33, // cold resistances
        Golden = 34,
        // Light shards
        LightBlue = 40,
        LightGreen = 41,
        Peach = 42,
        //LightRed = 43,
        // Boss shards
        Eikthyr = 90, // Stamina as eitr
        Elder = 91, // Summon roots occasionally to fight for you
        Bonemass = 92, // Applies poison to enemies on hit (chance)
        Moder = 93, // Chance to cause a frost nova when you are hit
        Yagluth = 94, // Chance to summon a meteor on hit
        Queen = 95, // Gain a small amount of eitr from stamina usage
        Fader = 96, // Chance to cause an aoe fire explosion on hit

        // This is the error path
        None
    }

    // Groups shards into classes. A category may be flagged exclusive (see Shards.IsExclusive),
    // meaning a player may wear at most one socketed shard of that category at a time.
    public enum ShardCategory {
        Core,
        Dark,
        Light,
        Boss,
        Unique
    }

    // The equipment "slot" a shard resolves to when socketed. This mirrors EpicLoot's ItemInfo types
    // (config/iteminfo.json) so a shard can grant a different effect to each individual item type, while
    // still assigning one effect to a whole group (all melee weapons, all shields, all armor) as a
    // fallback. Shards.ResolveCategory maps a host item to the most specific fine type it can; a fine
    // effect overrides its group and a group effect covers every member that has no fine effect.
    public enum ShardSlotCategory {
        // -------- Groups (broad fallback keys; not resolution targets in the primary path) --------
        MeleeWeapon,
        RangedWeapon,
        MagicWeapon,
        Shield,
        Armor,

        // -------- Fine weapon types (mirror ItemInfo) --------
        Swords,
        Axes,
        TwoHandAxes,
        Knives,
        Fists,
        Clubs,
        Sledges,
        Polearms,
        Spears,
        Pickaxes,
        Tools,
        Torches,
        Bows,
        Staffs,

        // -------- Fine shield types --------
        Bucklers,
        RoundShields,
        TowerShields,

        // -------- Fine armor types --------
        Head,
        Chest,
        Legs,
        Shoulders,

        // -------- Standalone (no group) --------
        Trinket,
        Utility
    }

    public class ShardEffectDefinition {
        public string EffectType;
        public Dictionary<ItemRarity, float> ValuesPerRarity = new Dictionary<ItemRarity, float>();
    }

    public class ShardDefinition {
        public ShardCategory Category = ShardCategory.Core;

        // The rarities this shard can be created/dropped at. Each (color, rarity) is a distinct prefab and
        // a shard's rarity is stored per instance in its MagicItem metadata; this set constrains drop rolls
        // and debug spawns (see Shards.ClampToRaritySet). Left empty here on purpose: Newtonsoft APPENDS
        // to a pre-initialized collection, so a non-empty default would merge with the JSON list;
        // InitializeShardDefinitions backfills all five when the config omits it.
        public List<ItemRarity> Rarities = new List<ItemRarity>();

        // When non-null, the shard grants this single effect on ANY host item type it is allowed
        // into (a "uniform" shard), instead of the per-item-type TypeEffects mapping below.
        public ShardEffectDefinition UniformEffect = null;

        public Dictionary<ShardSlotCategory, ShardEffectDefinition> TypeEffects = new Dictionary<ShardSlotCategory, ShardEffectDefinition>();

        public float GetValue(ShardSlotCategory category, ItemRarity rarity) {
            if (UniformEffect != null) {
                return UniformEffect.ValuesPerRarity.TryGetValue(rarity, out var uniform) ? uniform : 0f;
            }
            if (TypeEffects.TryGetValue(category, out var effectDef)) {
                return effectDef.ValuesPerRarity.TryGetValue(rarity, out var value) ? value : 0f;
            }
            return 0f;
        }
    }

    // Root of config/shardstones.json: the full shard effect/rarity grid keyed by color.
    public class ShardStonesConfig {
        public Dictionary<ShardType, ShardDefinition> Shards = new Dictionary<ShardType, ShardDefinition>();
    }

    public static class Shards {
        public static readonly String ShardIndicator = "ShardStone";

        // Per-(color, rarity) prefab stack cap. Each (color, rarity) is a distinct prefab with a distinct
        // display name, so only identical-rarity shards of the same color merge -- up to this many.
        private const int ShardStackSize = 100;

        // Shard effect/rarity definitions, loaded from config/shardstones.json (registered in
        // ELConfig.InitializeConfig). Keyed by color; each carries its own rarity set.
        private static Dictionary<ShardType, ShardDefinition> _definitions =
            new Dictionary<ShardType, ShardDefinition>();

        // Config setup hook (SychronizeConfig<ShardStonesConfig>). Backfills defaults so downstream
        // lookups never hit a null Rarities/TypeEffects.
        public static void InitializeShardDefinitions(ShardStonesConfig config) {
            _definitions = config?.Shards ?? new Dictionary<ShardType, ShardDefinition>();
            foreach (var def in _definitions.Values) {
                if (def == null) {
                    continue;
                }
                if (def.Rarities == null || def.Rarities.Count == 0) {
                    def.Rarities = new List<ItemRarity> {
                        ItemRarity.Magic, ItemRarity.Rare, ItemRarity.Epic, ItemRarity.Legendary, ItemRarity.Mythic
                    };
                }
                if (def.TypeEffects == null) {
                    def.TypeEffects = new Dictionary<ShardSlotCategory, ShardEffectDefinition>();
                }
            }
        }

        public static ShardStonesConfig GetCFG() {
            return new ShardStonesConfig { Shards = _definitions };
        }

        // Sets a shard instance's rarity in its MagicItem metadata -- the semantic source of truth read
        // by GetShardRarity, socketing, and tooltips. Rarity is otherwise encoded in the prefab name /
        // distinct display name (see CreateAndLoadShardItems), which is what separates inventory stacks;
        // m_quality is no longer used for shards. Always assign shard rarity through here.
        public static void StampRarity(ItemDrop.ItemData item, ItemRarity rarity) {
            if (item == null) {
                return;
            }
            var mic = item.Data().GetOrCreate<MagicItemComponent>();
            var magicItem = mic.MagicItem ?? new MagicItem();
            magicItem.Rarity = rarity;
            mic.SetMagicItem(magicItem);
        }

        // Snaps a rarity to the nearest one in a color's declared set (Rarities in shardstones.json).
        // Returns the input unchanged when the set is empty/undefined or already contains it.
        public static ItemRarity ClampToRaritySet(ShardType color, ItemRarity rarity) {
            var set = ShardDefinitions.Get(color)?.Rarities;
            if (set == null || set.Count == 0 || set.Contains(rarity)) {
                return rarity;
            }
            ItemRarity best = set[0];
            int bestDiff = int.MaxValue;
            foreach (var r in set) {
                int diff = Math.Abs((int)r - (int)rarity);
                if (diff < bestDiff) {
                    bestDiff = diff;
                    best = r;
                }
            }
            return best;
        }

        // Accessors kept under the ShardDefinitions name for existing call sites (MagicTooltipShard,
        // ShardEffectDefinitions). Backed by the config loaded above.
        public static class ShardDefinitions {
            public static Dictionary<ShardType, ShardDefinition> ShardEffects => _definitions;

            public static ShardDefinition Get(ShardType color) {
                return ShardEffects.TryGetValue(color, out var def) ? def : null;
            }
        }

        // Check if this is a shard item
        public static bool IsShard(ItemDrop.ItemData item) {
            return item?.m_shared != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Material &&
                item.m_shared.m_ammoType.EndsWith(ShardIndicator);
        }

        public static ShardType GetShardColor(ItemDrop.ItemData item) {
            // Color lives in parts[0] of the 2-part ammoType (e.g. "Yellow|ShardStone").
            if (!IsShard(item)) {
                return ShardType.None;
            }
            string[] parts = item.m_shared.m_ammoType.Split('|');
            if (parts.Length < 2) {
                return ShardType.None;
            }
            if (Enum.TryParse(parts[0], true, out ShardType color)) {
                return color;
            }
            return ShardType.None;
        }

        public static ItemRarity GetShardRarity(ItemDrop.ItemData item) {
            // A shard's rarity is stored in its MagicItem metadata (the semantic source of truth); the
            // prefab name / display name also encode it, but this metadata is what code reads.
            if (!IsShard(item)) {
                return ItemRarity.Magic;
            }
            return item.IsMagic(out var magicItem) ? magicItem.Rarity : ItemRarity.Magic;
        }

        public static ShardEffectDefinition GetShardEffect(ItemDrop.ItemData item, ShardType color) {
            if (!ShardDefinitions.ShardEffects.TryGetValue(color, out var colorEffects)) {
                return null;
            }

            // A uniform shard (e.g. a boss shard) grants the same effect on any host item type.
            if (colorEffects.UniformEffect != null) {
                return colorEffects.UniformEffect;
            }

            if (colorEffects.TypeEffects == null) {
                return null;
            }

            // A fine-type effect (e.g. Swords) overrides its group (MeleeWeapon); a group effect covers
            // every member with no fine effect of its own.
            var slot = ResolveCategory(item);
            if (colorEffects.TypeEffects.TryGetValue(slot, out var fineEffect)) {
                return fineEffect;
            }
            if (GroupOf(slot) is ShardSlotCategory group &&
                colorEffects.TypeEffects.TryGetValue(group, out var groupEffect)) {
                return groupEffect;
            }
            return null;
        }

        // Maps each EpicLoot ItemInfo type string (config/iteminfo.json "Type") to its fine slot. This is
        // the primary resolution path -- it distinguishes types that share a skill/ItemType (e.g. Axes vs
        // TwoHandAxes, the three shield subtypes) that the fallback heuristic below cannot.
        private static readonly Dictionary<string, ShardSlotCategory> ItemInfoTypeToSlot =
            new Dictionary<string, ShardSlotCategory>(StringComparer.OrdinalIgnoreCase)
            {
                { "Swords", ShardSlotCategory.Swords },
                { "Axes", ShardSlotCategory.Axes },
                { "TwoHandAxes", ShardSlotCategory.TwoHandAxes },
                { "Knives", ShardSlotCategory.Knives },
                { "Fists", ShardSlotCategory.Fists },
                { "Clubs", ShardSlotCategory.Clubs },
                { "Sledges", ShardSlotCategory.Sledges },
                { "Polearms", ShardSlotCategory.Polearms },
                { "Spears", ShardSlotCategory.Spears },
                { "Pickaxes", ShardSlotCategory.Pickaxes },
                { "Tools", ShardSlotCategory.Tools },
                { "Torches", ShardSlotCategory.Torches },
                { "Bows", ShardSlotCategory.Bows },
                { "Staffs", ShardSlotCategory.Staffs },
                { "Bucklers", ShardSlotCategory.Bucklers },
                { "RoundShields", ShardSlotCategory.RoundShields },
                { "TowerShields", ShardSlotCategory.TowerShields },
                { "HeadArmor", ShardSlotCategory.Head },
                { "ChestArmor", ShardSlotCategory.Chest },
                { "LegsArmor", ShardSlotCategory.Legs },
                { "ShouldersArmor", ShardSlotCategory.Shoulders },
                { "Trinket", ShardSlotCategory.Trinket },
                { "Utility", ShardSlotCategory.Utility }
            };

        // The broad group a fine slot falls back to when a shard defines no effect for the exact type.
        // Returns null for standalone slots (Trinket, Utility) and for group values themselves.
        public static ShardSlotCategory? GroupOf(ShardSlotCategory slot) {
            switch (slot) {
                case ShardSlotCategory.Swords:
                case ShardSlotCategory.Axes:
                case ShardSlotCategory.TwoHandAxes:
                case ShardSlotCategory.Knives:
                case ShardSlotCategory.Fists:
                case ShardSlotCategory.Clubs:
                case ShardSlotCategory.Sledges:
                case ShardSlotCategory.Polearms:
                case ShardSlotCategory.Spears:
                case ShardSlotCategory.Pickaxes:
                case ShardSlotCategory.Tools:
                case ShardSlotCategory.Torches:
                    return ShardSlotCategory.MeleeWeapon;
                case ShardSlotCategory.Bows:
                    return ShardSlotCategory.RangedWeapon;
                case ShardSlotCategory.Staffs:
                    return ShardSlotCategory.MagicWeapon;
                case ShardSlotCategory.Bucklers:
                case ShardSlotCategory.RoundShields:
                case ShardSlotCategory.TowerShields:
                    return ShardSlotCategory.Shield;
                case ShardSlotCategory.Head:
                case ShardSlotCategory.Chest:
                case ShardSlotCategory.Legs:
                case ShardSlotCategory.Shoulders:
                    return ShardSlotCategory.Armor;
                default:
                    return null;
            }
        }

        // Maps a host equipment item to the most specific fine slot a shard uses to pick its effect.
        // Primary: EpicLoot's own ItemInfo classification (covers vanilla + any item listed in
        // iteminfo.json, including modded ones). Fallback (items EpicLoot doesn't classify): a
        // skill/ItemType heuristic, resolved as finely as those raw fields allow.
        public static ShardSlotCategory ResolveCategory(ItemDrop.ItemData item) {
            var prefabName = item.m_dropPrefab?.name;
            if (!string.IsNullOrEmpty(prefabName) &&
                GatedItemTypeHelper.AllItemsWithDetails.TryGetValue(prefabName, out var details) &&
                !string.IsNullOrEmpty(details.Type) &&
                ItemInfoTypeToSlot.TryGetValue(details.Type, out var mapped)) {
                return mapped;
            }

            return ResolveCategoryFallback(item);
        }

        // Best-effort slot from an item's raw combat/type fields, for items not in iteminfo.json. Where a
        // subtype can't be distinguished (e.g. any shield), returns the broad group; GetShardEffect treats
        // a group value the same as a fine one, so a group-level shard effect still applies.
        private static ShardSlotCategory ResolveCategoryFallback(ItemDrop.ItemData item) {
            var shared = item.m_shared;
            var skill = shared.m_skillType;
            var twoHanded = shared.m_itemType == ItemData.ItemType.TwoHandedWeapon ||
                shared.m_itemType == ItemData.ItemType.TwoHandedWeaponLeft;

            switch (skill) {
                case Skills.SkillType.ElementalMagic:
                case Skills.SkillType.BloodMagic:
                    return ShardSlotCategory.Staffs;
                case Skills.SkillType.Bows:
                case Skills.SkillType.Crossbows:
                    return ShardSlotCategory.Bows;
                case Skills.SkillType.Swords:
                    return ShardSlotCategory.Swords;
                case Skills.SkillType.Knives:
                    return ShardSlotCategory.Knives;
                case Skills.SkillType.Spears:
                    return ShardSlotCategory.Spears;
                case Skills.SkillType.Polearms:
                    return ShardSlotCategory.Polearms;
                case Skills.SkillType.Pickaxes:
                    return ShardSlotCategory.Pickaxes;
                case Skills.SkillType.Unarmed:
                    return ShardSlotCategory.Fists;
                case Skills.SkillType.Axes:
                    return twoHanded ? ShardSlotCategory.TwoHandAxes : ShardSlotCategory.Axes;
                case Skills.SkillType.Clubs:
                    return twoHanded ? ShardSlotCategory.Sledges : ShardSlotCategory.Clubs;
            }

            switch (shared.m_itemType) {
                case ItemData.ItemType.Helmet:
                    return ShardSlotCategory.Head;
                case ItemData.ItemType.Chest:
                    return ShardSlotCategory.Chest;
                case ItemData.ItemType.Legs:
                    return ShardSlotCategory.Legs;
                case ItemData.ItemType.Shoulder:
                    return ShardSlotCategory.Shoulders;
                case ItemData.ItemType.Shield:
                    return ShardSlotCategory.Shield; // subtype indistinguishable -> broad group
                case ItemData.ItemType.Torch:
                    return ShardSlotCategory.Torches;
                case ItemData.ItemType.Tool:
                    return ShardSlotCategory.Tools;
                case ItemData.ItemType.OneHandedWeapon:
                case ItemData.ItemType.TwoHandedWeapon:
                case ItemData.ItemType.TwoHandedWeaponLeft:
                    return ShardSlotCategory.MeleeWeapon; // unknown melee weapon -> broad group
            }

            return ShardSlotCategory.Utility;
        }

        // Human-readable label for a slot category, e.g. "$mod_epicloot_shardslot_meleeweapon" ->
        // "Melee Weapon". Falls back to the raw enum name when no localization key is defined.
        public static string GetCategoryDisplayName(ShardSlotCategory category) {
            var token = $"mod_epicloot_shardslot_{category.ToString().ToLowerInvariant()}";
            var localized = Localization.instance.Localize($"${token}");
            return string.Equals(localized, token, StringComparison.Ordinal) ? category.ToString() : localized;
        }

        // Categories whose shards are mutually exclusive: a player may wear at most one socketed
        // shard of each such category at a time. Exclusivity is a property of the category, so it
        // applies uniformly to every color in it.
        private static readonly HashSet<ShardCategory> ExclusiveCategories = new HashSet<ShardCategory>
        {
            ShardCategory.Boss,
            ShardCategory.Unique
        };

        public static bool IsExclusive(ShardCategory category) => ExclusiveCategories.Contains(category);

        public static ShardCategory GetCategory(ShardType color) {
            var def = ShardDefinitions.Get(color);
            return def != null ? def.Category : ShardCategory.Core;
        }

        internal static void CreateAndLoadShardItems() {
            GameObject genericPrefab = EpicAssets.AssetBundle.LoadAsset<GameObject>("_ShardStone");
            CustomItem genericShard = new CustomItem(genericPrefab, false);
            ItemManager.Instance.AddItem(genericShard);
            genericPrefab.SetActive(false);

            var shardPrefabNames = new List<string>();

            foreach (string shardColor in Enum.GetNames(typeof(ShardType))) {
                if (shardColor == "None") {
                    continue;
                }

                Enum.TryParse(shardColor, true, out ShardType color);

                foreach(ItemRarity rarity in ShardDefinitions.Get(color).Rarities) {
                    var prefab = UnityEngine.Object.Instantiate(genericPrefab);
                    string PrefabName = $"{shardColor}_{rarity}_ShardStone";
                    prefab.name = PrefabName;
                    ItemDrop pid = prefab.GetComponent<ItemDrop>();
                    pid.m_itemData.m_dropPrefab = prefab;
                    pid.m_itemData.m_shared.m_icons = new Sprite[] { EpicAssets.AssetBundle.LoadAsset<Sprite>($"Assets/EpicLoot/Sprites/Shardstones/{shardColor}.png") };
                    pid.m_itemData.m_shared.m_ammoType = $"{shardColor}|ShardStone";
                    pid.m_itemData.m_shared.m_maxStackSize = ShardStackSize;

                    // Bake this prefab's rarity into its MagicItem metadata (the semantic source of
                    // truth read by GetShardRarity/socketing). Rarity is part of the prefab identity now.
                    StampRarity(pid.m_itemData, rarity);
                    pid.Save();

                    // Include the rarity in the display name so each (color, rarity) prefab is a
                    // distinct name -- that is what keeps different rarities in separate inventory
                    // stacks (vanilla merges by name), e.g. "$mod_epicloot_Rare Red Shardstone".
                    ItemConfig ShardItemConfig = new ItemConfig() {
                        Name = $"{EpicLoot.GetRarityDisplayName(rarity)} $mod_epicloot_shard_{shardColor} $mod_epicloot_assets_shardstone",
                        Description = "$mod_epicloot_assets_shardstone_introduce",
                    };

                    CustomItem custom = new CustomItem(prefab, false, ShardItemConfig);
                    ItemManager.Instance.AddItem(custom);

                    shardPrefabNames.Add(PrefabName);
                }
            }

            // Enable items once things are working so that ZNet issues don't happen.
            // A single idempotent handler activates every registered prefab; a null
            // lookup logs and continues so one missing prefab can't leave the rest inactive.
            void EnableShardItems() {
                foreach (string prefabName in shardPrefabNames) {
                    GameObject prefab = PrefabManager.Instance.GetPrefab(prefabName);
                    if (prefab == null) {
                        EpicLoot.LogError($"Could not find shardstone prefab '{prefabName}' to activate.");
                        continue;
                    }

                    prefab.SetActive(true);
                    prefab.GetComponent<ItemDrop>().m_itemData.m_dropPrefab = prefab;
                }
            }

            ItemManager.OnItemsRegistered += EnableShardItems;
        }

    }

}
