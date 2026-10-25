using System;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.Compendium;

public class MagicPages : MonoBehaviour
{
    public ExplainTextInfo ExplainPage;
    public TreasureBountyTextInfo TreasureBountyPage;
    public MagicEffectTextInfo MagicEffectsPage;
    public SetInfo SetInfos;
    public ShardStoneTextInfo ShardStonePage;

    public const int HEADER_FONT_SIZE = 40;
    public const int LARGE_FONT_SIZE = 24;
    public const int MEDIUM_FONT_SIZE = 20;
    public const int FONT_SIZE = 18;

    private const float GAMEPAD_SCROLL_SPEED = 1000f;
    private const float GAMEPAD_STICK_DEADZONE = 0.5f;
    private const string COLLAPSE_KEY = "JoyRStick";
    private const string PREVIOUS_TYPE_KEY = "JoyTabLeft";
    private const string NEXT_TYPE_KEY = "JoyTabRight";

    public float MinWidth { get; private set; }
    public float MinHeight { get; private set; }

    public MagicSearchField Search;
    public MagicPanelButton CollapseAll;
    public MagicDropdown TypeFilter;

    // Where the item type dropdown goes, decided with the rest of the row in Awake. The dropdown itself
    // is cloned from the enchanting table prefab, which is only there once the world scene is, so it
    // is built the first time a page is shown.
    private struct Placement
    {
        public Transform Parent;
        public Vector2 Anchor;
        public Vector2 Pivot;
        public Vector2 Position;
        public Vector2 Size;
    }

    private Placement _typeFilterPlacement;

    /// <summary>Whether table rows start folded; kept across pages and dialog opens so the choice sticks.</summary>
    public static bool Collapsed;

    /// <summary>The item type the tables are narrowed to, or null for all; kept the same way as <see cref="Collapsed"/>.</summary>
    public static ItemTypeOption SelectedType;

    private const float COLLAPSE_BUTTON_WIDTH = 150f;
    private const float FILTER_BUTTON_WIDTH = 190f;
    private const float BUTTON_SPACING = 10f;
    private const float MIN_SEARCH_WIDTH = 120f;
    public MagicTextList MagicPagesTextArea;
    public GameObject compendiumTextArea;
    [CanBeNull] public MagicTextElement TitleElement;

    private bool wasGlowing;
    private MagicTextInfo _page;

    /// <summary>The dialog had the parts the pages are built into; without them this component does nothing.</summary>
    public bool IsReady { get; private set; }

    // Auga's compendium dialogs have no close button to line the search bar up with, so it runs along the
    // top of Epic Loot's text area and the page content starts below it.
    private const float AUGA_SEARCH_BAR_HEIGHT = 32f;
    private const float AUGA_SEARCH_BAR_MARGIN = 8f;

    /// <summary>The dialog whose pages are being built or shown; the text list and elements size themselves from it.</summary>
    public static MagicPages instance;

