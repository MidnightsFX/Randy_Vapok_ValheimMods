using EpicLoot.Compendium;
using Jotunn.Managers;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.QuickConfig;

/// <summary>
/// The runtime font pass for the Quick Configure prefabs, the same one TemperPanel runs: the
/// placeholder TMP font authored in Unity is replaced with Valheim's own per text role.
/// </summary>
internal static class QuickConfigStyle {
    // Off by default so the authored look wins. Flip to restyle plain uGUI sliders and buttons with
    // Jotunn's Valheim-styled sprites when a prefab shipped without its own.
    private const bool UseJotunnWidgetStyle = false;

    internal static void Apply(GameObject root) {
        if (root == null) { return; }
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true)) {
            try {
                MagicFontManager.Apply(text, RoleOf(text));
            } catch (Exception e) {
                EpicLoot.LogWarning($"Quick Configure could not restyle '{text.name}': {e.Message}");
            }
        }
#pragma warning disable CS0162 // Unreachable code: the constant is the switch.
        if (UseJotunnWidgetStyle) { ApplyJotunnWidgets(root); }
#pragma warning restore CS0162
    }

    // NorseBoldOutline for the panel title, page titles and Row_Header labels; AveriaSansLibre for
    // text typed into a field and the status line; AveriaSansLibreOutline for everything else
    // (row labels, button captions, notes).
    private static MagicFontManager.TMP_FontOptions RoleOf(TMP_Text text) {
        if (text.name == "Title" || IsHeaderLabel(text.transform)) {
            return MagicFontManager.TMP_FontOptions.NorseBoldOutline;
        }
        if (text.name == "Status" || text.GetComponentInParent<TMP_InputField>(true) != null) {
            return MagicFontManager.TMP_FontOptions.AveriaSansLibre;
        }
        return MagicFontManager.TMP_FontOptions.AveriaSansLibreOutline;
    }

    // A header row keeps its template name (Row_Header, Row_Header (2)); its label may sit a level down.
    private static bool IsHeaderLabel(Transform text) {
        Transform current = text.parent;
        for (int depth = 0; current != null && depth < 3; depth++, current = current.parent) {
            if (current.name.StartsWith("Row_Header", StringComparison.Ordinal)) { return true; }
        }
        return false;
    }

    private static void ApplyJotunnWidgets(GameObject root) {
        if (GUIManager.Instance == null) { return; }
        foreach (Slider slider in root.GetComponentsInChildren<Slider>(true)) {
            try { GUIManager.Instance.ApplySliderStyle(slider); } catch (Exception) { /* keep the authored look */ }
        }
        foreach (Button button in root.GetComponentsInChildren<Button>(true)) {
            try { GUIManager.Instance.ApplyButtonStyle(button); } catch (Exception) { /* keep the authored look */ }
        }
    }
}
