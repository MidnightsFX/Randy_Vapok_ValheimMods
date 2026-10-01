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
    // A legacy InputField or Dropdown drives its own labels and would lose them to a TMP swap.
    public static int Convert(GameObject root, List<PrefabYaml.Reference> yamlReferences)
    {
        int count = ConvertControls(root, yamlReferences);
        HashSet<Text> inputTexts = new HashSet<Text>();
        foreach (InputField input in root.GetComponentsInChildren<InputField>(true))
        {
            inputTexts.Add(input.textComponent);
            inputTexts.Add(input.placeholder as Text);
        }

        foreach (Dropdown dropdown in root.GetComponentsInChildren<Dropdown>(true))
        {
            inputTexts.Add(dropdown.captionText);
            inputTexts.Add(dropdown.itemText);
        }

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

    private static int ConvertControls(GameObject root, List<PrefabYaml.Reference> yamlReferences)
    {
        int count = 0;
        foreach (InputField input in root.GetComponentsInChildren<InputField>(true))
        {
            if (!PrefabUtility.IsPartOfPrefabInstance(input))
            {
                ConvertInputField(root, input, yamlReferences);
                count++;
            }
        }

        foreach (Dropdown dropdown in root.GetComponentsInChildren<Dropdown>(true))
        {
            if (!PrefabUtility.IsPartOfPrefabInstance(dropdown))
            {
                ConvertDropdown(root, dropdown, yamlReferences);
                count++;
            }
        }

        return count;
    }

    private static void ConvertInputField(GameObject root, InputField input, List<PrefabYaml.Reference> yamlReferences)
    {
        GameObject go = input.gameObject;
        Text text = input.textComponent;
        Text placeholder = input.placeholder as Text;
        string content = input.text;
        int characterLimit = input.characterLimit;
        string contentType = input.contentType.ToString();
        string lineType = input.lineType.ToString();
        bool readOnly = input.readOnly;
        float caretBlinkRate = input.caretBlinkRate;
        int caretWidth = input.caretWidth;
        bool customCaretColor = input.customCaretColor;
        Color caretColor = input.caretColor;
        Color selectionColor = input.selectionColor;
        SelectableState state = SelectableState.Capture(input);
        List<(Component, string)> references = CollectReferences(root, input, yamlReferences);

        Object.DestroyImmediate(input);
        TextMeshProUGUI tmpText = text != null ? ConvertOne(root, text, yamlReferences) : null;
        TextMeshProUGUI tmpPlaceholder = placeholder != null ? ConvertOne(root, placeholder, yamlReferences) : null;

        TMP_InputField tmp = go.AddComponent<TMP_InputField>();
        tmp.textComponent = tmpText;
        tmp.placeholder = tmpPlaceholder;
        tmp.textViewport = (RectTransform)(tmpText != null ? tmpText.transform.parent : go.transform);
        tmp.text = content;
        tmp.characterLimit = characterLimit;
        tmp.contentType = (TMP_InputField.ContentType)System.Enum.Parse(typeof(TMP_InputField.ContentType), contentType);
        tmp.lineType = (TMP_InputField.LineType)System.Enum.Parse(typeof(TMP_InputField.LineType), lineType);
        tmp.readOnly = readOnly;
        tmp.caretBlinkRate = caretBlinkRate;
        tmp.caretWidth = caretWidth;
        tmp.customCaretColor = customCaretColor;
        tmp.caretColor = caretColor;
        tmp.selectionColor = selectionColor;
        if (tmpText != null)
        {
            tmp.pointSize = tmpText.fontSize;
        }

        state.Apply(tmp);
        Relink(references, tmp);
    }

    private static void ConvertDropdown(GameObject root, Dropdown dropdown, List<PrefabYaml.Reference> yamlReferences)
    {
        GameObject go = dropdown.gameObject;
        Text captionText = dropdown.captionText;
        Text itemText = dropdown.itemText;
        Image captionImage = dropdown.captionImage;
        Image itemImage = dropdown.itemImage;
        RectTransform template = dropdown.template;
        int value = dropdown.value;
        float alphaFadeSpeed = dropdown.alphaFadeSpeed;
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        foreach (Dropdown.OptionData option in dropdown.options)
        {
            options.Add(new TMP_Dropdown.OptionData(option.text, option.image, Color.white));
        }

        SelectableState state = SelectableState.Capture(dropdown);
        List<(Component, string)> references = CollectReferences(root, dropdown, yamlReferences);

        Object.DestroyImmediate(dropdown);
        TextMeshProUGUI tmpCaption = captionText != null ? ConvertOne(root, captionText, yamlReferences) : null;
        TextMeshProUGUI tmpItem = itemText != null ? ConvertOne(root, itemText, yamlReferences) : null;

        TMP_Dropdown tmp = go.AddComponent<TMP_Dropdown>();
        tmp.template = template;
        tmp.captionText = tmpCaption;
        tmp.itemText = tmpItem;
        tmp.captionImage = captionImage;
        tmp.itemImage = itemImage;
        tmp.options = options;
        tmp.alphaFadeSpeed = alphaFadeSpeed;
        tmp.SetValueWithoutNotify(value);

        state.Apply(tmp);
        Relink(references, tmp);
    }

    private struct SelectableState
    {
        private bool _interactable;
        private Graphic _targetGraphic;
        private Selectable.Transition _transition;
        private ColorBlock _colors;
        private SpriteState _spriteState;
        private AnimationTriggers _animationTriggers;
        private Navigation _navigation;

        public static SelectableState Capture(Selectable from)
        {
            return new SelectableState
            {
                _interactable = from.interactable,
                _targetGraphic = from.targetGraphic,
                _transition = from.transition,
                _colors = from.colors,
                _spriteState = from.spriteState,
                _animationTriggers = from.animationTriggers,
                _navigation = from.navigation,
            };
        }

        public void Apply(Selectable to)
        {
            to.interactable = _interactable;
            to.targetGraphic = _targetGraphic;
            to.transition = _transition;
            to.colors = _colors;
            to.spriteState = _spriteState;
            to.animationTriggers = _animationTriggers;
            to.navigation = _navigation;
        }
    }

    private static List<(Component, string)> CollectReferences(GameObject root, Component target, List<PrefabYaml.Reference> yamlReferences)
    {
        List<(Component, string)> references = FindReferences(root, target);
        string targetPath = PathOf(target.transform, root.transform);
        foreach (PrefabYaml.Reference reference in yamlReferences)
        {
            if (reference.TargetPath != targetPath)
            {
                continue;
            }

            Transform owner = reference.OwnerPath == "" ? root.transform : root.transform.Find(reference.OwnerPath);
            Component[] components = owner != null ? owner.GetComponents<Component>() : null;
            if (components != null && reference.OwnerComponentIndex < components.Length &&
                components[reference.OwnerComponentIndex] != null && components[reference.OwnerComponentIndex] != target)
            {
                references.Add((components[reference.OwnerComponentIndex], reference.PropertyPath));
            }
        }

        return references;
    }

    private static void Relink(List<(Component, string)> references, Object value)
    {
        foreach ((Component component, string propertyPath) in references)
        {
            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyPath);
            if (property == null)
            {
                Debug.LogWarning($"{component.GetType().Name} has no property {propertyPath}");
                continue;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static TextMeshProUGUI ConvertOne(GameObject root, Text text, List<PrefabYaml.Reference> yamlReferences)
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

        Relink(references, tmp);
        return tmp;
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
