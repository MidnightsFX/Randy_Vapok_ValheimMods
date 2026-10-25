using System.Collections.Generic;
using EpicLoot_UnityLib;
using TMPro;
using UnityEngine;

namespace EpicLoot.Compendium;

/// <summary>
/// The glyphs of the gamepad buttons that drive a control, shown in its top corners while a pad is active:
/// one key sits top right, two sit top left and top right.
/// </summary>
public class MagicGamepadHints
{
    private const float Size = 22f;
    private const float Margin = 4f;

    private readonly List<GameObject> _hints = new();
    private bool _shown;

    public MagicGamepadHints(RectTransform parent, params string[] gamepadKeys)
    {
        for (int i = 0; i < gamepadKeys.Length; i++)
        {
            bool left = gamepadKeys.Length > 1 && i == 0;
            GameObject go = new GameObject(gamepadKeys[i], typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one;
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            MagicFontManager.Apply(text, MagicFontManager.TMP_FontOptions.AveriaSerifLibreOutline);
            text.fontSize = 16;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            text.richText = true;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(left ? 0f : 1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(Size, Size);
            rect.anchoredPosition = new Vector2(left ? Margin : -Margin, Margin);
            go.AddComponent<GamepadGlyph>().ZInputKey = gamepadKeys[i];
            go.SetActive(false);
            _hints.Add(go);
        }
    }

    public void Show(bool gamepadActive)
    {
        if (_shown == gamepadActive)
        {
            return;
        }

        _shown = gamepadActive;
        foreach (GameObject hint in _hints)
        {
            hint.SetActive(gamepadActive);
        }
    }
}
