using System.Collections.Generic;
using System.Linq;
using EpicLoot_UnityLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace EpicLoot.Compendium;

/// <summary>
/// A dropdown in the compendium frame, cloned from the enchanting panel's sort picker, with the glyphs of
/// the gamepad buttons that step through it.
/// </summary>
public class MagicDropdown
{
    private readonly GameObject _obj;
    private readonly RectTransform _rect;
    private readonly TMP_Dropdown _dropdown;
    private readonly MagicGamepadHints _hints;

    /// <summary>Null when the enchanting table prefab, which carries the template, is not loaded.</summary>
    public static MagicDropdown TryCreate(Transform parent, string name, UnityAction<int> onChanged, params string[] gamepadKeys)
    {
        GameObject tablePrefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(EnchantingTable.PrefabName) : null;
        GameObject uiPrefab = tablePrefab != null ? tablePrefab.GetComponent<EnchantingTable>()?.EnchantingUIPrefab : null;
        TMP_Dropdown template = uiPrefab != null ? uiPrefab.GetComponentInChildren<TMP_Dropdown>(true) : null;
        return template != null ? new MagicDropdown(parent, name, template, onChanged, gamepadKeys) : null;
    }

    private MagicDropdown(Transform parent, string name, TMP_Dropdown template, UnityAction<int> onChanged, string[] gamepadKeys)
    {
        _obj = Object.Instantiate(template.gameObject, parent, false);
        _obj.name = name;
        _obj.SetActive(true);
        _rect = (RectTransform)_obj.transform;
        _rect.localScale = Vector3.one;
        _dropdown = _obj.GetComponent<TMP_Dropdown>();
        _dropdown.onValueChanged.RemoveAllListeners();
        _dropdown.onValueChanged.AddListener(onChanged);

        // The sort picker carries its own "Sort by" caption beside the dropdown; only the value text and the
        // list template belong here.
        foreach (TextMeshProUGUI text in _obj.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            bool inTemplate = _dropdown.template != null && text.transform.IsChildOf(_dropdown.template);
            if (text != _dropdown.captionText && !inTemplate)
            {
                Object.Destroy(text.gameObject);
            }
        }

        _hints = new MagicGamepadHints(_rect, gamepadKeys);
    }

    /// <summary>Replaces the options and opens the list tall enough to show them all, up to <paramref name="maxListHeight"/>.</summary>
    public void SetOptions(IEnumerable<string> labels, float maxListHeight = 400f)
    {
        _dropdown.ClearOptions();
        _dropdown.AddOptions(labels.Select(x => Localization.instance.Localize(x)).ToList());

        RectTransform item = _dropdown.itemText != null ? _dropdown.itemText.rectTransform.parent as RectTransform : null;
        if (_dropdown.template != null && item != null)
        {
            float height = Mathf.Min(_dropdown.options.Count * item.sizeDelta.y + 8f, maxListHeight);
            _dropdown.template.sizeDelta = new Vector2(_dropdown.template.sizeDelta.x, height);
        }
    }

    /// <summary>Sets the text size of the value and the list entries, and makes each entry tall enough to hold it.</summary>
    public void SetFontSize(int size)
    {
        if (_dropdown.captionText != null)
        {
            _dropdown.captionText.fontSize = size;
        }

        if (_dropdown.itemText == null)
        {
            return;
        }

        _dropdown.itemText.fontSize = size;
        if (_dropdown.itemText.rectTransform.parent is RectTransform item)
        {
            item.sizeDelta = new Vector2(item.sizeDelta.x, size + 8f);
        }
    }

    public void SetValue(int index) => _dropdown.SetValueWithoutNotify(index);

    public void Dock(Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition)
    {
        _rect.anchorMin = _rect.anchorMax = anchor;
        _rect.pivot = pivot;
        _rect.anchoredPosition3D = new Vector3(anchoredPosition.x, anchoredPosition.y, 0f);
    }

    public void SetSize(float width, float height) => _rect.sizeDelta = new Vector2(width, height);

    public void Enable(bool enable) => _obj.SetActive(enable);

    public bool IsEnabled => _obj.activeInHierarchy;

    public void ShowHint(bool gamepadActive) => _hints.Show(gamepadActive);
}
