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

    public float MinWidth { get; private set; }
    public float MinHeight { get; private set; }

    public MagicSearchField Search;
    public MagicTextList MagicPagesTextArea;
    public GameObject compendiumTextArea;
    [CanBeNull] public MagicTextElement TitleElement;

    private bool wasGlowing;

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
            Search = new MagicSearchField(MagicPagesTextArea.Root);
            Search.DockTop(Mathf.Max(MinWidth - 40f, 200f), AUGA_SEARCH_BAR_HEIGHT, AUGA_SEARCH_BAR_MARGIN);
            Search.SetBackgroundColor(new Color(0f, 0f, 0f, 0.5f));
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

        // Calculate the Seach bar position
        float spacing = 30f;
        float buttonEdge = closeButtonRect.position.x + (closeButtonRect.rect.width / 2); // Button is centered
        float boxEdge = textAreaRect.position.x; // Box is anchored to the bottom right
        float width = boxEdge - buttonEdge - spacing;
        float height = closeButtonRect.rect.height;
        Vector3 position = new Vector3(buttonEdge + (width / 2) + spacing, closeButtonRect.position.y);
        Search.SetPosition(position);
        Search.SetSize(width, height);

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

    public void OnSearch(string query)
    {
        foreach (MagicTextGroup element in MagicPagesTextArea.Elements)
        {
            element.Enable(element.IsMatch(query.Trim()));
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

        compendiumTextArea.SetActive(true);
        Search.Enable(false);
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
    }
}
