using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.Compendium;

public class MagicTableColumn
{
    public string Header;
    public float Weight;
    public TextAnchor Alignment = TextAnchor.MiddleLeft;
    public bool Outline;
}

public class MagicTextTable : IMagicTextPart
{
    private const float PadX = 8f;
    private const float PadY = 4f;
    private const float CellSpacing = 6f;
    private const float ArrowSize = 14f;
    private const float RowMinHeight = 24f;
    private const float DetailIndent = 30f;
    private const float DetailBottomPad = 6f;
    private const int HeaderFontSize = 15;
    private const int DetailFontSize = 16;
    private const float SeparatorHeight = 2f;

    private static readonly Color HeaderColor = new Color(0.85f, 0.82f, 0.76f, 0.9f);
    private static readonly Color SeparatorColor = new Color(1f, 1f, 1f, 0.22f);
    private static readonly Color StripeColor = new Color(1f, 1f, 1f, 0.05f);
    private static readonly Color HoverColor = new Color(1f, 1f, 1f, 0.14f);
    private static readonly Color PressedColor = new Color(1f, 1f, 1f, 0.2f);
    private static readonly Color ArrowColor = new Color(0.85f, 0.82f, 0.76f, 0.9f);

    private readonly GameObject _obj;
    private readonly RectTransform _rect;
    private readonly MagicTextElement _template;
    private readonly MagicTableColumn[] _columns;
    private readonly float[] _cellWidths;
    private readonly Action _onLayoutChanged;
    private readonly List<Row> _rows = new();
    private readonly List<RectTransform> _fixedParts = new();
    private readonly Sprite _arrowSprite;

    public MagicTextTable(Transform parent, MagicTextElement template, float width, MagicTableColumn[] columns,
        Action onLayoutChanged)
    {
        _template = template;
        _columns = columns;
        _onLayoutChanged = onLayoutChanged;
        _arrowSprite = EpicAssets.AssetBundle?.LoadAsset<Sprite>("Assets/EpicLoot/Sprites/Enchanting/DownArrow.png");

        _obj = new GameObject("table", typeof(RectTransform));
        _rect = (RectTransform)_obj.transform;
        _rect.SetParent(parent, false);
        _rect.localScale = Vector3.one;
        _rect.sizeDelta = new Vector2(width, 0f);
        VerticalLayoutGroup layout = _obj.AddComponent<VerticalLayoutGroup>();
        ConfigureVertical(layout, 0);

        float inner = width - 2f * PadX - ArrowSize - CellSpacing * columns.Length;
        float totalWeight = columns.Sum(x => x.Weight);
        _cellWidths = columns.Select(x => inner * x.Weight / totalWeight).ToArray();

        BuildHeader();
    }

    /// <summary><paramref name="tag"/> identifies the row to the filter predicate a page may apply.</summary>
    public void AddRow(string[] cells, IList<string> detail, bool expanded, object tag = null)
    {
        Row row = new Row(this, cells, detail, expanded && detail.Count > 0 && !MagicPages.Collapsed, tag);
        _rows.Add(row);
    }

    public void Finish()
    {
        _fixedParts.Add(CreateSeparator(_rect));
        Relayout();
    }

    public bool IsMatch(string loweredQuery) => _rows.Any(x => x.IsMatch(loweredQuery));
    public void Enable(bool enable) => _obj.SetActive(enable);
    public bool IsVisible => _obj.activeSelf;
    public void Destroy() => UnityEngine.Object.Destroy(_obj);
    public float GetHeight() => _rect.sizeDelta.y;

    public void SetAllExpanded(bool expanded)
    {
        foreach (Row row in _rows)
        {
            row.SetExpanded(expanded);
        }

        Relayout();
    }

    public void Filter(string loweredQuery, Func<object, bool> include = null)
    {
        bool any = false;
        foreach (Row row in _rows)
        {
            bool match = (loweredQuery.Length == 0 || row.IsMatch(loweredQuery)) && (include == null || include(row.Tag));
            row.Enable(match);
            any |= match;
        }

        Enable(any);
        Relayout();
    }

