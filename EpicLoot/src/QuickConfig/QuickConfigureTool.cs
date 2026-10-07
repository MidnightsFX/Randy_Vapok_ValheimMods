using Common;
using EpicLoot.Config;
using HarmonyLib;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EpicLoot.QuickConfig;

// A paged editor over the settings most worlds want to change. Opened from the shared Mod Config
// launcher, or once on the main menu as the first-time setup, which adds a welcome page in front.
// Every page can be saved, and the X closes from anywhere. The pages are prefabs from the epicloot
// bundle (see README.md in this folder); this file is the shell: opening, navigation, saving, and the
// staged state the pages edit.
internal static class QuickConfigureTool {
    private const string LauncherEntry = "Epic Loot";
    internal const string PanelAsset = "QuickConfigPanel";

    /// <summary>Page prefabs in display order. Page_Welcome is only shown in tutorial mode.</summary>
    internal static readonly string[] PageOrder = {
        "Page_Welcome", "Page_Balance", "Page_Rarity", "Page_LootDrops", "Page_Shardstones",
        "Page_EnchantingTable", "Page_Merchant", "Page_Bounties", "Page_Interface", "Page_ItemColors",
        "Page_EffectTuning", "Page_Advanced"
    };

    /// <summary>
    /// The pages shown until Show Advanced Configs is pressed: the welcome page in first-time setup, and
    /// the Balance page. Every other page is advanced. They come first in <see cref="PageOrder"/>, so a
    /// hidden page is always after every shown one.
    /// </summary>
    private static readonly HashSet<string> BasicPages = new HashSet<string>(StringComparer.Ordinal) {
        "Page_Welcome", "Page_Balance"
    };

    /// <summary>The row keys of the Show / Hide Advanced Configs buttons (on the Balance page).</summary>
    internal const string ShowAdvancedKey = "action:advanced:show";
    internal const string HideAdvancedKey = "action:advanced:hide";

    private sealed class PageInstance {
        internal string Name;
        internal string Title;
        internal GameObject Root;
        internal RowBinder Binder;
    }

    // --- runtime state ---
    private static GameObject panelRoot;
    // Every page that was built, and the ones in the Back/Next sequence; currentPage indexes the latter.
    private static List<PageInstance> pages;
    private static List<PageInstance> shownPages;
    private static bool advancedToggleMissing;
    private static int currentPage;
    private static bool tutorialMode;
    private static TMP_Text titleText;
    private static TMP_Text statusText;
    private static GameObject backBtn;
    private static GameObject nextBtn;
    private static GameObject finishBtn;
    private static GameObject resetBtn;
    private static TMP_Text nextCaption;
    private static bool discardArmed;
    private static bool overhaulBackupConfirmed;

    // What the pages edit, and what was live when the panel opened (or was last saved).
    private static StagedConfig staged;
    private static StagedConfig baseline;

    internal static bool IsOpen => panelRoot != null;

    internal static string L(string text) {
        if (string.IsNullOrEmpty(text)) { return ""; }
        return Localization.instance != null ? Localization.instance.Localize(text) : text;
    }

    internal static void Init() {
        // The corner button, the main-menu hook and the pause-menu patch belong to the shared launcher
        // in Common/src/Config/UI, so several mods share one button. See its README for the contract.
        ConfigUILauncher.Init();
        ApplyRegistration();
        QueueWelcomeWizard();
    }

    // Also the SettingChanged handler for ShowQuickConfigButton.
    internal static void ApplyRegistration() {
        if (ELConfig.ShowQuickConfigButton == null || ELConfig.ShowQuickConfigButton.Value == false) {
            ConfigUILauncher.Unregister(LauncherEntry);
            return;
        }

        // Resolved once, up front, so the warning about a missing Jotunn method shows at startup rather
        // than on the first save. The entry is offered either way: a host still works without the push
        // (the .cfg reload scheduler carries its changes to peers on its next poll), and a remote admin
        // is told on save that the server could not be reached.
        CanPushRemoteConfig();
        ConfigUILauncher.Register(LauncherEntry, OpenPanel);
    }

    private static bool IsHost() => QuickConfigBindings.IsHost();

