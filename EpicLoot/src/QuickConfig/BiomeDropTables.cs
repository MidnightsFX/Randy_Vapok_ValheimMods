using EpicLoot.Biomes;
using EpicLoot.Magic;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace EpicLoot.QuickConfig;

/// <summary>
/// One editable drop target of loottables.json: a tier template or boss at one creature level, or a
/// chest. AmountText is the count:weight table (Drops), RarityText the per-rarity weights the target's
/// Loot entries carry. Identity is Object + Occurrence (a boss's item table and shard table share the
/// Object name) + Level.
/// </summary>
internal sealed class BiomeDropRow {
    internal string Biome = BiomeDropTables.Other;
    internal string Object = "";
    internal int Occurrence;
    internal int? Level;
    internal string Label = "";
    internal string AmountText = "";
    internal string RarityText = "";
    /// <summary>True when the target's Loot entries carry different Rarity arrays; the first is shown.</summary>
    internal bool Mixed;
    /// <summary>Length of the first Rarity array in the file (5 or 6), so a write can keep a 5-wide file 5-wide.</summary>
    internal int RarityLength;

    internal string Id => $"{Object}#{Occurrence}@{(Level.HasValue ? Level.Value.ToString(CultureInfo.InvariantCulture) : "-")}";

    internal BiomeDropRow Clone() => new BiomeDropRow {
        Biome = Biome, Object = Object, Occurrence = Occurrence, Level = Level, Label = Label,
        AmountText = AmountText, RarityText = RarityText, Mixed = Mixed, RarityLength = RarityLength
    };

    internal static bool Same(BiomeDropRow a, BiomeDropRow b) {
        return a.Id == b.Id && a.AmountText == b.AmountText && a.RarityText == b.RarityText;
    }
}

/// <summary>
/// Reads loottables.json's LootTables into BiomeDropRows grouped by biome, and the text forms the rows
/// use. Grouping: tier templates through itemsorter.json's BiomeSorterData (a biome owns Tier{N}Mob and
/// Tier{N}EliteMob for its Tier), bosses through the biome registry's boss keys, chests through a name
/// table; anything else lands under "Other" so nothing is hidden.
/// </summary>
internal static class BiomeDropTables {
    internal const string Other = "Other";
    private const int LabelCreatureNames = 4;

    private static readonly Regex TierTemplate = new Regex(@"^Tier(\d+)(Elite)?Mob$", RegexOptions.Compiled);

    // Explicit chest names first (lowercase substrings); after these, TreasureMapChest_<Biome> and any
    // registry biome name found inside the object name.
    private static readonly (string Needle, string Biome)[] ChestBiomes = {
        ("forestcrypt", "BlackForest"), ("fcrypt", "BlackForest"), ("trollcave", "BlackForest"), ("shipwreck_karve_chest", "BlackForest"),
        ("sunkencrypt", "Swamp"), ("mountaincave", "Mountain"), ("heath", "Plains"), ("plains_stone", "Plains"),
        ("dvergr", "Mistlands"), ("charredfortress", "AshLands"), ("ashland_stone", "AshLands"),
        ("deepnorth_village", "DeepNorth"), ("memorial_buried", "DeepNorth"), ("morkhalla_", "DeepNorth")
    };

    // ------------------------------------------------------------------------------------------------
    //  Reading
    // ------------------------------------------------------------------------------------------------

    /// <summary>The rows for the live loot config, grouped by biome in progression order then Other; null when not loaded.</summary>
    internal static List<BiomeDropRow> Read() {
        LootTable[] tables = LootRoller.Config?.LootTables;
        if (tables == null) { return null; }

        // Creatures that reference a template give the template its label.
        Dictionary<string, List<string>> references = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (LootTable table in tables) {
            if (table == null || string.IsNullOrEmpty(table.Object) || string.IsNullOrEmpty(table.RefObject)) { continue; }
            if (references.TryGetValue(table.RefObject, out List<string> list) == false) {
                list = new List<string>();
                references[table.RefObject] = list;
            }
            list.Add(table.Object);
        }

        List<BiomeDropRow> rows = new List<BiomeDropRow>();
        Dictionary<string, int> occurrences = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (LootTable table in tables) {
            if (table == null || string.IsNullOrEmpty(table.Object) || string.IsNullOrEmpty(table.RefObject) == false) { continue; }
            bool leveled = table.LeveledLoot != null && table.LeveledLoot.Count > 0;
            if (leveled == false && table.Drops == null) { continue; }

            occurrences.TryGetValue(table.Object, out int occurrence);
            occurrences[table.Object] = occurrence + 1;
            string biome = GroupOf(table.Object);
            string suffix = OccurrenceSuffix(table, occurrence);
            references.TryGetValue(table.Object, out List<string> creatures);

            if (leveled) {
                foreach (LeveledLootDef def in table.LeveledLoot) {
                    if (def == null) { continue; }
                    rows.Add(MakeRow(biome, table.Object, occurrence, def.Level, def.Drops, def.Loot,
                        $"{table.Object} · level {def.Level}{suffix}{CreatureList(creatures)}"));
                }
            } else {
                rows.Add(MakeRow(biome, table.Object, occurrence, null, table.Drops, table.Loot, table.Object + suffix));
            }
        }

        List<string> order = GroupOrder();
        return rows
            .Select((row, index) => (Row: row, Index: index))
            .OrderBy(pair => GroupIndex(order, pair.Row.Biome))
            .ThenBy(pair => pair.Index)
            .Select(pair => pair.Row)
            .ToList();
    }