    private void Relayout()
    {
        int shown = 0;
        foreach (Row row in _rows.Where(x => x.Visible))
        {
            row.SetStriped(shown++ % 2 == 1);
        }

        float height = _fixedParts.Sum(x => x.sizeDelta.y) + _rows.Where(x => x.Visible).Sum(x => x.GetHeight());
        _rect.sizeDelta = new Vector2(_rect.sizeDelta.x, height);
        LayoutRebuilder.MarkLayoutForRebuild(_rect);
        _onLayoutChanged?.Invoke();
    }

    private void BuildHeader()
    {
        RectTransform line = CreateLine(_rect, "header");
        HorizontalLayoutGroup layout = line.GetComponent<HorizontalLayoutGroup>();
        layout.padding.left = Mathf.RoundToInt(PadX + ArrowSize + CellSpacing);

        float height = RowMinHeight;
        for (int i = 0; i < _columns.Length; i++)
        {
            MagicTextElement cell = CreateCell(line, _columns[i].Header, i, HeaderFontSize, false);
            cell.SetColor(HeaderColor);
            height = Mathf.Max(height, cell.GetHeight() + 2f * PadY);
        }

        line.sizeDelta = new Vector2(line.sizeDelta.x, height);
        _fixedParts.Add(line);
        _fixedParts.Add(CreateSeparator(_rect));
    }

    private MagicTextElement CreateCell(Transform line, string text, int column, int fontSize, bool allowOutline)
    {
        MagicTextElement cell = _template.Create(text, line, false);
        cell.SetFontSize(fontSize);
        cell.SetAlignment(_columns[column].Alignment);
        cell.EnableOutline(allowOutline && _columns[column].Outline);
        cell.Fit(_cellWidths[column], RowMinHeight - 2f * PadY);
        LayoutElement element = cell.Rect.gameObject.AddComponent<LayoutElement>();
        element.minWidth = element.preferredWidth = _cellWidths[column];
        element.minHeight = element.preferredHeight = cell.GetHeight();
        element.flexibleWidth = 0f;
        return cell;
    }

    private static RectTransform CreateLine(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        rect.sizeDelta = new Vector2(parent.GetComponent<RectTransform>().sizeDelta.x, RowMinHeight);
        HorizontalLayoutGroup layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(Mathf.RoundToInt(PadX), Mathf.RoundToInt(PadX), Mathf.RoundToInt(PadY),
            Mathf.RoundToInt(PadY));
        layout.spacing = CellSpacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return rect;
    }