    private static MethodInfo syncChangedConfig;
    private static bool syncChangedConfigResolved;

    private static bool CanPushRemoteConfig() {
        if (syncChangedConfigResolved == false) {
            syncChangedConfigResolved = true;
            syncChangedConfig = AccessTools.Method(typeof(SynchronizationManager), "SynchronizeChangedConfig");
            if (syncChangedConfig == null) {
                EpicLoot.LogWarningForce("Jotunn's SynchronizeChangedConfig could not be found. A remote admin " +
                    "cannot push Quick Configure changes to the server, and a host's changes reach its peers " +
                    "only on the next .cfg reload.");
            }
        }
        return syncChangedConfig != null;
    }

    // Reflection into a private Jotunn method, knowingly: it is the only way changed admin entries
    // reach the peers (host) or the server (remote admin) without opening the ConfigurationManager
    // window. Jotunn's own trigger for it is that window closing, or a ConfigFile reload.
    private static void PushRemoteConfigChanges(List<string> failures) {
        if (CanPushRemoteConfig() == false) {
            // A host is not stuck: the .cfg reload scheduler pushes the file on its next poll.
            if (IsHost()) { return; }
            failures.Add("Server settings could not be sent to the server (Jotunn's sync method is missing).");
            return;
        }
        try {
            syncChangedConfig.Invoke(SynchronizationManager.Instance, null);
        } catch (Exception e) {
            EpicLoot.LogWarningForce($"Could not push config changes to the server: {e.Message}");
            failures.Add($"Server settings could not be sent to the server: {e.Message}");
        }
    }

    // ------------------------------------------------------------------------------------------------
    //  First-time setup
    // ------------------------------------------------------------------------------------------------

    // Queued once, from Init, on the startup popup queue every mod carrying Common/src/Config/UI shares:
    // it waits for the main menu to settle and opens the popups one at a time, so this never lands on
    // top of another mod's welcome, or of the config update prompt queued behind it. Whether this user
    // has seen it is kept per user rather than per profile; see ConfigUIFirstRun.
    private static void QueueWelcomeWizard() {
        ConfigUIFirstRun.QueueFirstRunPopup(ELConfig.WelcomeWizardKey, ELConfig.WelcomeWizardRevision,
            ELConfig.WelcomeWizardMode, ConfigUIStartupPopups.OrderWelcome + 10, OpenWelcomeWizard, () => IsOpen);
    }

    // Declines when the panel is already up: someone reached it through Mod Config while this waited
    // its turn.
    private static bool OpenWelcomeWizard() {
        if (IsOpen) { return false; }
        OpenPanel(tutorial: true);
        return IsOpen;
    }

    // ------------------------------------------------------------------------------------------------
    //  Panel
    // ------------------------------------------------------------------------------------------------

    // The launcher's entry point.
    internal static void OpenPanel() {
        OpenPanel(tutorial: false);
    }

    internal static void OpenPanel(bool tutorial) {
        if (GUIManager.IsHeadless() || GUIManager.Instance == null || GUIManager.CustomGUIFront == null) { return; }
        // Built fresh every time, so every widget starts from the current configuration.
        DestroyPanel();
        tutorialMode = tutorial;
        staged = StagedConfig.Snapshot();
        baseline = StagedConfig.Snapshot();
        bool built;
        try {
            built = BuildPanel();
        } catch (Exception e) {
            EpicLoot.LogErrorForce($"Quick Configure failed to build its panel: {e}");
            built = false;
        }
        if (built == false) {
            DestroyPanel();
            return;
        }
        ShowPage(0);
    }

