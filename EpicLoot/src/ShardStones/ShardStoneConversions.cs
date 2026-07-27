using EpicLoot.CraftingV2;
using System;
using System.Collections.Generic;

namespace EpicLoot.ShardStones {
    // Generates the ShardStone rarity-upgrade recipes shown in the enchanting table's "Upgrade" tab.
    //
    // Each (color, rarity) is now a distinct prefab ({color}_{rarity}_ShardStone) whose baked metadata
    // carries its rarity. So each step is a prefab-to-prefab conversion that:
    //   - REQUIRES 1 {color}_{From}_ShardStone plus N of that step's enchanting Shard, and
    //   - PRODUCES 1 {color}_{To}_ShardStone.
    // No quality stamping is needed -- cloning the correct per-rarity prefab already yields the right rarity.
    //
    // Only steps whose From and To are BOTH in the color's declared rarity set are emitted, so single-rarity
    // shards (e.g. boss shards) get no upgrade path. There is one recipe per color per valid step, built from
    // the ShardType enum + each color's rarity set rather than hand-authored, so it stays in sync as colors
    // or rarity sets change.
    //
    // Wired to MaterialConversions.OnSetupMaterialConversions so it re-runs after every config (re)load; it
    // first strips any previously-generated entries by name prefix, so it is idempotent.
    public static class ShardStoneConversions {
        private const string NamePrefix = "ShardStoneUpgrade_";

        private struct UpgradeStep {
            public ItemRarity From;
            public ItemRarity To;
            public string Currency; // the classic enchanting Shard consumed for this step
            public int Amount;
        }

        private static readonly UpgradeStep[] Steps = {
            new UpgradeStep { From = ItemRarity.Magic,     To = ItemRarity.Rare,      Currency = "ShardMagic",     Amount = 4 },
            new UpgradeStep { From = ItemRarity.Rare,      To = ItemRarity.Epic,      Currency = "ShardRare",      Amount = 5 },
            new UpgradeStep { From = ItemRarity.Epic,      To = ItemRarity.Legendary, Currency = "ShardEpic",      Amount = 6 },
            new UpgradeStep { From = ItemRarity.Legendary, To = ItemRarity.Mythic,    Currency = "ShardLegendary", Amount = 7 },
        };

        // Boss shardstones additionally cost 1 trophy of the matching boss per upgrade step, so leveling one
        // up gates on re-fighting (or having beaten) that boss. Values are the vanilla trophy prefab names.
        private static readonly Dictionary<ShardType, string> BossTrophies = new Dictionary<ShardType, string> {
            { ShardType.Eikthyr,  "TrophyEikthyr" },
            { ShardType.Elder,    "TrophyTheElder" },
            { ShardType.Bonemass, "TrophyBonemass" },
            { ShardType.Moder,    "TrophyDragonQueen" },
            { ShardType.Yagluth,  "TrophyGoblinKing" },
            { ShardType.Queen,    "TrophySeekerQueen" },
            { ShardType.Fader,    "TrophyFader" },
        };

        public static void RegisterShardStoneUpgradeConversions() {
            var config = MaterialConversions.Config;
            if (config == null) {
                return;
            }

            config.MaterialConversions.RemoveAll(c => c.Name != null && c.Name.StartsWith(NamePrefix));

            foreach (string colorName in Enum.GetNames(typeof(ShardType))) {
                if (colorName == "None" || !Enum.TryParse(colorName, out ShardType color)) {
                    continue;
                }

                var def = Shards.ShardDefinitions.Get(color);
                var rarities = def?.Rarities;
                if (rarities == null) {
                    continue;
                }

                // Boss shards charge an extra trophy per step. A Boss-category color with no mapped trophy
                // (e.g. a boss added later without updating BossTrophies) still gets a working upgrade path.
                string bossTrophy = null;
                if (def.Category == ShardCategory.Boss) {
                    if (!BossTrophies.TryGetValue(color, out bossTrophy)) {
                        EpicLoot.LogWarning($"ShardStoneConversions: no boss trophy mapped for '{colorName}'; " +
                                            "emitting its upgrade recipes without a trophy cost.");
                    }
                }

                foreach (var step in Steps) {
                    // Skip steps into/out of a rarity this shard can't exist at (e.g. single-rarity boss shards).
                    if (!rarities.Contains(step.From) || !rarities.Contains(step.To)) {
                        continue;
                    }

                    var resources = new List<MaterialConversionRequirement> {
                        new MaterialConversionRequirement { Item = $"{colorName}_{step.From}_ShardStone", Amount = 1 },
                        new MaterialConversionRequirement { Item = step.Currency, Amount = step.Amount },
                    };
                    if (bossTrophy != null) {
                        resources.Add(new MaterialConversionRequirement { Item = bossTrophy, Amount = 1 });
                    }

                    config.MaterialConversions.Add(new MaterialConversion {
                        Name = $"{NamePrefix}{colorName}_{step.To}",
                        Product = $"{colorName}_{step.To}_ShardStone",
                        Amount = 1,
                        Type = MaterialConversionType.Upgrade,
                        Resources = resources
                    });
                }
            }

            // Rebuild the live lookup so the defensive (post-load) call path takes effect immediately. When
            // invoked from within Initialize this is redundant with Initialize's own rebuild, but harmless.
            MaterialConversions.Conversions.Clear();
            foreach (var entry in config.MaterialConversions) {
                MaterialConversions.Conversions.Add(entry.Type, entry);
            }
        }
    }
}