    private static RectTransform CreateSeparator(Transform parent)
    {
        GameObject go = new GameObject("separator", typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        rect.sizeDelta = new Vector2(0f, SeparatorHeight);
        Image image = go.AddComponent<Image>();
        image.color = SeparatorColor;
        image.raycastTarget = false;
        return rect;
    }

    private static void ConfigureVertical(VerticalLayoutGroup layout, int leftPadding)
    {
        layout.padding = new RectOffset(leftPadding, 0, 0, 0);
        layout.spacing = 0f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private class Row : IMagicTextPart
    {
        private readonly MagicTextTable _table;
        private readonly RectTransform _container;
        private readonly RectTransform _line;
        private readonly RectTransform _detail;
        private readonly RectTransform _arrow;
        private readonly Button _button;
        private readonly List<MagicTextElement> _texts = new();
        private readonly IList<string> _detailLines;
        private readonly string[] _detailSearchText;
        private bool _detailBuilt;
        private bool _expanded;

        public bool Visible { get; private set; } = true;
        public object Tag { get; }

        public Row(MagicTextTable table, string[] cells, IList<string> detail, bool expanded, object tag)
        {
            _table = table;
            Tag = tag;

            GameObject containerGo = new GameObject("row", typeof(RectTransform));
            _container = (RectTransform)containerGo.transform;
            _container.SetParent(table._rect, false);
            _container.localScale = Vector3.one;
            _container.sizeDelta = new Vector2(table._rect.sizeDelta.x, 0f);
            ConfigureVertical(containerGo.AddComponent<VerticalLayoutGroup>(), 0);

            _line = CreateLine(_container, "cells");
            Image background = _line.gameObject.AddComponent<Image>();
            background.color = Color.white;
            background.raycastTarget = true;
            _button = _line.gameObject.AddComponent<Button>();
            _button.targetGraphic = background;
            _button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = _button.colors;
            colors.highlightedColor = HoverColor;
            colors.pressedColor = PressedColor;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            _button.colors = colors;
            _button.onClick.AddListener(Toggle);
            SetStriped(table._rows.Count % 2 == 1);

            GameObject arrowGo = new GameObject("arrow", typeof(RectTransform));
            _arrow = (RectTransform)arrowGo.transform;
            _arrow.SetParent(_line, false);
            _arrow.localScale = Vector3.one;
            Image arrowImage = arrowGo.AddComponent<Image>();
            arrowImage.sprite = table._arrowSprite;
            arrowImage.preserveAspect = true;
            arrowImage.color = ArrowColor;
            arrowImage.raycastTarget = false;
            arrowImage.enabled = detail.Count > 0 && table._arrowSprite != null;
            LayoutElement arrowElement = arrowGo.AddComponent<LayoutElement>();
            arrowElement.minWidth = arrowElement.preferredWidth = ArrowSize;
            arrowElement.minHeight = arrowElement.preferredHeight = ArrowSize;

            float height = RowMinHeight;
            for (int i = 0; i < cells.Length && i < table._columns.Length; i++)
            {
                MagicTextElement cell = table.CreateCell(_line, cells[i], i, MagicPages.FONT_SIZE, true);
                _texts.Add(cell);
                height = Mathf.Max(height, cell.GetHeight() + 2f * PadY);
            }

            _line.sizeDelta = new Vector2(_line.sizeDelta.x, height);

            GameObject detailGo = new GameObject("detail", typeof(RectTransform));
            _detail = (RectTransform)detailGo.transform;
            _detail.SetParent(_container, false);
            _detail.localScale = Vector3.one;
            VerticalLayoutGroup detailLayout = detailGo.AddComponent<VerticalLayoutGroup>();
            ConfigureVertical(detailLayout, Mathf.RoundToInt(PadX + DetailIndent));
            detailLayout.padding.bottom = Mathf.RoundToInt(DetailBottomPad);

            _detailLines = detail;
            _detailSearchText = detail.Select(MagicTextElement.ToSearchText).ToArray();
            _expanded = expanded;
            ApplyExpanded();
        }

        public bool IsMatch(string loweredQuery) =>
            _texts.Any(x => x.IsMatch(loweredQuery)) || _detailSearchText.Any(x => x.Contains(loweredQuery));

        public void SetStriped(bool striped)
        {
            ColorBlock colors = _button.colors;
            colors.normalColor = striped ? StripeColor : Color.clear;
            colors.selectedColor = colors.normalColor;
            colors.disabledColor = colors.normalColor;
            _button.colors = colors;
        }

        public void Enable(bool enable)
        {
            Visible = enable;
            _container.gameObject.SetActive(enable);
        }

        public void Destroy() => UnityEngine.Object.Destroy(_container.gameObject);

        public float GetHeight() => _container.sizeDelta.y;

        private void BuildDetail()
        {
            if (_detailBuilt)
            {
                return;
            }

            _detailBuilt = true;
            float detailWidth = _table._rect.sizeDelta.x - PadX - DetailIndent;
            float detailHeight = DetailBottomPad;
            foreach (string text in _detailLines)
            {
                MagicTextElement element = _table._template.Create(text, _detail, false);
                element.SetFontSize(DetailFontSize);
                element.Fit(detailWidth, 18f);
                _texts.Add(element);
                detailHeight += element.GetHeight();
            }

            _detail.sizeDelta = new Vector2(_detail.sizeDelta.x, detailHeight);
        }

        private void Toggle()
        {
            if (_detailLines.Count == 0)
            {
                return;
            }

            _expanded = !_expanded;
            ApplyExpanded();
            _table.Relayout();
        }

        public void SetExpanded(bool expanded)
        {
            if (_detailLines.Count == 0 || _expanded == expanded)
            {
                return;
            }

            _expanded = expanded;
            ApplyExpanded();
        }

        private void ApplyExpanded()
        {
            if (_expanded)
            {
                BuildDetail();
            }

            _detail.gameObject.SetActive(_expanded);
            _arrow.localRotation = Quaternion.Euler(0f, 0f, _expanded ? 0f : 90f);
            float height = _line.sizeDelta.y + (_expanded ? _detail.sizeDelta.y : 0f);
            _container.sizeDelta = new Vector2(_container.sizeDelta.x, height);
        }
    }
}
