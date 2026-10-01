using System;
using System.Collections.Generic;
using EpicLoot;
using EpicLoot_UnityLib;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class PanelTmpMigration
{
    private const string Folder = "Assets/EpicLoot/Prefabs/Enchanting/";

    private static readonly (string path, string key)[] BottomRowIcons =
    {
        ("GamepadHints/BottomRow/ScrollButton/Icon", "JoyRStickUp"),
        ("GamepadHints/BottomRow/SelectButton/Icon", "JoyButtonA"),
        ("GamepadHints/BottomRow/SortingButton/Icon", "JoyRStick"),
        ("GamepadHints/TabHints/TabLeft/TabLeft (1)", "JoyTabLeft"),
        ("GamepadHints/TabHints/TabRight/TabRight (1)", "JoyTabRight"),
    };

    [MenuItem("Mod/Migrations/EnchantContent to TMP")]
    public static void Enchant()
    {
        Migrate("EnchantContent", new[] { "EnchantRaritySelector" },
            Concat(BottomRowIcons, ("MainButton/Hint-1/Icon", "JoyButtonX"), ("GamepadHints/Hint/Icon", "JoyButtonY")),
            content =>
            {
                // The rarity toggles overrode the selector label per instance; those overrides died with
                // the Text component they targeted.
                EnchantUI ui = content.GetComponent<EnchantUI>();
                for (int i = 0; i < ui.RarityButtons.Count; i++)
                {
                    ui.RarityButtons[i].GetComponentInChildren<TMP_Text>(true).text = $"$mod_epicloot_{(ItemRarity)i}";
                }

                Require(ui.EnchantInfo, ui.CostLabel);
            });
    }

    [MenuItem("Mod/Migrations/AugmentContent to TMP")]
    public static void Augment()
    {
        Migrate("AugmentContent", new[] { "AugmentSelector" },
            Concat(BottomRowIcons, ("MainButton/Hint/Icon", "JoyButtonX"), ("GamepadHints/Hint/Icon", "JoyButtonY")),
            content =>
            {
                AugmentUI ui = content.GetComponent<AugmentUI>();
                Require(ui.AvailableEffectsText, ui.AvailableEffectsHeader, ui.CostLabel);
            });
    }

    private static void Migrate(string content, string[] nestedPrefabs, (string path, string key)[] icons, Action<GameObject> verify)
    {
        foreach (string nested in nestedPrefabs)
        {
            string nestedPath = Folder + nested + ".prefab";
            Debug.Log($"{nestedPath}: converted {LegacyTextToTmp.ConvertPrefab(nestedPath)} Text components");
        }

        string contentPath = Folder + content + ".prefab";
        List<PrefabYaml.Reference> references = PrefabYaml.TextReferences(contentPath);
        GameObject root = PrefabUtility.LoadPrefabContents(contentPath);
        try
        {
            Debug.Log($"{contentPath}: converted {LegacyTextToTmp.Convert(root, references)} Text components");

            foreach ((string path, string key) in icons)
            {
                GamepadGlyphTools.ReplaceIcon(root, path, key);
            }

            verify(root);
            PrefabUtility.SaveAsPrefabAsset(root, contentPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"{content} migration done");
    }

    private static void Require(params UnityEngine.Object[] references)
    {
        foreach (UnityEngine.Object reference in references)
        {
            if (reference == null)
            {
                throw new InvalidOperationException("a label reference was lost in the conversion");
            }
        }
    }

    private static (string, string)[] Concat((string, string)[] shared, params (string, string)[] extra)
    {
        List<(string, string)> all = new List<(string, string)>(shared);
        all.AddRange(extra);
        return all.ToArray();
    }
}
