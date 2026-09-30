using EpicLoot.Compendium;
using EpicLoot_UnityLib;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class GamepadGlyphTools
{
    public const string PrefabPath = "Assets/EpicLoot/Prefabs/UI/GamepadGlyph.prefab";

    [MenuItem("Mod/Create GamepadGlyph prefab")]
    public static GameObject EnsurePrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing != null)
        {
            return existing;
        }

        GameObject go = new GameObject("GamepadGlyph", typeof(RectTransform));
        ((RectTransform)go.transform).sizeDelta = new Vector2(40, 40);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "$KEY_ButtonA";
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 20;
        tmp.richText = true;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;

        go.AddComponent<GamepadGlyph>();
        go.AddComponent<VanillaFont>().Font = MagicFontManager.TMP_FontOptions.AveriaSansLibre;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // Swaps a hand-drawn glyph Image for a GamepadGlyph instance in the same rect and sibling slot.
    public static void ReplaceIcon(GameObject root, string iconPath, string zinputKey)
    {
        Transform icon = root.transform.Find(iconPath);
        if (icon == null)
        {
            throw new System.ArgumentException($"{root.name}: no object at {iconPath}");
        }

        RectTransform iconRect = (RectTransform)icon;
        GameObject glyph = (GameObject)PrefabUtility.InstantiatePrefab(EnsurePrefab(), icon.parent);
        glyph.name = zinputKey;
        glyph.transform.SetSiblingIndex(icon.GetSiblingIndex());

        RectTransform glyphRect = (RectTransform)glyph.transform;
        glyphRect.anchorMin = iconRect.anchorMin;
        glyphRect.anchorMax = iconRect.anchorMax;
        glyphRect.pivot = iconRect.pivot;
        glyphRect.anchoredPosition = iconRect.anchoredPosition;
        glyphRect.sizeDelta = iconRect.sizeDelta;

        glyph.GetComponent<GamepadGlyph>().ZInputKey = zinputKey;

        LayoutElement layout = icon.GetComponent<LayoutElement>();
        if (layout != null)
        {
            LayoutElement copy = glyph.AddComponent<LayoutElement>();
            copy.ignoreLayout = layout.ignoreLayout;
            copy.minWidth = layout.minWidth;
            copy.minHeight = layout.minHeight;
            copy.preferredWidth = layout.preferredWidth;
            copy.preferredHeight = layout.preferredHeight;
            copy.flexibleWidth = layout.flexibleWidth;
            copy.flexibleHeight = layout.flexibleHeight;
        }

        Object.DestroyImmediate(icon.gameObject);
    }
}