    private static bool BuildPanel() {
        GameObject prefab = EpicLoot.LoadAsset<GameObject>(PanelAsset);
        if (prefab == null) {
            EpicLoot.LogErrorForce($"The {PanelAsset} prefab is missing from the asset bundle; Quick Configure cannot open.");
            return false;
        }

        // The prefab's full-screen Overlay dims and blocks clicks under the panel. The root owns the
        // input block and, on the main menu, hides the menu itself (MainMenuGuard); both are released
        // when it is destroyed, whatever route that takes.
        panelRoot = UnityEngine.Object.Instantiate(prefab, GUIManager.CustomGUIFront.transform, false);
        panelRoot.name = "EpicLootQuickConfigure";
        QuickConfigUi.Stretch(panelRoot);
        panelRoot.AddComponent<ConfigUI.ConfigUIInputGuard>().Hold();
        panelRoot.AddComponent<MainMenuGuard>();
        panelRoot.AddComponent<EscapeCloser>();

        Transform panel = QuickConfigUi.FindChild(panelRoot.transform, "Panel") ?? panelRoot.transform;
        titleText = QuickConfigUi.TextAt(panel, "Title");
        statusText = QuickConfigUi.TextAt(panel, "Status");
        Transform pagesContainer = QuickConfigUi.FindChild(panel, "Pages") ?? panel;

        Button close = QuickConfigUi.FindComponent<Button>(panel, "Close");
        if (close != null) {
            // The prefab's caption is a bare "X" in a 30px button; the localized word goes on the tooltip only.
            QuickConfigUi.Wire(close, RequestClose);
            QuickConfigTooltip.Attach(close.gameObject, QuickConfigTooltip.Text(L("$mod_epicloot_cfg_close"),
                "Closes the panel. If anything has not been saved yet you are asked about it first."));
        }
        backBtn = WireNav(panel, "Back", "$mod_epicloot_cfg_back", () => ShowPage(currentPage - 1),
            "The previous page. Moving between pages keeps your edits; only Save writes them.");
        WireNav(panel, "Save", "$mod_epicloot_cfg_save", OnSaveClicked,
            "Writes every page's changes: the .cfg entries and, on the host, the JSON files behind them. The panel stays open.");
        nextBtn = WireNav(panel, "Next", "$mod_epicloot_cfg_next", () => ShowPage(currentPage + 1),
            "The next page. Moving between pages keeps your edits; only Save writes them.");
        nextCaption = nextBtn != null ? QuickConfigUi.CaptionOf(nextBtn.GetComponent<Button>()) : null;
        finishBtn = WireNav(panel, "Finish", "$mod_epicloot_cfg_finish", OnFinishClicked,
            "Saves everything and closes the panel.");
        resetBtn = WireNav(panel, "Reset", "$mod_epicloot_cfg_reset_page", OnResetPageClicked,
            "Puts every setting on this page back to the value Epic Loot ships with. Nothing is written until you save.");

        pages = new List<PageInstance>();
        foreach (string pageName in PageOrder) {
            if (pageName == "Page_Welcome" && tutorialMode == false) { continue; }
            GameObject pagePrefab = EpicLoot.LoadAsset<GameObject>(pageName);
            if (pagePrefab == null) {
                EpicLoot.LogWarningForce($"The {pageName} prefab is missing from the asset bundle; the page is skipped.");
                continue;
            }
            GameObject pageRoot = UnityEngine.Object.Instantiate(pagePrefab, pagesContainer, false);
            pageRoot.name = pageName;
            QuickConfigUi.Stretch(pageRoot);
            PageInstance page = new PageInstance {
                Name = pageName,
                Title = PageTitle(pageName, pageRoot),
                Root = pageRoot,
                Binder = new RowBinder(pageRoot, staged, OnEdited)
            };
            page.Binder.Bind();
            pages.Add(page);
        }
        if (pages.Count == 0) {
            EpicLoot.LogErrorForce("None of the Quick Configure page prefabs are in the asset bundle; the panel cannot open.");
            return false;
        }

        // A bundle whose basic pages have no Show Advanced Configs row would leave the other pages
        // unreachable, so it gets every page.
        advancedToggleMissing = true;
        foreach (PageInstance page in pages) {
            if (BasicPages.Contains(page.Name) && QuickConfigUi.FindChild(page.Root.transform, ShowAdvancedKey) != null) {
                advancedToggleMissing = false;
            }
        }
        UpdateShownPages();

        QuickConfigStyle.Apply(panelRoot);
        RefreshAll();
        return true;
    }

