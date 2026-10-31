using BepInEx;
using EpicLoot.Config;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EpicLoot.Magic
{
    /// <summary>
    /// Whether the equipment auto-add pass would change anything, kept in config/EpicLoot/autoadd-state.json
    /// so the pass is skipped when nothing it reads has changed since its last complete run: the game build,
    /// the installed mods (a rebuilt DLL counts), its five settings, the items, recipes and magic materials
    /// it finds in the game, and the text of the config files it reads and writes. The file hashes recorded
    /// are the ones the pass left behind, so a file a patch or Always Refresh Core Configs rebuilds every
    /// launch never matches, and those setups run the pass every time, as they must.
    /// </summary>
    internal static class AutoAddRunState
    {
        // Bump when the pass reads something new that the fingerprint must cover.
        private const int Format = 1;

        private static readonly string[] ConfigFiles =
            ["iteminfo.json", "loottables.json", "adventuredata.json", "itemsorter.json"];

        private static string FilePath => Path.Combine(Paths.ConfigPath, "EpicLoot", "autoadd-state.json");

        private class Record
        {
            public string Fingerprint;
        }

        /// <summary>
        /// Everything the pass reads from the game and the mod settings, as text. Worked out once per pass:
        /// none of it changes while the pass runs, unlike the config files.
        /// </summary>
        /// <param name="equipmentLines">One "name|type" line per equipment item the pass considers.</param>
        internal static string DescribeGameInputs(IEnumerable<string> equipmentLines, IEnumerable<string> magicMats,
            IEnumerable<string> selfKillingItems)
        {
            StringBuilder text = new StringBuilder();
            text.Append("format ").Append(Format).Append('\n');
            text.Append(InstalledMods.Describe());
            text.Append("settings ")
                .Append(ELConfig.AutoAddEquipment.Value).Append(',')
                .Append(ELConfig.AutoRemoveEquipmentNotFound.Value).Append(',')
                .Append(ELConfig.OnlyAddEquipmentWithRecipes.Value).Append(',')
                .Append(ELConfig.AutoAddRemoveEquipmentFromVendor.Value).Append(',')
                .Append(ELConfig.AutoAddRemoveEquipmentFromLootLists.Value).Append('\n');
            AppendSorted(text, "equipment", equipmentLines);
            AppendSorted(text, "magic materials", magicMats);
            AppendSorted(text, "self-killing", selfKillingItems);
            // Loot entries naming any other item are checked against ObjectDB.
            AppendSorted(text, "objectdb", ObjectDB.instance.m_items.Where(item => item != null).Select(item => item.name));
            AppendSorted(text, "recipes", ObjectDB.instance.m_recipes.Where(recipe => recipe != null).Select(DescribeRecipe));
            return text.ToString();
        }

        /// <summary>The game inputs plus the current text of every config file the pass reads or writes.</summary>
        internal static string Fingerprint(string gameInputs)
        {
            StringBuilder text = new StringBuilder(gameInputs);
            foreach (string file in ConfigFiles)
            {
                string path = Path.Combine(ELConfig.GetOverhaulDirectoryPath(), file);
                text.Append(file).Append(' ').Append(File.Exists(path) ? InstalledMods.Hash(File.ReadAllText(path)) : "missing")
                    .Append('\n');
            }

            return InstalledMods.Hash(text.ToString());
        }

        internal static bool Matches(string fingerprint)
        {
            try
            {
                return File.Exists(FilePath) &&
                    JsonConvert.DeserializeObject<Record>(File.ReadAllText(FilePath))?.Fingerprint == fingerprint;
            }
            catch (Exception e)
            {
                EpicLoot.Log($"Could not read {FilePath}, the equipment auto-add pass runs: {e.Message}");
                return false;
            }
        }

        internal static void Save(string fingerprint)
        {
            try
            {
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(new Record { Fingerprint = fingerprint }, Formatting.Indented));
            }
            catch (Exception e)
            {
                EpicLoot.Log($"Could not write {FilePath}: {e.Message}");
            }
        }

        // What DetermineBossLevelForItem and FindEnabledRecipe read from a recipe.
        private static string DescribeRecipe(Recipe recipe)
        {
            StringBuilder text = new StringBuilder();
            text.Append(recipe.m_item != null ? recipe.m_item.name : "").Append('|')
                .Append(recipe.m_enabled).Append('|')
                .Append(recipe.m_craftingStation != null ? recipe.m_craftingStation.name : "").Append('|');
            foreach (Piece.Requirement requirement in recipe.m_resources ?? Array.Empty<Piece.Requirement>())
            {
                text.Append(requirement?.m_resItem != null ? requirement.m_resItem.name : "").Append(',');
            }

            return text.ToString();
        }

        private static void AppendSorted(StringBuilder text, string heading, IEnumerable<string> lines)
        {
            text.Append(heading).Append('\n');
            foreach (string line in lines.OrderBy(line => line, StringComparer.Ordinal))
            {
                text.Append(line).Append('\n');
            }
        }
    }
}
