using EpicLoot.CraftingV2;
using System;
using System.Collections.Generic;

namespace EpicLoot.ShardStones {
    // One rarity step of the ShardStone upgrade ladder. The cost is free-form: the source shard itself is
    // always consumed (SourceAmount of it), and Resources holds everything else the step charges.
    [Serializable]
    public class ShardStoneUpgradeStep {
        public ItemRarity From;
        public ItemRarity To;
        public int SourceAmount = 1;  // how many {color}_{From}_ShardStone the step consumes
        public int ProductAmount = 1; // how many {color}_{To}_ShardStone the step yields
        public List<MaterialConversionRequirement> Resources;
    }

    // Root of config/shardstoneconversions.json: the cost tables behind the generated ShardStone
    // rarity-upgrade recipes.
    //
    // Every collection here is deliberately left null rather than pre-initialized. Newtonsoft APPENDS to a
    // pre-initialized collection, so a non-empty default would merge with the player's JSON; and leaving
    // them null buys a distinction the ShardDefinition.Rarities pattern cannot express -- an ABSENT
    // property arrives as null and is backfilled with the shipped defaults, while an explicit [] or {}
    // arrives empty and is honoured as "the player turned this off".
    [Serializable]
    public class ShardStoneConversionsConfig {
        // The upgrade ladder, applied to every color. A step is only emitted for a color that can exist at
        // both its From and its To rarity (see ShardDefinition.Rarities).
        public List<ShardStoneUpgradeStep> UpgradeSteps;

        // Extra cost added to every step of every shard in the category. Defaults to two blank runestones
        // of the step's source rarity on Unique shards, which have no per-color gate of their own.
        public Dictionary<ShardCategory, List<MaterialConversionRequirement>> CategoryExtraResources;

        // Extra cost added to every step of one specific color. Defaults to one matching trophy per boss
        // shard, so leveling one gates on re-fighting (or having beaten) that boss.
        public Dictionary<ShardType, List<MaterialConversionRequirement>> ShardExtraResources;
    }

    // Generates the ShardStone rarity-upgrade recipes shown in the enchanting table's "Upgrade" tab.
    //
    // Only steps whose From and To are BOTH in the color's declared rarity set are emitted, so single-rarity
    // shards (e.g. boss shards) get no upgrade path. There is one recipe per color per valid step, built from
    // the ShardType enum + each color's rarity set rather than hand-authored, so it stays in sync as colors
    // or rarity sets change. The costs come from config/shardstoneconversions.json.
    //
    // Wired to MaterialConversions.OnSetupMaterialConversions so it re-runs after every config (re)load; it
    // first strips any previously-generated entries by name prefix, so it is idempotent.
    public static class ShardStoneConversions {
        private const string NamePrefix = "ShardStoneUpgrade_";

        // "{Rarity}" anywhere in a cost item name resolves to the rarity the step upgrades FROM, matching
        // the shipped ladder where each step is paid for in its own source rarity (Magic -> Rare costs
        // ShardMagic). Same token convention as loottables.json. It is what lets a single flat extras
        // entry express a per-rarity cost: "Runestone{Rarity}" charges RunestoneEpic on the Epic step and
        // RunestoneLegendary on the Legendary one, without needing a per-step config shape.
        private const string RarityToken = "{Rarity}";

        public static ShardStoneConversionsConfig Config;

        // Config setup hook (SychronizeConfig<ShardStoneConversionsConfig>). Backfills and sanitizes so the
        // generator never has to null-check or guard against nonsense amounts.
        public static void Initialize(ShardStoneConversionsConfig config) {
            Config = config ?? new ShardStoneConversionsConfig();

            Config.UpgradeSteps ??= DefaultUpgradeSteps();
            Config.CategoryExtraResources ??= DefaultCategoryExtraResources();
            Config.ShardExtraResources ??= DefaultShardExtraResources();

            Config.UpgradeSteps.RemoveAll(step => {
                if (step == null) {
                    return true;
                }
                if (step.From == step.To) {
                    EpicLoot.LogWarning($"ShardStoneConversions: upgrade step '{step.From}' -> '{step.To}' " +
                                        "goes nowhere and will be ignored.");
                    return true;
                }
                if (step.SourceAmount < 1) {
                    EpicLoot.LogWarning($"ShardStoneConversions: step '{step.From}' -> '{step.To}' has a " +
                                        $"SourceAmount of {step.SourceAmount}; clamping to 1.");
                    step.SourceAmount = 1;
                }
                if (step.ProductAmount < 1) {
                    EpicLoot.LogWarning($"ShardStoneConversions: step '{step.From}' -> '{step.To}' has a " +
                                        $"ProductAmount of {step.ProductAmount}; clamping to 1.");
                    step.ProductAmount = 1;
                }
                step.Resources ??= new List<MaterialConversionRequirement>();
                return false;
            });

            // The OnSetupMaterialConversions event only fires when materialconversions.json (re)loads, so
            // re-emit here too. This is what makes a live edit of this file, a config push from a dedicated
            // server, or an out-of-order RPC arrival actually take effect. No-ops until material conversions
            // have loaded, which covers the first-launch case where this config is read first.
            RegisterShardStoneUpgradeConversions();
        }

        public static ShardStoneConversionsConfig GetCFG() {
            return Config;
        }

