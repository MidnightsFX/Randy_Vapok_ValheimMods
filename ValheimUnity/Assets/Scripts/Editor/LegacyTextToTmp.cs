using System.Collections.Generic;
using EpicLoot.Compendium;
using EpicLoot_UnityLib;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class LegacyTextToTmp
{
    [MenuItem("Mod/Convert Legacy Text to TMP (selected prefabs)")]
    private static void ConvertSelected()
    {
        foreach (Object selected in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(selected);
            if (path.EndsWith(".prefab"))
            {
                Debug.Log($"{path}: converted {ConvertPrefab(path)} Text components");
            }
        }
    }

    public static int ConvertPrefab(string assetPath)
    {
        List<PrefabYaml.Reference> references = PrefabYaml.TextReferences(assetPath);
        GameObject root = PrefabUtility.LoadPrefabContents(assetPath);
        try
        {
            int count = Convert(root, references);
            if (count > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(root, assetPath);
            }

            return count;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // Texts inside nested prefab instances belong to their own asset; convert that asset instead.
    // A legacy InputField drives its text and placeholder itself and would lose them to a TMP swap.
    public static int Convert(GameObject root, List<PrefabYaml.Reference> yamlReferences)
    {
        HashSet<Text> inputTexts = new HashSet<Text>();
        foreach (InputField input in root.GetComponentsInChildren<InputField>(true))
        {
            inputTexts.Add(input.textComponent);
            inputTexts.Add(input.placeholder as Text);
        }

        int count = 0;
        foreach (Text text in root.GetComponentsInChildren<Text>(true))
        {
            if (PrefabUtility.IsPartOfPrefabInstance(text) || inputTexts.Contains(text))
            {
                continue;
            }

            ConvertOne(root, text, yamlReferences);
            count++;
        }

        return count;
    }

    private static void ConvertOne(GameObject root, Text text, List<PrefabYaml.Reference> yamlReferences)
    {
        GameObject go = text.gameObject;
        string textPath = PathOf(text.transform, root.transform);
        string content = text.text;
        Color color = text.color;
        int fontSize = text.fontSize;
        bool bestFit = text.resizeTextForBestFit;
        int minSize = text.resizeTextMinSize;
        int maxSize = text.resizeTextMaxSize;
        TextAnchor anchor = text.alignment;
        bool richText = text.supportRichText;
        bool wrap = text.horizontalOverflow == HorizontalWrapMode.Wrap;
        bool overflow = text.verticalOverflow == VerticalWrapMode.Overflow;
        bool raycastTarget = text.raycastTarget;
        FontStyle style = text.fontStyle;
        Font legacyFont = text.font;
        MagicFontManager.TMP_FontOptions font = PickFont(legacyFont);
        List<(Component, string)> references = FindReferences(root, text);

        Object.DestroyImmediate(text);
        foreach (BaseMeshEffect effect in go.GetComponents<BaseMeshEffect>())
        {
            Object.DestroyImmediate(effect);
        }

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.color = color;
        tmp.fontSize = fontSize;
        tmp.enableAutoSizing = bestFit;
        tmp.fontSizeMin = minSize;
        tmp.fontSizeMax = maxSize;
        tmp.alignment = Map(anchor);
        tmp.richText = richText;
        tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        tmp.overflowMode = overflow ? TextOverflowModes.Overflow : TextOverflowModes.Truncate;
        tmp.raycastTarget = raycastTarget;
        tmp.fontStyle = Map(style);
        if (legacyFont != null && legacyFont.name.Contains("Bold") && font != MagicFontManager.TMP_FontOptions.NorseBoldOutline)
        {
            tmp.fontStyle |= FontStyles.Bold;
        }
        go.AddComponent<VanillaFont>().Font = font;

        foreach (PrefabYaml.Reference reference in yamlReferences)
        {
            if (reference.TargetPath != textPath)
            {
                continue;
            }

            Transform owner = reference.OwnerPath == "" ? root.transform : root.transform.Find(reference.OwnerPath);
            Component[] components = owner != null ? owner.GetComponents<Component>() : null;
            if (components != null && reference.OwnerComponentIndex < components.Length &&
                components[reference.OwnerComponentIndex] != null)
            {
                references.Add((components[reference.OwnerComponentIndex], reference.PropertyPath));
            }
        }

        foreach ((Component component, string propertyPath) in references)
        {
            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyPath);
            if (property == null)
            {
                Debug.LogWarning($"{component.GetType().Name} has no property {propertyPath} for {textPath}");
                continue;
            }

            property.objectReferenceValue = tmp;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static string PathOf(Transform transform, Transform root)
    {
        List<string> parts = new List<string>();
        for (Transform t = transform; t != root && t != null; t = t.parent)
        {
            parts.Add(t.name);
        }

        parts.Reverse();
        return string.Join("/", parts);
    }

    private static MagicFontManager.TMP_FontOptions PickFont(Font font)
    {
        string name = font != null ? font.name : "";
        if (name.Contains("Norse"))
        {
            return MagicFontManager.TMP_FontOptions.NorseBoldOutline;
        }

        if (name.Contains("Serif"))
        {
            return MagicFontManager.TMP_FontOptions.AveriaSerifLibreOutline;
        }

        return MagicFontManager.TMP_FontOptions.AveriaSansLibreOutline;
    }

    public static List<(Component, string)> FindReferences(GameObject root, Object target)
    {
        List<(Component, string)> references = new List<(Component, string)>();
        foreach (Component component in root.GetComponentsInChildren<Component>(true))
        {
            if (component == null || component == target)
            {
                continue;
            }

            SerializedProperty property = new SerializedObject(component).GetIterator();
            while (property.Next(true))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference &&
                    property.objectReferenceValue == target)
                {
                    references.Add((component, property.propertyPath));
                }
            }
        }

        return references;
    }

    private static TextAlignmentOptions Map(TextAnchor anchor)
    {
        switch (anchor)
        {
            case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
            case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
            case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
            case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
            case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
            case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
            case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
            case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
            case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
            default: return TextAlignmentOptions.TopLeft;
        }
    }

    private static FontStyles Map(FontStyle style)
    {
        switch (style)
        {
            case FontStyle.Bold: return FontStyles.Bold;
            case FontStyle.Italic: return FontStyles.Italic;
            case FontStyle.BoldAndItalic: return FontStyles.Bold | FontStyles.Italic;
            default: return FontStyles.Normal;
        }
    }
}