    private static GameObject WireNav(Transform panel, string name, string token, Action onClick, string tooltip) {
        Button button = QuickConfigUi.FindComponent<Button>(panel, name);
        if (button == null) {
            EpicLoot.LogWarningForce($"The {PanelAsset} prefab has no '{name}' button.");
            return null;
        }
        QuickConfigUi.SetCaption(button, token);
        QuickConfigUi.Wire(button, () => onClick());
        QuickConfigTooltip.Attach(button.gameObject, QuickConfigTooltip.Text(L(token), tooltip));
        return button.gameObject;
    }

    // "Page_Balance" -> "$mod_epicloot_cfg_page_balance"; an inactive Title child on the page overrides it.
    private static string PageTitle(string pageName, GameObject pageRoot) {
        TMP_Text override_ = QuickConfigUi.TextAt(pageRoot.transform, "Title");
        if (override_ != null && string.IsNullOrWhiteSpace(override_.text) == false) { return L(override_.text); }
        string id = pageName.StartsWith("Page_", StringComparison.Ordinal) ? pageName.Substring(5) : pageName;
        string token = "$mod_epicloot_cfg_page_" + id.ToLowerInvariant();
        string localized = L(token);
        return string.IsNullOrEmpty(localized) || localized == token ? id : localized;
    }

    private static void ShowPage(int page) {
        if (shownPages == null || shownPages.Count == 0) { return; }
        currentPage = Mathf.Clamp(page, 0, shownPages.Count - 1);
        PageInstance shown = shownPages[currentPage];
        foreach (PageInstance instance in pages) {
            instance.Root.SetActive(instance == shown);
        }
        if (titleText != null) {
            titleText.text = $"Epic Loot - {shown.Title}  (Page {currentPage + 1} of {shownPages.Count})";
        }

        if (backBtn != null) { backBtn.SetActive(currentPage > 0); }
        bool last = currentPage == shownPages.Count - 1;
        if (nextBtn != null) { nextBtn.SetActive(last == false); }
        if (finishBtn != null) { finishBtn.SetActive(last); }
        if (nextCaption != null) {
            nextCaption.text = L(tutorialMode && currentPage == 0 ? "$mod_epicloot_cfg_getstarted" : "$mod_epicloot_cfg_next");
        }

        if (resetBtn != null) { resetBtn.SetActive(shown.Binder.ResettableKeys().Count > 0); }
        shown.Binder.Refresh();
        if (shown.Binder.HasReadOnlyRows && IsHost() == false) {
            SetStatus(L("$mod_epicloot_cfg_readonly_offhost"), true);
        } else {
            SetStatus("", true);
        }
    }

    // ------------------------------------------------------------------------------------------------
    //  Advanced pages
    // ------------------------------------------------------------------------------------------------

    /// <summary>Whether every page is in the Back/Next sequence, or only <see cref="BasicPages"/>.</summary>
    internal static bool AdvancedPagesShown => advancedToggleMissing
        || ELConfig.ShowAdvancedQuickConfigPages == null || ELConfig.ShowAdvancedQuickConfigPages.Value;

    // Pressed on the Balance page. It is a view preference rather than a staged setting, so it takes
    // effect, and is written to the .cfg, at once. Edits on pages it hides stay staged and are saved.
    internal static void SetAdvancedPagesShown(bool shown) {
        if (ELConfig.ShowAdvancedQuickConfigPages != null) {
            ELConfig.ShowAdvancedQuickConfigPages.Value = shown;
        }
        if (pages == null || shownPages == null) { return; }

        PageInstance current = currentPage >= 0 && currentPage < shownPages.Count ? shownPages[currentPage] : null;
        UpdateShownPages();
        int index = current != null ? shownPages.IndexOf(current) : -1;
        // A page that was just hidden comes after every page still shown.
        ShowPage(index >= 0 ? index : shownPages.Count - 1);
    }

    private static void UpdateShownPages() {
        bool all = AdvancedPagesShown;
        shownPages = new List<PageInstance>();
        foreach (PageInstance page in pages) {
            if (all || BasicPages.Contains(page.Name)) { shownPages.Add(page); }
        }
        if (shownPages.Count == 0) { shownPages.AddRange(pages); }
    }

    private static void SetStatus(string message, bool ok) {
        if (statusText == null) { return; }
        statusText.text = message ?? "";
        if (GUIManager.Instance != null) {
            statusText.color = ok ? GUIManager.Instance.ValheimBeige : GUIManager.Instance.ValheimOrange;
        }
    }

