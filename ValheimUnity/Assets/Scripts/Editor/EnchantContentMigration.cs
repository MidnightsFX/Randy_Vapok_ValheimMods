using System.Collections.Generic;
using EpicLoot;
using EpicLoot_UnityLib;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class EnchantContentMigration
{
    private const string ContentPath = "Assets/EpicLoot/Prefabs/Enchanting/EnchantContent.prefab";
    private const string SelectorPath = "Assets/EpicLoot/Prefabs/Enchanting/EnchantRaritySelector.prefab";

    private static readonly (string path, string key)[] Icons =
    {
        ("MainButton/Hint-1/Icon", "JoyButtonX"),
        ("GamepadHints/Hint/Icon", "JoyButtonY"),
        ("GamepadHints/BottomRow/ScrollButton/Icon", "JoyRStickUp"),
        ("GamepadHints/BottomRow/SelectButton/Icon", "JoyButtonA"),
        ("GamepadHints/BottomRow/SortingButton/Icon", "JoyRStick"),
        ("GamepadHints/TabHints/TabLeft/TabLeft (1)", "JoyTabLeft"),
        ("GamepadHints/TabHints/TabRight/TabRight (1)", "JoyTabRight"),
    };

    [MenuItem("Mod/Migrations/EnchantContent to TMP")]
    public static void Run()
    {
        Debug.Log($"{SelectorPath}: converted {LegacyTextToTmp.ConvertPrefab(SelectorPath)} Text components");

        List<PrefabYaml.Reference> references = PrefabYaml.TextReferences(ContentPath);
        GameObject content = PrefabUtility.LoadPrefabContents(ContentPath);
        try
        {
            Debug.Log($"{ContentPath}: converted {LegacyTextToTmp.Convert(content, references)} Text components");

            foreach ((string path, string key) in Icons)
            {
                GamepadGlyphTools.ReplaceIcon(content, path, key);
            }

            // The rarity toggles overrode the selector label per instance; those overrides died with
            // the Text component they targeted.
            EnchantUI ui = content.GetComponent<EnchantUI>();
            for (int i = 0; i < ui.RarityButtons.Count; i++)
            {
                ui.RarityButtons[i].GetComponentInChildren<TMP_Text>(true).text = $"$mod_epicloot_{(ItemRarity)i}";
            }

            if (ui.EnchantInfo == null || ui.CostLabel == null)
            {
                throw new System.InvalidOperationException("EnchantUI lost its label references");
            }

            PrefabUtility.SaveAsPrefabAsset(content, ContentPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(content);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("EnchantContent migration done");
    }
}