    private static BiomeDropRow MakeRow(string biome, string obj, int occurrence, int? level, float[][] drops, LootDrop[] loot, string label) {
        BiomeDropRow row = new BiomeDropRow {
            Biome = biome, Object = obj, Occurrence = occurrence, Level = level, Label = label,
            AmountText = drops != null ? QuickConfigBindings.FormatRarityTable(drops) : ""
        };
        float[] first = null;
        if (loot != null) {
            foreach (LootDrop entry in loot) {
                if (entry?.Rarity == null) { continue; }
                if (first == null) {
                    first = entry.Rarity;
                    row.RarityLength = entry.Rarity.Length;
                } else if (Padded(entry.Rarity).SequenceEqual(Padded(first)) == false) {
                    row.Mixed = true;
                }
            }
        }
        row.RarityText = first != null ? FormatRarity(first) : "";
        return row;
    }

    // A boss's second table with the same Object name is its shard table; anything else is numbered.
    private static string OccurrenceSuffix(LootTable table, int occurrence) {
        if (occurrence == 0) { return ""; }
        IEnumerable<LootDrop> loot = table.LeveledLoot != null && table.LeveledLoot.Count > 0
            ? table.LeveledLoot.Where(def => def?.Loot != null).SelectMany(def => def.Loot)
            : table.Loot ?? Array.Empty<LootDrop>();
        List<LootDrop> entries = loot.Where(entry => entry != null).ToList();
        bool shards = entries.Count > 0 && entries.All(entry => (entry.Item ?? "").IndexOf("Shard", StringComparison.OrdinalIgnoreCase) >= 0);
        return shards ? " (shards)" : $" #{occurrence + 1}";
    }

    private static string CreatureList(List<string> creatures) {
        if (creatures == null || creatures.Count == 0) { return ""; }
        string names = string.Join(", ", creatures.Take(LabelCreatureNames));
        if (creatures.Count > LabelCreatureNames) { names += $", +{creatures.Count - LabelCreatureNames} more"; }
        return $" ({names})";
    }

    // ------------------------------------------------------------------------------------------------
    //  Grouping
    // ------------------------------------------------------------------------------------------------

    /// <summary>Registry biome names in progression order, then Other.</summary>
    internal static List<string> GroupOrder() {
        List<string> order = new List<string>();
        try {
            foreach (BiomeDefinition definition in BiomeDataManager.BiomesInOrder) {
                if (string.IsNullOrEmpty(definition?.Name) == false && order.Contains(definition.Name) == false) { order.Add(definition.Name); }
            }
        } catch (Exception) {
            // Registry not loaded: everything groups under Other.
        }
        order.Add(Other);
        return order;
    }

    private static int GroupIndex(List<string> order, string biome) {
        int index = order.IndexOf(biome);
        return index >= 0 ? index : order.Count;
    }

    internal static string GroupDisplayName(string group) {
        if (string.IsNullOrEmpty(group)) { return ""; }
        return group == Other ? Other : QuickConfigBindings.BiomeDisplayName(group);
    }

    private static string GroupOf(string obj) {
        string biome = TemplateBiome(obj) ?? BossBiome(obj) ?? ChestBiome(obj);
        return string.IsNullOrEmpty(biome) ? Other : biome;
    }

    // Tier{N}Mob / Tier{N}EliteMob belong to the biome whose sorter entry names that tier.
    private static string TemplateBiome(string obj) {
        Match match = TierTemplate.Match(obj);
        if (match.Success == false) { return null; }
        string tier = "Tier" + match.Groups[1].Value;
        Dictionary<string, AutoAddEnchantableItems.SortingData> sorter = AutoAddEnchantableItems.Config?.BiomeSorterData;
        if (sorter == null) { return null; }
        foreach (KeyValuePair<string, AutoAddEnchantableItems.SortingData> pair in sorter) {
            if (pair.Value == null || string.Equals(pair.Value.Tier, tier, StringComparison.OrdinalIgnoreCase) == false) { continue; }
            return RegistryName(pair.Key);
        }
        return null;
    }