    // Any row edit: conditional rows reflow, readouts recompute, and the widgets re-read the staged
    // values (a preset button changes several rows at once).
    private static void OnEdited(string message) {
        discardArmed = false;
        RefreshAll();
        if (string.IsNullOrEmpty(message) == false) { SetStatus(message, true); }
    }

    private static void RefreshAll() {
        if (pages == null) { return; }
        foreach (PageInstance page in pages) { page.Binder.Refresh(); }
    }

    // Asks first: the reset replaces whatever this page has staged, saved or not.
    private static void OnResetPageClicked() {
        if (shownPages == null || currentPage < 0 || currentPage >= shownPages.Count) { return; }
        PageInstance page = shownPages[currentPage];
        bool asked = QuickConfigConfirm.Show("$mod_epicloot_cfg_reset_title", "$mod_epicloot_cfg_reset_body",
            "$mod_epicloot_cfg_keep_editing", null, "$mod_epicloot_cfg_reset_confirm",
            onKeep: null,
            onDiscard: null,
            onSaveClose: () => ResetPage(page));
        if (asked == false) { ResetPage(page); }
    }

    private static void ResetPage(PageInstance page) {
        if (staged == null || page == null) { return; }
        int changed = QuickConfigDefaults.Reset(staged, page.Binder.ResettableKeys());
        discardArmed = false;
        RefreshAll();
        SetStatus(L(changed > 0 ? "$mod_epicloot_cfg_reset_done" : "$mod_epicloot_cfg_reset_nothing"), true);
    }

    // The X. Unsaved changes get a chance to be kept.
    private static void RequestClose() {
        if (staged != null && baseline != null && staged.Matches(baseline) == false) {
            if (ShowDiscardConfirm()) { return; }
            // No confirm prefab: say so, and let a second close discard.
            if (discardArmed == false) {
                discardArmed = true;
                SetStatus(L("$mod_epicloot_cfg_discard_body"), false);
                return;
            }
        }
        ClosePanel();
    }

    // Escape (or the gamepad's back button) closes the topmost thing: the picker, the confirm prompt,
    // else the panel, which asks first when there are unsaved edits. Called every frame by the panel
    // root and by the Menu.Update prefix, whichever runs first; both then report the key as taken, so
    // the pause menu under the panel does not also act on it.
    private static int escapeFrame = -1;
    // Whether a text box had focus at the end of the last frame. See EditingText.
    private static bool textFocusedLastFrame;

    internal static bool TakeEscape() {
        // The frame after one that was taken counts as taken too: a gamepad button reads as pressed
        // until ZInput next updates (from Game.Update), which can fall on either side of the two
        // callers, so the next frame's first caller could see the same press again.
        if (escapeFrame >= 0 && Time.frameCount - escapeFrame <= 1) { return true; }
        if (panelRoot == null) { return false; }
        bool escape = ZInput.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyButtonB");
        // The pause menu also hides on the gamepad's menu button. The panel does not act on it, but
        // the menu must not either: once hidden it cannot come back while the input block is up.
        bool menuButton = ZInput.GetButtonDown("JoyMenu");
        if (escape == false && menuButton == false) { return false; }
        escapeFrame = Time.frameCount;
        // Keys that belong to something on top: the console closes itself on Escape, a text box
        // cancels its edit.
        if (escape == false || global::Console.IsVisible() || UnifiedPopup.IsVisible() || EditingText()) { return true; }
        if (QuickConfigPicker.IsOpen) {
            QuickConfigPicker.Close();
        } else if (QuickConfigConfirm.IsOpen) {
            QuickConfigConfirm.Close();
        } else {
            RequestClose();
        }
        return true;
    }

    // Checked against the end of the last frame as well as now: the text box handles Escape itself
    // from the EventSystem's update, which drops its focus the moment it cancels the edit, and that
    // update may already have run this frame by the time the key is checked here.
    private static bool EditingText() {
        return textFocusedLastFrame || TextFieldFocused();
    }

