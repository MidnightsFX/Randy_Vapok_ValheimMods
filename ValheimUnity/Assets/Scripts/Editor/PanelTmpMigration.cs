using System;
using System.Collections.Generic;
using EpicLoot;
using EpicLoot_UnityLib;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PanelTmpMigration
{
    private const string Folder = "Assets/EpicLoot/Prefabs/Enchanting/";

    private static readonly (string path, string key)[] TabIcons =
    {
        ("GamepadHints/TabHints/TabLeft/TabLeft (1)", "JoyTabLeft"),
        ("GamepadHints/TabHints/TabRight/TabRight (1)", "JoyTabRight"),
    };

    private static readonly (string path, string key)[] BottomRowIcons = Concat(TabIcons,
        ("GamepadHints/BottomRow/ScrollButton/Icon", "JoyRStickUp"),
        ("GamepadHints/BottomRow/SelectButton/Icon", "JoyButtonA"),
        ("GamepadHints/BottomRow/SortingButton/Icon", "JoyRStick"));

    private static readonly (string path, string key)[] QuantityRowIcons = Concat(TabIcons,
        ("GamepadHints/BottomRow/QuantityButton/Icon", "JoyDPadUp"),
        ("GamepadHints/BottomRow/SelectAllButton/Icon", "JoyLStick"),
        ("GamepadHints/BottomRow/SelectButton/Icon", "JoyButtonA"),
        ("GamepadHints/BottomRow/SortingButton/Icon", "JoyRStick"));

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

    [MenuItem("Mod/Migrations/RuneContent to TMP")]
    public static void Rune()
    {
        Migrate("RuneContent", new string[0],
            Concat(BottomRowIcons, ("MainButton/Hint-1/Icon", "JoyButtonX"), ("GamepadHints/Hint/Icon", "JoyButtonY"), ("ModeSelectors/Hint/Icon", "JoyButtonY")),
            content =>
            {
                RuneUI ui = content.GetComponent<RuneUI>();
                Require(ui.CostLabel, ui.Warning);
            });
    }

    [MenuItem("Mod/Migrations/SacrificeContent to TMP")]
    public static void Sacrifice()
    {
        Migrate("SacrificeContent", new string[0],
            Concat(QuantityRowIcons, ("ModeSelectors/Hint/Icon", "JoyButtonY"), ("SacrificeButton/Hint/Icon", "JoyButtonX")),
            content =>
            {
                SacrificeUI ui = content.GetComponent<SacrificeUI>();
                Require(ui.Warning, ui.Explainer);
            });
    }

    [MenuItem("Mod/Migrations/ConvertContent to TMP")]
    public static void Convert()
    {
        Migrate("ConvertContent", new string[0],
            Concat(QuantityRowIcons, ("ModeSelectors/Hint/Icon", "JoyButtonY"), ("MainButton/Hint/Icon", "JoyButtonX")),
            content =>
            {
                ConvertUI ui = content.GetComponent<ConvertUI>();
                Require(ui.CostLabel);
            });
    }

    [MenuItem("Mod/Migrations/DisenchantContent to TMP")]
    public static void Disenchant()
    {
        Migrate("DisenchantContent", new string[0],
            Concat(TabIcons,
                ("GamepadHints/BottomRow/SelectButton/Icon", "JoyButtonA"),
                ("GamepadHints/BottomRow/SortingButton/Icon", "JoyRStick"),
                ("MainButton/Hint/Icon", "JoyButtonX")),
            content =>
            {
                DisenchantUI ui = content.GetComponent<DisenchantUI>();
                Require(ui.CostLabel);
            });
    }

    [MenuItem("Mod/Migrations/UpgradeContent to TMP")]
    public static void Upgrade()
    {
        Migrate("UpgradeContent", new string[0],
            Concat(TabIcons,
                ("GamepadHints/BottomRow/SelectButton/Icon", "JoyButtonA"),
                ("MainButton/Hint/Icon", "JoyButtonX")),
            content =>
            {
                UpgradeTableUI ui = content.GetComponent<UpgradeTableUI>();
                Require(ui.SelectedFeatureText, ui.SelectedFeatureInfoText, ui.CostLabel);
                ui.SelectedFeatureInfoText.lineSpacing = -15;
            });
    }

    [MenuItem("Mod/Migrations/List elements to TMP")]
    public static void ListElements()
    {
        List<PrefabYaml.TextOverride> upgradeOverrides = PrefabYaml.NestedTextOverrides(Folder + "UpgradeContent.prefab");
        foreach (string leaf in new[] { "EnchantElement", "ItemElement", "ItemGridElement", "FeatureStatus", "TableFeatureElement", "MultiSelectItemList", "SingleSelectItemList" })
        {
            string path = Folder + leaf + ".prefab";
            Debug.Log($"{path}: converted {LegacyTextToTmp.ConvertPrefab(path)} Text components");
        }

        foreach (string leaf in new[] { "ItemElement", "ItemGridElement", "TableFeatureElement" })
        {
            MultiSelectItemListElement element = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + leaf + ".prefab").GetComponent<MultiSelectItemListElement>();
            Require(element.ItemName != null ? element.ItemName : element.ItemTotalQuantity);
        }

        Require(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "FeatureStatus.prefab").GetComponent<FeatureStatus>().ManyStarsLabel);
        ReapplyNestedTextOverrides("UpgradeContent", upgradeOverrides);
        AssetDatabase.SaveAssets();
        Debug.Log("List elements migration done");
    }

    // Captured before the nested prefab converts: the override targets the old Text file id.
    private static void ReapplyNestedTextOverrides(string content, List<PrefabYaml.TextOverride> overrides)
    {
        string contentPath = Folder + content + ".prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(contentPath);
        try
        {
            int applied = 0;
            foreach (PrefabYaml.TextOverride entry in overrides)
            {
                Transform instance = FindDeep(root.transform, entry.InstanceName);
                Transform target = instance != null ? FindDeep(instance, entry.TargetObjectName) : null;
                TMP_Text label = target != null ? target.GetComponent<TMP_Text>() : null;
                if (label != null)
                {
                    label.text = entry.Value;
                    applied++;
                }
            }

            Debug.Log($"{contentPath}: re-applied {applied} of {overrides.Count} nested label overrides");
            PrefabUtility.SaveAsPrefabAsset(root, contentPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
            {
                return child;
            }
        }

        return null;
    }

    [MenuItem("Mod/Migrations/Input fields and dropdowns to TMP")]
    public static void Controls()
    {
        string[] panels = { "EnchantContent", "AugmentContent", "RuneContent", "SacrificeContent", "ConvertContent", "DisenchantContent", "UpgradeContent" };
        Dictionary<string, List<PrefabYaml.TextOverride>> overrides = new Dictionary<string, List<PrefabYaml.TextOverride>>();
        foreach (string panel in panels)
        {
            overrides[panel] = PrefabYaml.NestedTextOverrides(Folder + panel + ".prefab");
        }

        foreach (string prefab in new[] { "MultiSelectItemList", "SingleSelectItemList", "ItemElement", "RuneContent", "SacrificeContent" })
        {
            string path = Folder + prefab + ".prefab";
            Debug.Log($"{path}: converted {LegacyTextToTmp.ConvertPrefab(path)} controls and labels");
        }

        foreach (string list in new[] { "MultiSelectItemList", "SingleSelectItemList" })
        {
            MultiSelectItemList component = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + list + ".prefab").GetComponent<MultiSelectItemList>();
            Require(component.SortByDropdown, component.FilterByText);
        }

        Require(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "ItemElement.prefab").GetComponent<MultiSelectItemListElement>().ItemSelectedQuantity);
        Require(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "SacrificeContent.prefab").GetComponent<SacrificeUI>().IdentifyStyle);

        foreach (string panel in panels)
        {
            if (overrides[panel].Count > 0)
            {
                ReapplyNestedTextOverrides(panel, overrides[panel]);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Controls migration done");
    }

    [MenuItem("Mod/Migrations/Tab bar hints to glyphs")]
    public static void TabBar()
    {
        Migrate("EnchantingUI", new string[0],
            new[]
            {
                ("Panel/TabGamepadHints/LTrigger/LTrigger (1)", "JoyLTrigger"),
                ("Panel/TabGamepadHints/RTrigger/RTrigger (1)", "JoyRTrigger"),
            },
            content => { });
    }

    [MenuItem("Mod/Migrations/Selector hints follow the last button")]
    public static void SelectorHints()
    {
        (string content, string selectors)[] columns =
        {
            ("SacrificeContent", "ModeSelectors"),
            ("RuneContent", "ModeSelectors"),
            ("ConvertContent", "ModeSelectors"),
            ("EnchantContent", "RaritySelectors"),
        };

        foreach ((string content, string selectors) in columns)
        {
            string contentPath = Folder + content + ".prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(contentPath);
            try
            {
                Transform column = root.transform.Find(selectors);
                if (!column.TryGetComponent(out ContentSizeFitter fitter))
                {
                    fitter = column.gameObject.AddComponent<ContentSizeFitter>();
                }

                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                if (column.Find("Hint") == null)
                {
                    RectTransform hint = (RectTransform)root.transform.Find("GamepadHints/Hint");
                    hint.SetParent(column, false);
                    hint.anchorMin = hint.anchorMax = new Vector2(1, 0);
                    hint.anchoredPosition = new Vector2(5, -8);

                    // UIGamePad lives in assembly_valheim, which the editor assembly cannot reference.
                    Component mainButtonPad = root.transform.Find("MainButton").GetComponent("UIGamePad");
                    SerializedObject pad = new SerializedObject(column.gameObject.AddComponent(mainButtonPad.GetType()));
                    pad.FindProperty("m_hint").objectReferenceValue = hint.gameObject;
                    pad.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, contentPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            Debug.Log($"{content} selector hint migration done");
        }

        AssetDatabase.SaveAssets();
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