        // The shipped ladder: 1 source shard plus an increasing pile of the matching classic enchanting
        // Shard. Kept in code as well as in the JSON so a missing or truncated config still produces the
        // intended progression.
        private static List<ShardStoneUpgradeStep> DefaultUpgradeSteps() {
            return new List<ShardStoneUpgradeStep> {
                MakeStep(ItemRarity.Magic,     ItemRarity.Rare,      "ShardMagic",     4),
                MakeStep(ItemRarity.Rare,      ItemRarity.Epic,      "ShardRare",      5),
                MakeStep(ItemRarity.Epic,      ItemRarity.Legendary, "ShardEpic",      6),
                MakeStep(ItemRarity.Legendary, ItemRarity.Mythic,    "ShardLegendary", 7),
            };
        }

        private static ShardStoneUpgradeStep MakeStep(ItemRarity from, ItemRarity to, string currency, int amount) {
            return new ShardStoneUpgradeStep {
                From = from,
                To = to,
                SourceAmount = 1,
                ProductAmount = 1,
                Resources = new List<MaterialConversionRequirement> {
                    new MaterialConversionRequirement { Item = currency, Amount = amount }
                }
            };
        }

        // Unique shards have no boss to gate them the way ShardExtraResources gates the boss shards, so
        // their surcharge is per-category and scales with the ladder instead: two blank runestones of the
        // rarity being upgraded from, on top of whatever the step itself charges.
        private static Dictionary<ShardCategory, List<MaterialConversionRequirement>> DefaultCategoryExtraResources() {
            return new Dictionary<ShardCategory, List<MaterialConversionRequirement>> {
                {
                    ShardCategory.Unique, new List<MaterialConversionRequirement> {
                        new MaterialConversionRequirement { Item = $"Runestone{RarityToken}", Amount = 2 }
                    }
                },
            };
        }

        // Values are the vanilla trophy prefab names.
        private static Dictionary<ShardType, List<MaterialConversionRequirement>> DefaultShardExtraResources() {
            return new Dictionary<ShardType, List<MaterialConversionRequirement>> {
                { ShardType.Eikthyr,  Trophy("TrophyEikthyr") },
                { ShardType.Elder,    Trophy("TrophyTheElder") },
                { ShardType.Bonemass, Trophy("TrophyBonemass") },
                { ShardType.Moder,    Trophy("TrophyDragonQueen") },
                { ShardType.Yagluth,  Trophy("TrophyGoblinKing") },
                { ShardType.Queen,    Trophy("TrophySeekerQueen") },
                { ShardType.Fader,    Trophy("TrophyFader") },
            };
        }

        private static List<MaterialConversionRequirement> Trophy(string prefab) {
            return new List<MaterialConversionRequirement> {
                new MaterialConversionRequirement { Item = prefab, Amount = 1 }
            };
        }

        public static void RegisterShardStoneUpgradeConversions() {
            var config = MaterialConversions.Config;
            if (config == null) {
                return;
            }

            // Initialize has not run yet if this fired from the material-conversions event first; fall back to
            // the shipped defaults rather than emitting no upgrade path at all. Initialize calls back into
            // here once Config is populated, so this returns rather than continuing.
            if (Config == null) {
                Initialize(null);
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

                Config.CategoryExtraResources.TryGetValue(def.Category, out var categoryExtras);
                Config.ShardExtraResources.TryGetValue(color, out var shardExtras);

                // Boss shards are expected to carry a per-color cost (their trophy). A Boss-category color
                // with no entry -- e.g. a boss added later without updating the config -- still gets a
                // working upgrade path, but it is almost always an omission, so say so.
                if (def.Category == ShardCategory.Boss && (shardExtras == null || shardExtras.Count == 0)) {
                    EpicLoot.LogWarning($"ShardStoneConversions: no ShardExtraResources entry for boss shard " +
                                        $"'{colorName}'; emitting its upgrade recipes without a trophy cost.");
                }

                foreach (var step in Config.UpgradeSteps) {
                    // Skip steps into/out of a rarity this shard can't exist at (e.g. single-rarity boss shards).
                    if (!rarities.Contains(step.From) || !rarities.Contains(step.To)) {
                        continue;
                    }

                    var resources = new List<MaterialConversionRequirement>();
                    AddResource(resources, $"{colorName}_{step.From}_ShardStone", step.SourceAmount);
                    AddResources(resources, step.Resources, step.From);
                    AddResources(resources, categoryExtras, step.From);
                    AddResources(resources, shardExtras, step.From);

                    config.MaterialConversions.Add(new MaterialConversion {
                        // The From rarity is part of the name because nothing stops a player authoring two
                        // steps that land on the same To rarity, and each recipe needs its own identity.
                        Name = $"{NamePrefix}{colorName}_{step.From}_to_{step.To}",
                        Product = $"{colorName}_{step.To}_ShardStone",
                        Amount = step.ProductAmount,
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

        private static void AddResources(List<MaterialConversionRequirement> target,
            List<MaterialConversionRequirement> source, ItemRarity stepRarity) {
            if (source == null) {
                return;
            }
            foreach (var requirement in source) {
                if (requirement != null) {
                    AddResource(target, ExpandRarityToken(requirement.Item, stepRarity), requirement.Amount);
                }
            }
        }

        private static string ExpandRarityToken(string item, ItemRarity rarity) {
            return string.IsNullOrEmpty(item) ? item : item.Replace(RarityToken, rarity.ToString());
        }

        // The step cost, the category extra and the shard extra are independent tables and may well name the
        // same item. The enchanting UI renders one cost row per requirement, so merge rather than letting the
        // same currency show up twice.
        private static void AddResource(List<MaterialConversionRequirement> target, string item, int amount) {
            if (string.IsNullOrEmpty(item) || amount <= 0) {
                return;
            }

            var existing = target.Find(r => r.Item == item);
            if (existing != null) {
                existing.Amount += amount;
                return;
            }

            target.Add(new MaterialConversionRequirement { Item = item, Amount = amount });
        }
    }
}