    public void Awake()
    {
        ExplainPage = new ExplainTextInfo(Localization.instance.Localize(
            $"{EpicLoot.GetMagicEffectPip(false)} $mod_epicloot_me_explaintitle"));
        TreasureBountyPage = new TreasureBountyTextInfo(Localization.instance.Localize(
            $"{EpicLoot.GetMagicEffectPip(false)} $mod_epicloot_adventure_title"));
        MagicEffectsPage = new MagicEffectTextInfo(Localization.instance.Localize(
            $"{EpicLoot.GetMagicEffectPip(false)} $mod_epicloot_active_magic_effects"));
        SetInfos = new SetInfo(Localization.instance.Localize(
            $"{EpicLoot.GetMagicEffectPip(false)} $mod_epicloot_legendary_sets"));
        ShardStonePage = new ShardStoneTextInfo(Localization.instance.Localize(
            $"{EpicLoot.GetMagicEffectPip(false)} $mod_epicloot_shardstones_title"));

        // The vanilla dialog keeps its parts in Texts_frame. Auga's compendium dialogs (AugaTextsDialog) have no
        // frame: the text area, in the same TextArea/ScrollArea/Content shape, sits under the root, and there
        // is no close button.
        Transform frame = transform.Find("Texts_frame");
        bool augaDialog = frame == null;
        if (augaDialog)
        {
            frame = transform;
        }

        Transform textArea = frame.Find("TextArea");
        if (textArea == null || textArea.Find("ScrollArea/Content") == null)
        {
            EpicLoot.LogWarning($"Compendium: {name} has no TextArea/ScrollArea/Content; Epic Loot's pages are left out of it.");
            return;
        }

        instance = this;

        compendiumTextArea = textArea.gameObject;
        RectTransform textAreaRect = compendiumTextArea.GetComponent<RectTransform>();
        MinWidth = textAreaRect.rect.width;
        MinHeight = textAreaRect.rect.height;

        if (augaDialog)
        {
            MagicPagesTextArea = new MagicTextList(compendiumTextArea, frame);
            MagicPagesTextArea.SetTopPadding(Mathf.RoundToInt(AUGA_SEARCH_BAR_HEIGHT + 2 * AUGA_SEARCH_BAR_MARGIN));
            float rowWidth = Mathf.Max(MinWidth - 40f, 200f);
            float buttonsWidth = COLLAPSE_BUTTON_WIDTH + FILTER_BUTTON_WIDTH + 2f * BUTTON_SPACING;
            float searchWidth = rowWidth - buttonsWidth;
            Search = new MagicSearchField(MagicPagesTextArea.Root);
            Search.DockTop(searchWidth, AUGA_SEARCH_BAR_HEIGHT, AUGA_SEARCH_BAR_MARGIN, -buttonsWidth / 2f);
            Search.SetBackgroundColor(new Color(0f, 0f, 0f, 0.5f));
            CollapseAll = new MagicPanelButton(MagicPagesTextArea.Root, "collapseAll", ToggleCollapsed, COLLAPSE_KEY);
            CollapseAll.Dock(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(rowWidth / 2f - COLLAPSE_BUTTON_WIDTH / 2f, -AUGA_SEARCH_BAR_MARGIN));
            CollapseAll.SetSize(COLLAPSE_BUTTON_WIDTH, AUGA_SEARCH_BAR_HEIGHT);
            CollapseAll.SetBackgroundColor(new Color(0f, 0f, 0f, 0.5f));
            _typeFilterPlacement = new Placement
            {
                Parent = MagicPagesTextArea.Root,
                Anchor = new Vector2(0.5f, 1f),
                Pivot = new Vector2(0.5f, 1f),
                Position = new Vector2(rowWidth / 2f - COLLAPSE_BUTTON_WIDTH - BUTTON_SPACING - FILTER_BUTTON_WIDTH / 2f, -AUGA_SEARCH_BAR_MARGIN),
                Size = new Vector2(FILTER_BUTTON_WIDTH, AUGA_SEARCH_BAR_HEIGHT),
            };
            Search.SetFont(MagicFontManager.GetFont(MagicFontManager.FontOptions.AveriaSerifLibre));
            Search.Input.onValueChanged.AddListener(OnSearch);
            IsReady = true;
            Reset();
            return;
        }

        Image closeButtonImg = frame.Find("Closebutton").GetComponent<Image>();
        Button closeButton = closeButtonImg.GetComponent<Button>();
        RectTransform closeButtonRect = closeButtonImg.GetComponent<RectTransform>();
        Search = new MagicSearchField(frame);

        // Everything below is measured in the frame's own space: the close button's world position cannot be
        // mixed with rect widths, which are in canvas units. The item type filter and then the search bar run
        // from the close button to the text area; the collapse toggle goes left of the close button, narrower
        // than it so it clears the topic list, or between the close button and the filter when that gap is
        // too small.
        float spacing = 16f;
        RectTransform frameRect = (RectTransform)frame;
        Vector2 frameOrigin = frameRect.rect.center;
        Vector3 closeCenter = frameRect.InverseTransformPoint(closeButtonRect.TransformPoint(closeButtonRect.rect.center));
        float closeWidth = closeButtonRect.rect.width;
        float height = closeButtonRect.rect.height;
        float closeLeft = closeCenter.x - closeWidth / 2f;
        float closeRight = closeCenter.x + closeWidth / 2f;
        float boxLeft = frameRect.InverseTransformPoint(textAreaRect.TransformPoint(new Vector3(textAreaRect.rect.xMin, 0f, 0f))).x;
        float listRight = frameRect.rect.xMin;
        RectTransform listRoot = GetComponent<TextsDialog>()?.m_listRoot;
        if (listRoot != null)
        {
            RectTransform listRect = listRoot.parent as RectTransform ?? listRoot;
            listRight = frameRect.InverseTransformPoint(listRect.TransformPoint(new Vector3(listRect.rect.xMax, 0f, 0f))).x;
        }

        float toggleWidth = Mathf.Min(closeWidth * 0.6f, closeLeft - listRight - 2f * spacing);
        float toggleCenterX;
        float searchLeft = closeRight + spacing;
        if (toggleWidth >= 90f)
        {
            toggleCenterX = closeLeft - spacing - toggleWidth / 2f;
        }
        else
        {
            toggleWidth = closeWidth * 0.6f;
            toggleCenterX = closeRight + spacing + toggleWidth / 2f;
            searchLeft = toggleCenterX + toggleWidth / 2f + spacing;
        }

        CollapseAll = new MagicPanelButton(frame, "collapseAll", ToggleCollapsed, COLLAPSE_KEY);
        CollapseAll.SetBackground(closeButtonImg);
        CollapseAll.SetSize(toggleWidth, height);
        CollapseAll.Dock(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(toggleCenterX - frameOrigin.x, closeCenter.y - frameOrigin.y));

        // The row ends at the text area when that sits to the right of the close button, and at the frame's
        // edge when the text area is above the row instead.
        float rowRight = (boxLeft > searchLeft ? boxLeft : frameRect.rect.xMax) - spacing;
        float width = Mathf.Max(boxLeft - spacing - searchLeft, MIN_SEARCH_WIDTH);
        Search.Dock(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(searchLeft + width / 2f - frameOrigin.x, closeCenter.y - frameOrigin.y));
        Search.SetSize(width, height);

        float filterLeft = searchLeft + width + spacing;
        float filterWidth = Mathf.Clamp(rowRight - filterLeft, 0f, closeWidth * 0.9f);
        _typeFilterPlacement = new Placement
        {
            Parent = frame,
            Anchor = new Vector2(0.5f, 0.5f),
            Pivot = new Vector2(0.5f, 0.5f),
            Position = new Vector2(filterLeft + filterWidth / 2f - frameOrigin.x, closeCenter.y - frameOrigin.y),
            Size = new Vector2(filterWidth, height),
        };

        Search.SetBackground(closeButtonImg);
        Search.SetBackground(closeButton.spriteState.disabledSprite);
        Search.SetFont(MagicFontManager.GetFont(MagicFontManager.FontOptions.AveriaSerifLibre));
        Search.Input.onValueChanged.AddListener(OnSearch);

        MagicPagesTextArea = new MagicTextList(compendiumTextArea, frame);
        IsReady = true;
        Reset();
    }