    private static bool TextFieldFocused() {
        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected == null) { return false; }
        TMP_InputField tmp = selected.GetComponent<TMP_InputField>();
        if (tmp != null) { return tmp.isFocused; }
        InputField legacy = selected.GetComponent<InputField>();
        return legacy != null && legacy.isFocused;
    }

    private class EscapeCloser : MonoBehaviour {
        public void Update() {
            TakeEscape();
        }

        public void LateUpdate() {
            textFocusedLastFrame = TextFieldFocused();
        }
    }

    // Closing by any route finishes the first-time setup: the welcome page promises that the X is enough.
    // Only the shared record is written: a "show it again next launch" ticked on the Advanced page and
    // saved stays as it was saved.
    private static void ClosePanel() {
        if (tutorialMode) {
            ConfigUIFirstRun.MarkSeen(ELConfig.WelcomeWizardKey, ELConfig.WelcomeWizardRevision);
        }
        DestroyPanel();
    }

    private static void DestroyPanel() {
        QuickConfigConfirm.Close();
        QuickConfigPicker.Close();
        QuickConfigTooltip.Close();
        if (panelRoot != null) {
            UnityEngine.Object.Destroy(panelRoot);
        }
        panelRoot = null;
        pages = null;
        shownPages = null;
        advancedToggleMissing = false;
        titleText = null;
        statusText = null;
        backBtn = null;
        nextBtn = null;
        finishBtn = null;
        resetBtn = null;
        nextCaption = null;
        tutorialMode = false;
        textFocusedLastFrame = false;
        discardArmed = false;
        overhaulBackupConfirmed = false;
    }

    private static bool ShowDiscardConfirm() {
        return QuickConfigConfirm.Show("$mod_epicloot_cfg_discard_title", "$mod_epicloot_cfg_discard_body",
            "$mod_epicloot_cfg_keep_editing", "$mod_epicloot_cfg_discard", "$mod_epicloot_cfg_save_close",
            onKeep: null,
            onDiscard: ClosePanel,
            onSaveClose: OnFinishClicked);
    }

    // Hides the main menu while the panel is up and restores it when the panel goes. FejdStartup
    // keeps reading the keyboard while its menu is visible: Enter in one of the panel's value boxes
    // would start the game underneath, and the arrow keys and gamepad pull focus back onto the menu
    // buttons. Vanilla's own Settings panel does the same. Tied to OnDestroy, like
    // ConfigUIInputGuard, so no close path can leave the menu hidden.
    private class MainMenuGuard : MonoBehaviour {
        private GameObject hiddenMenu;

        public void Awake() {
            FejdStartup startup = FejdStartup.instance;
            if (startup != null && startup.m_mainMenu != null && startup.m_mainMenu.activeSelf) {
                hiddenMenu = startup.m_mainMenu;
                hiddenMenu.SetActive(false);
            }
        }

        public void OnDestroy() {
            if (hiddenMenu != null) {
                hiddenMenu.SetActive(true);
            }
            hiddenMenu = null;
        }
    }

    // ------------------------------------------------------------------------------------------------
    //  Save
    // ------------------------------------------------------------------------------------------------

    private static void OnSaveClicked() {
        TrySave(closeAfter: false);
    }

    private static void OnFinishClicked() {
        TrySave(closeAfter: true);
    }

    // A preset over a hand-edited magiceffects.json asks first; the save resumes from the prompt.
    private static void TrySave(bool closeAfter) {
        if (staged == null || baseline == null) { return; }
        if (overhaulBackupConfirmed == false
            && BalancePreset.NeedsOverhaulRewrite(staged, baseline)
            && ConfigVersionManager.IsPlayerModified(BalancePreset.MagicEffectsConfigName)) {
            bool shown = QuickConfigConfirm.Show("$mod_epicloot_cfg_backup_confirm_title", "$mod_epicloot_cfg_backup_confirm_body",
                "$mod_epicloot_cfg_keep_editing", null, "$mod_epicloot_cfg_backup_continue",
                onKeep: null,
                onDiscard: null,
                onSaveClose: () => {
                    overhaulBackupConfirmed = true;
                    TrySave(closeAfter);
                });
            if (shown) { return; }
            // No confirm prefab: still back the file up, which is the safe half of the question.
            overhaulBackupConfirmed = true;
        }

        if (SaveStaged(out string message)) {
            if (closeAfter) {
                ClosePanel();
            } else {
                string saved = L("$mod_epicloot_cfg_saved");
                SetStatus(string.IsNullOrEmpty(message) ? saved : saved + " " + message, true);
            }
        } else {
            SetStatus(L("$mod_epicloot_cfg_saving_failed") + " " + message, false);
        }
    }

    // Applies everything staged. Returns false with the reasons when anything was refused; the panel
    // stays open with the edits intact either way, since a half-applied save closing its window is how
    // an admin ends up believing it landed. On success the message carries the notes.
    private static bool SaveStaged(out string message) {
        message = "";
        if (staged == null || baseline == null) { return false; }

        string invalid = staged.ValidationError();
        if (invalid != null) {
            message = invalid;
            return false;
        }

        List<string> failures = new List<string>();
        List<string> notes = new List<string>();
        HashSet<string> writtenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. The .cfg entries, in one batch and one write. Only entries whose staged value differs
        //    from the baseline are set, so nothing that moved while the panel was open is clobbered.
        int written = 0;
        try {
            ELConfig.cfg.SaveOnConfigSet = false;
            written = staged.ApplyTo(baseline);
        } catch (Exception e) {
            EpicLoot.LogErrorForce($"Quick Configure failed to apply the .cfg entries: {e}");
            failures.Add($"Settings were not saved: {e.Message}");
        } finally {
            ELConfig.cfg.SaveOnConfigSet = true;
            try {
                ELConfig.cfg.Save();
            } catch (Exception e) {
                EpicLoot.LogErrorForce($"Quick Configure could not write the .cfg file: {e}");
                failures.Add($"The .cfg file could not be written: {e.Message}");
            }
        }

        // 2. Host only: the balance preset's overhaul rewrite, then each changed JSON file
        //    (magiceffects.json last, so a rewrite never races an edit of the same file).
        if (IsHost()) {
            if (BalancePreset.NeedsOverhaulRewrite(staged, baseline)) {
                if (BalancePreset.ApplyOverhaulRewrite(overhaulBackupConfirmed, out string rewriteMessage)) {
                    notes.Add(rewriteMessage);
                    staged.PresetPressed = null;
                    // The file is new; staged Effect Tuning values now come from it, not the old copy.
                    // The baseline takes it too, so the fresh template is not written back over itself
                    // as an edit; staged shard effects still differ from it and are saved below.
                    BalancePreset.ReloadMagicEffects(staged);
                    BalancePreset.ReloadMagicEffects(baseline);
                } else {
                    failures.Add(rewriteMessage);
                }
            }
            JsonConfigEdits.ApplyDirty(staged, baseline, failures, notes, writtenFiles);
        } else {
            staged.PresetPressed = null;
        }

        // 3. Changed admin entries only travel when Jotunn is told: from a host to its peers, from a
        //    remote admin to the server. Nothing else pushes them (no SettingChanged hook; Jotunn's own
        //    trigger is the ConfigurationManager window closing).
        if (ZNet.instance != null && written > 0) {
            PushRemoteConfigChanges(failures);
            if (IsHost() == false && failures.Count == 0) { notes.Add(L("$mod_epicloot_cfg_sent_to_server")); }
        }

        overhaulBackupConfirmed = false;

        // 4. What was just written is now the live configuration, so it is the new point unsaved
        //    changes are measured from. Rows that failed to write keep their staged values and stay
        //    marked unsaved.
        baseline = StagedConfig.Snapshot();
        if (failures.Count == 0) {
            staged.CopyFrom(baseline);
        } else {
            // Rows of a file that was written take what landed, which for a weight table is the
            // rebalanced one: kept as typed, a retry would rebalance it against the new baseline and
            // undo the edit.
            staged.TakeFrom(baseline, slot => slot is JsonSlot json && writtenFiles.Contains(json.File));
        }
        RefreshAll();

        if (failures.Count > 0) {
            message = string.Join(" ", failures);
            return false;
        }

        EpicLoot.Log($"Quick Configure applied and saved the configuration ({written} .cfg entries).");
        message = string.Join(" ", notes);
        return true;
    }
}
