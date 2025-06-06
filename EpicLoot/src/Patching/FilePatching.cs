﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using Common;
using EpicLoot.Abilities;
using EpicLoot.Adventure;
using EpicLoot.Config;
using EpicLoot.Crafting;
using EpicLoot.CraftingV2;
using EpicLoot.GatedItemType;
using EpicLoot.LegendarySystem;
using EpicLoot_UnityLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace EpicLoot.Patching
{
    [Serializable]
    public enum PatchAction
    {
        None,           // Do nothing
        Add,            // Add the provided value to the selected object with the provided property name, if the property already exists, it's value is overwritten
        Overwrite,      // Replace the selected token's value with the provided value
        Remove,         // Remove the selected token from the array or object
        Append,         // Append the provided value to the end of the selected array
        AppendAll,      // Append the provided array to the end of the selected array.
        InsertBefore,   // Insert the provided value into the array containing the selected token, before the token
        InsertAfter,    // Insert the provided value into the array containing the selected token, after the token
        RemoveAll,      // Remove all elements of an array or all properties of an object
        Merge,          // Use property values in the provided object to add or overwrite property values on the selected object
        MultiAdd,       // Add the provided value to all defined values in MultiPropertyName
    }

    [Serializable]
    public class Patch
    {
        public int Priority = -1;
        public string Author = "";
        public string SourceFile = "";
        public string TargetFile = "";
        public string Path = "";
        public PatchAction Action = PatchAction.None;
        public bool Require;
        public string PropertyName = "";
        public JToken Value = null;
        public string[] MultiPropertyName = null;
    }

    [Serializable]
    public class PatchFile
    {
        public int Priority = 500;
        public string TargetFile = "";
        public string Author = "";
        public bool RequireAll = false;
        public List<Patch> Patches;
    }

    public static class FilePatching
    {
        public static string PatchesDirPath = GetPatchesDirectoryPath();
        public static List<string> ConfigFileNames = new List<string>();
        public static MultiValueDictionary<string, Patch> PatchesPerFile = new MultiValueDictionary<string, Patch>();

        public static void LoadAllPatches()
        {
            try
            {
                PatchesDirPath = GetPatchesDirectoryPath();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Unable to Get Patch Directory: {e.Message}");
                var debugPath = GetPatchesDirectoryPath(true);
                Debug.LogWarning($"Attempted path is [{debugPath}]");
                return;
            }

            try
            {
                // If the folder does not exist, there are no patches
                if (string.IsNullOrEmpty(PatchesDirPath))
                    return;

                var patchesFolder = new DirectoryInfo(PatchesDirPath);
                if (!patchesFolder.Exists)
                    return;

                var pluginFolder = new DirectoryInfo(Assembly.GetExecutingAssembly().Location);

                CheckForOldPatches(pluginFolder.Parent);
                ConfigFileNames = EpicLoot.GetEmbeddedResourceNamesFromDirectory();
                ProcessPatchDirectory(patchesFolder);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Unable to Get Patch Directory: {e.Message}");
                var debugPath = GetPatchesDirectoryPath(true);
                Debug.LogWarning($"Attempted PatchesDirPath is [{PatchesDirPath}]");
                Debug.LogWarning($"Attempted debugPath is [{debugPath}]");
            }

            ApplyAllPatches();
        }

        public static void CheckForOldPatches(DirectoryInfo pluginFolder)
        {
            var oldPatchFolder = Path.Combine(pluginFolder.FullName, "patches");
            
            if (Directory.Exists(oldPatchFolder))
            {
                if (Directory.GetFiles(oldPatchFolder, "*.json", SearchOption.AllDirectories).Length > 0)
                {
                    EpicLoot.LogWarningForce($"***************************************************");
                    EpicLoot.LogWarningForce($"Epic Loot Patch Folder Has Moved To:");
                    EpicLoot.LogWarningForce($"{PatchesDirPath}");
                    EpicLoot.LogWarningForce($"Please move your patch files. Patch files found in this folder will not be loaded");
                    EpicLoot.LogWarningForce($"***************************************************");
                }
            }
        }

        public static void RemoveFilePatches(string fileName, string patchFile)
        {
            PatchesPerFile.GetValues(fileName, true).RemoveAll(y => y.SourceFile.Equals(patchFile));
        }

        public static void ProcessPatchDirectory(DirectoryInfo dir)
        {
            FileInfo[] files = null;
            try
            {
                files = dir.GetFiles("*.json");
            }
            catch (Exception e)
            {
                EpicLoot.LogError($"Error parsing patch directory ({dir.Name}): {e.Message}");
            }

            if (files != null)
            {
                foreach (var file in files)
                {
                    ProcessPatchFile(file);
                }
            }

            var subDirs = dir.GetDirectories();
            foreach (var subDir in subDirs)
            {
                ProcessPatchDirectory(subDir);
            }
        }

        public static List<string> ProcessPatchFile(FileInfo file)
        {
            var defaultTargetFile = "";
            if (ConfigFileNames.Contains(file.Name))
                defaultTargetFile = file.Name;

            PatchFile patchFile = null;
            try
            {
                patchFile = JsonConvert.DeserializeObject<PatchFile>(File.ReadAllText(file.FullName));
            }
            catch (Exception e)
            {
                EpicLoot.LogErrorForce($"Error parsing patch file ({file.Name})! Error: {e.Message}");
                return null;
            }

            if (patchFile == null)
            {
                EpicLoot.LogErrorForce($"Error parsing patch file ({file.Name})! Error: unknown!");
                return null;
            }

            if (!string.IsNullOrEmpty(patchFile.TargetFile) && !string.IsNullOrEmpty(defaultTargetFile) && 
                patchFile.TargetFile != defaultTargetFile)
            {
                EpicLoot.LogWarningForce($"TargetFile ({patchFile.TargetFile}) specified in patch file ({file.Name}) " +
                    $"does not match! If patch file name matches a config file name, TargetFile is unnecessary.");
            }

            if (!string.IsNullOrEmpty(patchFile.TargetFile))
                defaultTargetFile = patchFile.TargetFile;

            if (!string.IsNullOrEmpty(defaultTargetFile) && !ConfigFileNames.Contains(defaultTargetFile))
            {
                EpicLoot.LogErrorForce($"TargetFile ({defaultTargetFile}) specified in patch file ({file.Name}) " +
                    $"does not exist! {file.Name} will not be processed.");
                return null;
            }

            var requiresSpecifiedSourceFile = string.IsNullOrEmpty(defaultTargetFile);

            var author = string.IsNullOrEmpty(patchFile.Author) ? "<author>" : patchFile.Author;
            var requireAll = patchFile.RequireAll;
            var defaultPriority = patchFile.Priority;
            List<string> files_with_new_patches = new List<string>();

            foreach(var patch in patchFile.Patches)
            {
                EpicLoot.Log($"Patch: ({file.Name})\n  > Action: {patch.Action}\n  > " +
                    $"Path: {patch.Path}\n  > Value: {patch.Value}");

                patch.Require = requireAll || patch.Require;
                if (string.IsNullOrEmpty(patch.Author))
                    patch.Author = author;
                if (string.IsNullOrEmpty(patch.TargetFile))
                {
                    if (requiresSpecifiedSourceFile)
                    {
                        EpicLoot.LogErrorForce($"Patch in file ({file.Name}) " +
                            $"requires a specified TargetFile!");
                        continue;
                    }

                    patch.TargetFile = defaultTargetFile;
                }
                else if (!ConfigFileNames.Contains(patch.TargetFile))
                {
                    EpicLoot.LogErrorForce($"Patch in file ({file.Name}) " +
                        $"has unknown specified source file ({patch.TargetFile})!");
                    continue;
                }
                
                if (patch.Priority < 0)
                    patch.Priority = defaultPriority;

                patch.SourceFile = file.Name;
                EpicLoot.Log($"Adding Patch from {patch.SourceFile} to file {patch.TargetFile} with {patch.Path}");
                PatchesPerFile.Add(patch.TargetFile, patch);
                // each patch section can add a different file, but we only need to actually refresh the file once.
                if (files_with_new_patches.Contains(patch.TargetFile) == false)
                {
                    files_with_new_patches.Add(patch.TargetFile);
                }
            }
            return files_with_new_patches;
        }

        public static string GetPatchesDirectoryPath(bool debug = false)
        {
            var patchesFolderPath = Path.Combine(Paths.ConfigPath, "EpicLoot", "patches");
            
            if (debug)
                return patchesFolderPath;
            
            var dirInfo = Directory.CreateDirectory(patchesFolderPath);

            return dirInfo.FullName;
        }

        public static string BuildPatchedConfig(string targetfile, JObject source_file_json)
        {
            var patches = PatchesPerFile.GetValues(targetfile, true).OrderByDescending(x => x.Priority).ToList();
            if (patches.Count == 0) {
                return null;
            }

            foreach (var patch in patches)
            {
                ApplyPatch(source_file_json, patch);
            }

            var output = source_file_json.ToString(ELConfig.OutputPatchedConfigFiles.Value ? Formatting.Indented : Formatting.None);
            return output;
        }

        // This is only called on startup, and will modify all base classes that have patches loaded locally
        public static void ApplyAllPatches()
        {
            foreach (var entry in PatchesPerFile)
            {
                LoadPatchedJSON(entry.Key);
            }
        }

        public static void ApplyPatchesToSpecificFilesWithNetworkUpdates(List<string> files_with_patch_updates)
        {
            // skip update if there are no changes, this should never happen
            if (files_with_patch_updates.Count == 0) return;

            EpicLoot.Log($"Applying {files_with_patch_updates.Count} patched files");
            foreach(string file in files_with_patch_updates)
            {
                EpicLoot.Log($"Applying patchfile: {file}");
                LoadPatchedJSON(file, true);
            }
            // Once the update has been provided for these files, they dont need updates again unless something changes
            files_with_patch_updates.Clear();
        }

        internal static void LoadPatchedJSON(string patch_filename, bool network_updates = false)
        {
            var base_json_string = JObject.Parse(EpicLoot.ReadEmbeddedResourceFile("EpicLoot.config." + patch_filename));
            var patched_json = BuildPatchedConfig(patch_filename, base_json_string);

            // We don't want to do network updates on startup
            switch (patch_filename)
            {
                case "loottables.json":
                    LootRoller.Initialize(JsonConvert.DeserializeObject<LootConfig>(patched_json));
                    if (network_updates) { ELConfig.LootConfigSendConfigs(); }
                    break;
                case "magiceffects.json":
                    MagicItemEffectDefinitions.Initialize(JsonConvert.DeserializeObject<MagicItemEffectsList>(patched_json));
                    if (network_updates) { ELConfig.MagicEffectsSendConfigs(); }
                    break;
                case "iteminfo.json":
                    GatedItemTypeHelper.Initialize(JsonConvert.DeserializeObject<ItemInfoConfig>(patched_json));
                    if (network_updates) { ELConfig.ItemInfoConfigSendConfigs(); }
                    break;
                case "recipes.json":
                    RecipesHelper.Initialize(JsonConvert.DeserializeObject<RecipesConfig>(patched_json));
                    if (network_updates) { ELConfig.RecipesConfigSendConfigs(); }
                    break;
                case "enchantcosts.json":
                    EnchantCostsHelper.Initialize(JsonConvert.DeserializeObject<EnchantingCostsConfig>(patched_json));
                    if (network_updates) { ELConfig.EnchantCostConfigSendConfigs(); }
                    break;
                case "itemnames.json":
                    MagicItemNames.Initialize(JsonConvert.DeserializeObject<ItemNameConfig>(patched_json));
                    if (network_updates) { ELConfig.MagicItemNamesSendConfigs(); }
                    break;
                case "adventuredata.json":
                    AdventureDataManager.Initialize(JsonConvert.DeserializeObject<AdventureDataConfig>(patched_json));
                    if (network_updates) { ELConfig.AdventureDataSendConfigs(); }
                    break;
                case "legendaries.json":
                    UniqueLegendaryHelper.Initialize(JsonConvert.DeserializeObject<LegendaryItemConfig>(patched_json));
                    if (network_updates) { ELConfig.LegendarySendConfigs(); }
                    break;
                case "abilities.json":
                    AbilityDefinitions.Initialize(JsonConvert.DeserializeObject<AbilityConfig>(patched_json));
                    if (network_updates) { ELConfig.AbilitiesSendConfigs(); }
                    break;
                case "materialconversions.json":
                    MaterialConversions.Initialize(JsonConvert.DeserializeObject<MaterialConversionsConfig>(patched_json));
                    if (network_updates) { ELConfig.MaterialConversionSendConfigs(); }
                    break;
                case "enchantingupgrades.json":
                    EnchantingTableUpgrades.InitializeConfig(JsonConvert.DeserializeObject<EnchantingUpgradesConfig>(patched_json));
                    if (network_updates) { ELConfig.EnchantingTableUpgradeSendConfigs(); }
                    break;
            }
        }

        public static void ApplyPatch(JObject json, Patch patch)
        {
            var selectedTokens = json.SelectTokens(patch.Path).ToList();
            if (patch.Require && selectedTokens.Count == 0)
            {
                EpicLoot.LogErrorForce($"Required Patch ({patch.SourceFile}) path ({patch.Path}) failed to select any tokens in target file ({patch.TargetFile})!");
                return;
            }

            foreach (var token in selectedTokens)
            {
                switch (patch.Action)
                {
                    case PatchAction.Add: ApplyPatch_Add(token, patch); break;
                    case PatchAction.Overwrite: ApplyPatch_Overwrite(token, patch); break;
                    case PatchAction.Remove: ApplyPatch_Remove(token, patch); break;
                    case PatchAction.Append: ApplyPatch_Append(token, patch); break;
                    case PatchAction.AppendAll: ApplyPatch_Append(token, patch, true); break;
                    case PatchAction.InsertBefore: ApplyPatch_Insert(token, patch, false); break;
                    case PatchAction.InsertAfter: ApplyPatch_Insert(token, patch, true); break;
                    case PatchAction.RemoveAll: ApplyPatch_RemoveAll(token, patch); break;
                    case PatchAction.Merge: ApplyPatch_Merge(token, patch); break;
                    case PatchAction.MultiAdd: ApplyPatch_MultiAdd(token, patch); break;
                    default: break;
                }
            }
        }

        public static void ApplyPatch_MultiAdd(JToken token, Patch patch)
        {
            if (patch.Value == null) {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'MultiAdd' but has not supplied a Value for the added value! This patch will be ignored!");
                return;
            }
            int index = 0;
            foreach (var item in patch.MultiPropertyName) {
                Patch_Add(token, item, patch.Value);
                index ++;
            }
        }

        public static void ApplyPatch_Add(JToken token, Patch patch)
        {
            if (patch.Value == null)
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'Add' but has not supplied a json Value! This patch will be ignored!");
                return;
            }
            if (string.IsNullOrEmpty(patch.PropertyName))
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'Add' but has not supplied a PropertyName for the added value! This patch will be ignored!");
                return;
            }

            if (token.Type == JTokenType.Object)
            {
                Patch_Add(token, patch.PropertyName, patch.Value);
            }
            else
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'Add' but has selected a token that is not a json Object! This patch will be ignored!");
            }
        }

        internal static void Patch_Add(JToken token, string property, JToken value)
        {
            var jObject = ((JObject)token);
            if (jObject.ContainsKey(property) && jObject.Property(property) is JProperty jProperty) {
                EpicLoot.LogWarningForce($"Patch has action 'Add' but a property with the name ({property}) already exists! The property's value will be overwritten");
                jProperty.Value = value;
            } else {
                jObject.Add(property, value);
            }
        }

        public static void ApplyPatch_Overwrite(JToken token, Patch patch)
        {
            if (patch.Value == null)
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'Overwrite' but has not supplied a json Value! This patch will be ignored!");
                return;
            }

            if (token.Type == JTokenType.Property)
            {
                ((JProperty)token).Value = patch.Value;
            }
            else if (token.Parent?.Type == JTokenType.Property)
            {
                ((JProperty)token.Parent).Value = patch.Value;
            }
            else
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'Overwrite' but did not select a json Object Property or Property Value! This patch will be ignored!");
            }
        }

        public static void ApplyPatch_Remove(JToken token, Patch patch)
        {
            if (patch.Value != null)
            {
                EpicLoot.LogWarningForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'Remove' but has supplied a json Value. (This patch will still be processed)");
            }

            token.Remove();
        }

        public static void ApplyPatch_Append(JToken token, Patch patch, bool appendAll = false)
        {
            var actionName = appendAll ? "AppendAll" : "Append";

            if (patch.Value == null)
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action '{actionName}' but has not supplied a json Value! This patch will be ignored!");
                return;
            }

            if (token.Type == JTokenType.Array)
            {
                if (appendAll)
                {
                    if (patch.Value.Type == JTokenType.Array)
                    {
                        var mergeSettings = new JsonMergeSettings
                        {
                            MergeArrayHandling = MergeArrayHandling.Concat,
                            MergeNullValueHandling = MergeNullValueHandling.Ignore
                        };
                        ((JArray)token).Merge(patch.Value, mergeSettings);

                    }
                    else
                    {
                        EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'AppendAll' but has provided a value in the source file that is not a json Array!");
                    }
                }
                else
                {
                    ((JArray)token).Add(patch.Value);
                }

            }
            else
            {

                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action {actionName} but has selected a token in the target file that is not a json Array!");
            }
        }

        public static void ApplyPatch_Insert(JToken token, Patch patch, bool after)
        {
            var actionName = $"Insert{(after ? "After" : "Before")}";
            if (patch.Value == null)
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action '{actionName}' but has not supplied a json Value! This patch will be ignored!");
                return;
            }

            var parent = token.Parent;
            if (parent == null)
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action '{actionName}' but the parent of the selected token is not a container! This patch will be ignored!");
                return;
            }

            if (parent.Type == JTokenType.Array)
            {
                if (after)
                    token.AddAfterSelf(patch.Value);
                else
                    token.AddBeforeSelf(patch.Value);
            }
            else if (parent.Type == JTokenType.Object)
            {
                if (string.IsNullOrEmpty(patch.PropertyName))
                {
                    EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action '{actionName}' and has selected a property of a json Object, but not provided a PropertyName! This patch will be ignored!");
                    return;
                }

                if (after)
                    token.AddAfterSelf(new JProperty(patch.PropertyName, patch.Value));
                else
                    token.AddBeforeSelf(new JProperty(patch.PropertyName, patch.Value));
            }
        }

        public static void ApplyPatch_RemoveAll(JToken token, Patch patch)
        {
            const string actionName = "RemoveAll";
            if (patch.Value != null)
            {
                EpicLoot.LogWarningForce($"Patch ({patch.SourceFile}, {patch.Path}) has action '{actionName}' but has supplied a json Value! (This patch will still be processed)");
            }

            if (token.Type == JTokenType.Array)
            {
                ((JArray)token).RemoveAll();
            }
            else if (token.Type == JTokenType.Object)
            {
                ((JObject)token).RemoveAll();
            }
            else
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action '{actionName}' but selected token is not an Array or Object! This patch will be ignored!");
            }
        }

        public static void ApplyPatch_Merge(JToken token, Patch patch)
        {
            if (patch.Value == null)
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'Merge' but has not supplied a json Value! This patch will be ignored!");
                return;
            }

            if (patch.Value.Type != JTokenType.Object)
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'Merge' but has supplied a json Value that is not an Object! This patch will be ignored!");
                return;
            }

            if (token.Type != JTokenType.Object)
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'Merge' but has selected a token that is not a json Object! This patch will be ignored!");
                return;
            }
            
            var baseObject = ((JObject)token);
            var partialObject = ((JObject)patch.Value);

            MergeObject(baseObject, partialObject);
        }

        private static void MergeObject(JObject baseObject, JObject partialObject)
        {
            foreach (JProperty partialProperty in partialObject.Properties())
            {
                if (baseObject.ContainsKey(partialProperty.Name) && baseObject.Property(partialProperty.Name) is JProperty baseProperty)
                {
                    if (baseProperty.Value.Type == JTokenType.Object && partialProperty.Value.Type == JTokenType.Object)
                    {
                        MergeObject((JObject)baseProperty.Value, (JObject)partialProperty.Value);
                    }
                    else
                    {
                        baseProperty.Value = partialProperty.Value;
                    }
                }
                else
                {
                    baseObject.Add(partialProperty.Name, partialProperty.Value);
                }
            }
        }
    }
}
