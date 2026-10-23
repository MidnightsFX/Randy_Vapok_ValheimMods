using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EpicLoot.Compendium;

/// <summary>A labelled button in the compendium frame, with the glyphs of the gamepad buttons that trigger it.</summary>
public class MagicPanelButton
{
    private readonly GameObject _obj;
    private readonly RectTransform _rect;
    private readonly Image _background;
    private readonly Button _button;
    private readonly TextMeshProUGUI _label;
    private readonly MagicGamepadHints _hints;

    public MagicPanelButton(Transform parent, string name, UnityAction onClick, params string[] gamepadKeys)
    {
        _obj = new GameObject(name, typeof(RectTransform));
        _rect = (RectTransform)_obj.transform;
        _rect.SetParent(parent, false);
        _rect.localScale = Vector3.one;
        _background = _obj.AddComponent<Image>();
        _button = _obj.AddComponent<Button>();
        _button.targetGraphic = _background;
        _button.navigation = new Navigation { mode = Navigation.Mode.None };
        _button.onClick.AddListener(onClick);

        GameObject labelGo = new GameObject("label", typeof(RectTransform));
        labelGo.transform.SetParent(_rect, false);
        labelGo.transform.localScale = Vector3.one;
        _label = labelGo.AddComponent<TextMeshProUGUI>();
        MagicFontManager.Apply(_label, MagicFontManager.TMP_FontOptions.AveriaSerifLibreOutline);
        _label.fontSize = MagicPages.FONT_SIZE;
        _label.alignment = TextAlignmentOptions.Center;
        _label.enableWordWrapping = false;
        _label.overflowMode = TextOverflowModes.Overflow;
        _label.raycastTarget = false;
        _label.richText = true;
        RectTransform labelRect = _label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 0f);
        labelRect.offsetMax = new Vector2(-8f, 0f);

        _hints = new MagicGamepadHints(_rect, gamepadKeys);
    }

    public void SetBackground(Image source)
    {
        _background.sprite = source.sprite;
        _background.material = source.material;
        _background.color = source.color;
        _background.type = source.type;
    }

    public void SetBackgroundColor(Color color)
    {
        _background.sprite = null;
        _background.color = color;
    }

    public void Dock(Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition)
    {
        _rect.anchorMin = _rect.anchorMax = anchor;
        _rect.pivot = pivot;
        _rect.anchoredPosition3D = new Vector3(anchoredPosition.x, anchoredPosition.y, 0f);
    }

    public void SetSize(float width, float height) => _rect.sizeDelta = new Vector2(width, height);

    public void Enable(bool enable) => _obj.SetActive(enable);

    public bool IsEnabled => _obj.activeInHierarchy;

    public void SetLabel(string text) => _label.text = Localization.instance.Localize(text);

    public void ShowHint(bool gamepadActive) => _hints.Show(gamepadActive);
}