    public void Update()
    {
        if (!IsReady)
        {
            return;
        }

        UpdateGamepadScroll();
        UpdateButtons();

        //  makes search field glow when focused
        if (wasGlowing && !InSearchField())
        {
            Search.EnableGlow(false);
            wasGlowing = false;
        }
        else if (!wasGlowing && InSearchField())
        {
            Search.EnableGlow(true);
            wasGlowing = true;
        }
    }

    private void UpdateButtons()
    {
        bool gamepad = ZInput.IsGamepadActive();
        bool takeInput = gamepad && MagicPagesTextArea.IsEnabled;
        if (CollapseAll != null && CollapseAll.IsEnabled)
        {
            CollapseAll.ShowHint(gamepad);
            if (takeInput && ZInput.GetButtonDown(COLLAPSE_KEY))
            {
                ToggleCollapsed();
            }
        }

        if (TypeFilter != null && TypeFilter.IsEnabled)
        {
            TypeFilter.ShowHint(gamepad);
            if (takeInput && ZInput.GetButtonDown(NEXT_TYPE_KEY))
            {
                CycleItemType(1);
            }
            else if (takeInput && ZInput.GetButtonDown(PREVIOUS_TYPE_KEY))
            {
                CycleItemType(-1);
            }
        }
    }

    public void ToggleCollapsed()
    {
        Collapsed = !Collapsed;
        UpdateCollapseLabel();
        MagicPagesTextArea.SetAllExpanded(!Collapsed);
    }

    private void UpdateCollapseLabel() =>
        CollapseAll?.SetLabel(Collapsed ? "$mod_epicloot_expand_all" : "$mod_epicloot_collapse_all");

    /// <summary>Moves the item type filter <paramref name="step"/> places through all types, wrapping past the ends.</summary>
    public void CycleItemType(int step)
    {
        ItemTypeOption[] options = ItemTypeFilter.Options;
        int count = options.Length + 1;
        int position = SelectedType == null ? 0 : Array.IndexOf(options, SelectedType) + 1;
        position = ((position + step) % count + count) % count;
        SelectedType = position == 0 ? null : options[position - 1];
        UpdateTypeFilterValue();
        ApplyFilters();
    }