    // "defeated_gdking" and "gd_king" agree once the prefix, underscores and case are dropped.
    private static string BossBiome(string obj) {
        string normalizedObject = NormalizeKey(obj);
        if (normalizedObject.Length < 4) { return null; }
        try {
            foreach (string bossKey in BiomeDataManager.BossKeysInOrder) {
                string normalizedKey = NormalizeKey(bossKey);
                if (normalizedKey.Length < 4) { continue; }
                if (normalizedKey != normalizedObject && normalizedKey.Contains(normalizedObject) == false
                    && normalizedObject.Contains(normalizedKey) == false) { continue; }
                Heightmap.Biome biome = BiomeDataManager.GetFirstBiomeForBossKey(bossKey);
                if (biome != Heightmap.Biome.None && BiomeDataManager.IsKnown(biome)) { return BiomeDataManager.GetName(biome); }
            }
        } catch (Exception) {
            // Registry not loaded.
        }
        return null;
    }

    private static string NormalizeKey(string value) {
        string lower = (value ?? "").ToLowerInvariant();
        if (lower.StartsWith("defeated_", StringComparison.Ordinal)) { lower = lower.Substring("defeated_".Length); }
        return new string(lower.Where(char.IsLetterOrDigit).ToArray());
    }

    private static string ChestBiome(string obj) {
        string lower = obj.ToLowerInvariant();
        foreach ((string needle, string biome) in ChestBiomes) {
            if (lower.Contains(needle)) { return RegistryName(biome) ?? biome; }
        }
        const string mapChest = "TreasureMapChest_";
        if (obj.StartsWith(mapChest, StringComparison.OrdinalIgnoreCase)) {
            string name = obj.Substring(mapChest.Length);
            return RegistryName(name) ?? name;
        }
        try {
            foreach (BiomeDefinition definition in BiomeDataManager.BiomesInOrder) {
                if (string.IsNullOrEmpty(definition?.Name) == false && lower.Contains(definition.Name.ToLowerInvariant())) { return definition.Name; }
            }
        } catch (Exception) {
            // Registry not loaded.
        }
        return null;
    }

    // The registry's spelling of a biome name, or null when the registry does not know it.
    private static string RegistryName(string name) {
        try {
            if (BiomeDataManager.TryResolve(name, out Heightmap.Biome biome) && biome != Heightmap.Biome.None && BiomeDataManager.IsKnown(biome)) {
                return BiomeDataManager.GetName(biome);
            }
        } catch (Exception) {
            // Registry not loaded.
        }
        return null;
    }

    // ------------------------------------------------------------------------------------------------
    //  Rarity weights text form
    // ------------------------------------------------------------------------------------------------

    private static float[] Padded(float[] rarity) {
        float[] padded = new float[Rarities.Count];
        for (int i = 0; i < padded.Length && i < rarity.Length; i++) { padded[i] = rarity[i]; }
        return padded;
    }

    /// <summary>"75, 25, 0, 0, 0, 0": one weight per rarity, padded to Rarities.Count.</summary>
    internal static string FormatRarity(float[] rarity) {
        return string.Join(", ", Padded(rarity).Select(weight => weight.ToString("0.###", CultureInfo.InvariantCulture)));
    }

    /// <summary>Parses comma-separated weights (at most one per rarity, padded with zeros), each 0 or more, at least one above 0.</summary>
    internal static bool ParseRarity(string text, out float[] weights, out string error) {
        weights = null;
        error = null;
        if (string.IsNullOrWhiteSpace(text)) {
            error = "enter one weight per rarity, for example 75, 25, 0, 0, 0, 0.";
            return false;
        }
        string[] parts = text.Split(',');
        if (parts.Length > Rarities.Count) {
            error = $"at most {Rarities.Count} weights (one per rarity, Magic to {Rarities.Highest}).";
            return false;
        }
        float[] parsed = new float[Rarities.Count];
        bool any = false;
        for (int i = 0; i < parts.Length; i++) {
            string part = parts[i].Trim();
            if (part.Length == 0) { continue; }
            if (float.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out float weight) == false) {
                error = $"'{part}' is not a number.";
                return false;
            }
            if (weight < 0f) {
                error = $"weight {part} must be 0 or more.";
                return false;
            }
            parsed[i] = weight;
            any |= weight > 0f;
        }
        if (any == false) {
            error = "at least one rarity weight must be above 0.";
            return false;
        }
        weights = parsed;
        return true;
    }

    /// <summary>How many weights to write: the file's own width when it was narrower and the extra weights are all 0.</summary>
    internal static int WriteLength(float[] weights, int fileLength) {
        if (fileLength <= 0 || fileLength >= weights.Length) { return weights.Length; }
        for (int i = fileLength; i < weights.Length; i++) {
            if (weights[i] != 0f) { return weights.Length; }
        }
        return fileLength;
    }
}