    private void OnTypeFilterChanged(int index)
    {
        SelectedType = index <= 0 ? null : ItemTypeFilter.Options[index - 1];
        ApplyFilters();
    }

    private void UpdateTypeFilterValue() =>
        TypeFilter?.SetValue(SelectedType == null ? 0 : Array.IndexOf(ItemTypeFilter.Options, SelectedType) + 1);

    private void EnsureTypeFilter()
    {
        if (TypeFilter != null || _typeFilterPlacement.Parent == null)
        {
            return;
        }

        TypeFilter = MagicDropdown.TryCreate(_typeFilterPlacement.Parent, "itemTypeFilter", OnTypeFilterChanged,
            PREVIOUS_TYPE_KEY, NEXT_TYPE_KEY);
        if (TypeFilter == null)
        {
            return;
        }

        TypeFilter.Dock(_typeFilterPlacement.Anchor, _typeFilterPlacement.Pivot, _typeFilterPlacement.Position);
        TypeFilter.SetSize(_typeFilterPlacement.Size.x, _typeFilterPlacement.Size.y);
        TypeFilter.SetFontSize(FONT_SIZE);
        TypeFilter.SetOptions(new[] { "$mod_epicloot_setrune_anyitem" }.Concat(ItemTypeFilter.Options.Select(x => x.Name)));
    }

    private void UpdateGamepadScroll()
    {
        if (!MagicPagesTextArea.IsEnabled || !ZInput.IsGamepadActive())
        {
            return;
        }

        float rightStickAxis = ZInput.GetJoyRightStickY();
        if (Mathf.Abs(rightStickAxis) > GAMEPAD_STICK_DEADZONE)
        {
            MagicPagesTextArea.ScrollBy(-rightStickAxis * GAMEPAD_SCROLL_SPEED * Time.unscaledDeltaTime);
        }
    }

    public static bool InSearchField()
    {
        if (instance == null || !instance.IsReady || instance.Search == null)
        {
            return false;
        }

        return instance.Search.Input.isFocused;
    }

    public void OnSearch(string query) => ApplyFilters();

    /// <summary>Narrows the page to the search text and, where the page supports it, the selected item type.</summary>
    public void ApplyFilters()
    {
        string query = Search.Input.text.Trim();
        Func<object, bool> include = null;
        if (SelectedType != null && _page != null && _page.HasItemTypeFilter)
        {
            MagicTextInfo page = _page;
            ItemTypeOption type = SelectedType;
            include = tag => page.AllowsItemType(tag, type);
        }

        foreach (MagicTextGroup element in MagicPagesTextArea.Elements)
        {
            element.Filter(query, include);
        }
    }

    public void Reset()
    {
        if (!IsReady)
        {
            return;
        }

        // One retry per open for anything that missed because its asset was not loaded yet; the lookup
        // scans every loaded object, so it must not run per line.
        MagicFontManager.RetryFailedLookups();

        _page = null;
        compendiumTextArea.SetActive(true);
        Search.Enable(false);
        CollapseAll?.Enable(false);
        TypeFilter?.Enable(false);
        MagicPagesTextArea.Enable(false);
        MagicPagesTextArea.Clear();
        TitleElement?.Destroy();
    }

    public void OnSelectText(MagicTextInfo text)
    {
        if (!IsReady)
        {
            return;
        }

        instance = this;
        _page = text;
        compendiumTextArea.SetActive(false);
        MagicPagesTextArea.Enable(true);
        Search.Enable(text.ShowSearchBar);
        Search.Input.SetTextWithoutNotify(string.Empty);

        TitleElement = MagicPagesTextArea.Create(text.m_topic);
        TitleElement!.SetSize(MinWidth, 100f);
        TitleElement.SetFontSize(HEADER_FONT_SIZE);
        TitleElement.SetColor(new Color(1f, 0.65f, 0.15f));
        TitleElement.SetAlignment(TextAnchor.MiddleCenter);

        text.Build(this);
        MagicPagesTextArea.ResizeOverlay();
        if (CollapseAll != null)
        {
            UpdateCollapseLabel();
            CollapseAll.Enable(MagicPagesTextArea.HasTables);
        }

        EnsureTypeFilter();
        if (TypeFilter != null)
        {
            UpdateTypeFilterValue();
            TypeFilter.Enable(text.ShowSearchBar && text.HasItemTypeFilter);
        }

        if (SelectedType != null && text.HasItemTypeFilter)
        {
            ApplyFilters();
        }
    }
}
