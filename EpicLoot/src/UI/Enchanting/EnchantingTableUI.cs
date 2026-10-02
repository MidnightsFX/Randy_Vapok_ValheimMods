using System;
using System.Collections.Generic;
using EpicLoot.CraftingV2;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public class EnchantingTableUI : MonoBehaviour
    {
        public GameObject Root;
        public GameObject Scrim;
        public TabHandler TabHandler;
        public GameObject TabScrim;

        [Header("Content")]
        public EnchantingTableUIPanelBase[] Panels;

        [Header("Audio")]
        public AudioSource Audio;
        public AudioClip TabClickSFX;
        public AudioClip EnchantBonusSFX;

        public EnchantingTable SourceTable { get; private set; }

        public static EnchantingTableUI instance { get; set; }

        // How many frames IsVisible still answers true after the window hides, so the press that closed it
        // is not read again as opening the pause menu.
        private const int HiddenGraceFrames = 2;

        private int _hiddenFrames;
        private GameObject[] _gamepadHintContainers = Array.Empty<GameObject>();
        private bool _gamepadHintsShown = true;
        private bool _correctingTab;
        private bool _setUp;

        public void Awake()
        {
            instance = this;
        }

        public void Start()
        {
            Setup();
        }

        // Runs once per window: from Start, or straight after creation in PrepareForWorld, while Root is still
        // active, so the Auga fixup below sees the same active objects it would on a first open.
        private void Setup()
        {
            if (_setUp)
            {
                return;
            }

            _setUp = true;

            Localization.instance.Localize(transform);

            EnchantingUIController.SetupUIAudioSource(Audio);

            // Before SetupTabs: the Auga fixup swaps every tab button, and the tab sorting, activation and
            // feature status wiring below have to see the buttons that stay.
            EnchantingUIAugaFixup.AugaFixup(this);

            SetupTabs();
            CollectGamepadHints();
        }

        /// <summary>
        /// Builds the window hidden, so the first time the player uses a table it only has to fill its lists.
        /// Creating it the first time took several hundred milliseconds: instantiating the prefab, waking every
        /// page, localizing the whole tree and compiling all of that code. This runs on each world's first
        /// spawn (Game.m_playerInitialSpawn), in the same frame the player appears and while the loading screen
        /// still covers it. The window then lives as long as the in-game GUI, until logout.
        /// </summary>
        public static void PrepareForWorld()
        {
            if (instance != null || ZNetScene.instance == null)
            {
                return;
            }

            GameObject tablePrefab = ZNetScene.instance.GetPrefab(EnchantingTable.PrefabName);
            EnchantingTable table = tablePrefab != null ? tablePrefab.GetComponent<EnchantingTable>() : null;
            if (table == null || table.EnchantingUIPrefab == null)
            {
                return;
            }

            try
            {
                CreateUI(table.EnchantingUIPrefab);
                if (instance == null)
                {
                    return;
                }

                instance.Setup();
                instance.SelectStartingTab();
            }
            catch (Exception e)
            {
                // Handlers after this one on Game.m_playerInitialSpawn, and the rest of vanilla's spawn
                // frame, must still run. Show builds or finishes the window the usual way.
                Debug.LogError($"[EpicLoot] Could not prepare the enchanting table window: {e}");
            }
            finally
            {
                if (instance != null)
                {
                    instance.Root.SetActive(false);
                    instance.Scrim.SetActive(false);
                    instance._hiddenFrames = HiddenGraceFrames + 1;
                }
            }
        }

        // What TabHandler.Start would otherwise do on the first Show: open the default page, which wakes it and
        // fills its list now rather than then. Not through the tab's onClick, which would play the tab sound
        // under the loading screen. SetActiveTab also marks a tab as chosen, so TabHandler.Start leaves the page
        // alone. A default page the config has turned off is skipped forward, as OnActiveTabChanged would.
        private void SelectStartingTab()
        {
            int tabCount = TabHandler.m_tabs.Count;
            int defaultIndex = Math.Max(0, TabHandler.m_tabs.FindIndex(tab => tab.m_default));
            for (int offset = 0; offset < tabCount; ++offset)
            {
                int candidate = (defaultIndex + offset) % tabCount;
                if (IsTabAvailable(candidate))
                {
                    TabHandler.SetActiveTab(candidate, forceSelect: true, invokeOnClick: false);
                    return;
                }
            }
        }

        private static void CreateUI(GameObject enchantingUIPrefab)
        {
            if (StoreGui.instance == null)
            {
                return;
            }

            Transform inGameGui = StoreGui.instance.transform.parent;
            int siblingIndex = StoreGui.instance.transform.GetSiblingIndex() + 1;

            // Call to arms compatibility: increase scroll sensitivity
            foreach (ScrollRect scrollRect in enchantingUIPrefab.GetComponentsInChildren<ScrollRect>(true))
            {
                scrollRect.scrollSensitivity = 800f;
            }

            GameObject enchantingUI = Instantiate(enchantingUIPrefab, inGameGui);
            enchantingUI.transform.SetSiblingIndex(siblingIndex);

            // TODO: Reduce duplicate code, mock this inside unity in the future
            Transform existingBackground = StoreGui.instance.m_rootPanel.transform.Find("border (1)");
            Transform panel = enchantingUI.transform.Find("Panel");
            if (existingBackground != null & panel != null)
            {
                Image image = existingBackground.GetComponent<Image>();
                panel.GetComponent<Image>().material = image.material;
            }
        }

        private void SetupTabs()
        {
            foreach(TabHandler.Tab tab in TabHandler.m_tabs)
            {
                tab.m_onClick.AddListener(PlayTabSelectSFX);

                FeatureStatus fs = tab.m_button.gameObject.GetComponent<FeatureStatus>();
                if (fs != null)
                {
                    fs.Refresh();
                }
            }

            // Overrides the prefab's bindings, which put the tab bar on the bumpers that
            // MultiSelectListFocusController also claims, and left Tab bound here while Update uses it to
            // close the window.
            TabHandler.m_gamepadInput = true;
            TabHandler.m_gamepadNavigateLeft = "JoyLTrigger";
            TabHandler.m_gamepadNavigateRight = "JoyRTrigger";
            TabHandler.m_tabKeyInput = false;

            if (TabScrim != null && !TabHandler.m_blockingElements.Contains(TabScrim))
            {
                TabHandler.m_blockingElements.Add(TabScrim);
            }

            TabHandler.ActiveTabChanged += OnActiveTabChanged;

            SortTabsIntoVisualOrder();
            RefreshTabActivation();
        }

        // The prefab lists Upgrade before Rune while the tab bar shows Rune before Upgrade, and TabHandler
        // cycles by list index, so the triggers would visit the last two tabs in the wrong order.
        private void SortTabsIntoVisualOrder()
        {
            TabHandler.Tab selected = TabHandler.m_selected >= 0 && TabHandler.m_selected < TabHandler.m_tabs.Count
                ? TabHandler.m_tabs[TabHandler.m_selected]
                : null;

            TabHandler.m_tabs.Sort((a, b) => GetTabSiblingIndex(a).CompareTo(GetTabSiblingIndex(b)));

            if (selected != null)
            {
                TabHandler.m_selected = TabHandler.m_tabs.IndexOf(selected);
            }
        }

        private static int GetTabSiblingIndex(TabHandler.Tab tab)
        {
            return tab.m_button != null ? tab.m_button.transform.GetSiblingIndex() : int.MaxValue;
        }

        // Also called directly, not just from the event: TabHandler picks its default tab in Start, which
        // may run before the subscription above.
        private void RefreshTabActivation()
        {
            EnchantingUIController.TabActivation(this);

            if (TabHandler != null)
            {
                OnActiveTabChanged(TabHandler.GetActiveTab());
            }
        }

        // TabHandler's own cycling skips tabs whose button is null, not ones config has deactivated, so it
        // can land on a hidden tab and show its empty page.
        private void OnActiveTabChanged(int index)
        {
            if (_correctingTab || TabHandler == null || index < 0 ||
                index >= TabHandler.m_tabs.Count || IsTabAvailable(index))
            {
                return;
            }

            // Still readable here: this fires in the same frame as the press that moved the tab.
            int direction = ZInput.GetButtonDown(TabHandler.m_gamepadNavigateLeft) ? -1 : 1;

            int tabCount = TabHandler.m_tabs.Count;
            for (int offset = 1; offset < tabCount; ++offset)
            {
                int candidate = ((index + offset * direction) % tabCount + tabCount) % tabCount;
                if (!IsTabAvailable(candidate))
                {
                    continue;
                }

                _correctingTab = true;
                try
                {
                    // Silent while closed: a live config change can move the tab of the window built at
                    // spawn, and onClick is the tab sound.
                    TabHandler.SetActiveTab(candidate, invokeOnClick: Root.activeSelf);
                }
                finally
                {
                    _correctingTab = false;
                }

                return;
            }
        }

        private bool IsTabAvailable(int index)
        {
            TabHandler.Tab tab = TabHandler.m_tabs[index];
            return tab.m_button != null && tab.m_button.gameObject.activeSelf;
        }

        // Matches the prefab's hint containers by name; they ship always-on with nothing to toggle them.
        private void CollectGamepadHints()
        {
            List<GameObject> hints = new List<GameObject>();
            foreach (Transform child in Root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "GamepadHints" || child.name == "TabGamepadHints")
                {
                    hints.Add(child.gameObject);
                }
            }

            _gamepadHintContainers = hints.ToArray();
            RefreshGamepadHints();
        }

        private void RefreshGamepadHints()
        {
            bool show = ZInput.IsGamepadActive();
            if (show == _gamepadHintsShown)
            {
                return;
            }

            _gamepadHintsShown = show;
            foreach (GameObject hint in _gamepadHintContainers)
            {
                if (hint != null)
                {
                    hint.SetActive(show);
                }
            }
        }

        public static void Show(EnchantingTable source)
        {
            if (instance == null)
            {
                CreateUI(source.EnchantingUIPrefab);
            }

            if (instance == null)
            {
                Debug.LogError("Enchanting Table UI not setup properly!");
                return;
            }

            instance.SourceTable = source;

            // On first creation the prefab is instantiated already-active, so every panel and tab
            // FeatureStatus ran OnEnable before SourceTable was assigned and skipped subscribing to
            // the table's change events. Force an inactive->active transition so their OnEnable runs
            // again with SourceTable set (this is what the close/reopen path already does).
            if (instance.Root.activeSelf)
            {
                instance.Root.SetActive(false);
            }
            instance.Root.SetActive(true);
            instance.Scrim.SetActive(true);
            instance.SourceTable.Refresh();

            foreach (EnchantingTableUIPanelBase panel in instance.Panels)
            {
                panel.DeselectAll();
            }
        }

        public static void Hide()
        {
            if (instance == null)
            {
                return;
            }

            instance.Root.SetActive(false);
            instance.Scrim.SetActive(false);
            instance.SourceTable = null;
        }

        public static bool IsVisible()
        {
            return instance != null && ((instance._hiddenFrames <= HiddenGraceFrames) ||
                (instance.Root != null && instance.Root.activeSelf));
        }

        public static bool IsInTextInput()
        {
            if (!IsVisible())
            {
                return false;
            }

            InputField[] textFields = instance.Root.GetComponentsInChildren<InputField>(false);
            foreach (InputField inputField in textFields)
            {
                if (inputField.isFocused)
                {
                    return true;
                }
            }

            return false;
        }

        public void Update()
        {
            if (Root == null)
            {
                return;
            }

            if (!Root.activeSelf)
            {
                _hiddenFrames++;
                return;
            }

            _hiddenFrames = 0;

            RefreshGamepadHints();

            // The player died (or logged out) with the table open: close it -- nothing else does,
            // and every dereference below would NRE each frame over the death screen.
            if (Player.m_localPlayer == null)
            {
                Hide();
                return;
            }

            bool disallowClose = (Chat.instance != null && Chat.instance.HasFocus()) ||
                Console.IsVisible() || Menu.IsVisible() || (TextViewer.instance != null &&
                TextViewer.instance.IsVisible()) || Player.m_localPlayer.InCutscene();

            if (disallowClose)
            {
                return;
            }

            bool gotCloseInput = ZInput.GetButtonDown("JoyButtonB") ||
                ZInput.GetKeyDown(KeyCode.Escape) || ZInput.GetKeyDown(KeyCode.Tab);

            if (gotCloseInput)
            {
                ZInput.ResetButtonStatus("JoyButtonB");
                ZInput.ResetButtonStatus("JoyJump");

                bool panelCapturedInput = false;
                foreach (EnchantingTableUIPanelBase panel in Panels)
                {
                    if (panel.isActiveAndEnabled && panel.CanCancel())
                    {
                        panel.Cancel();
                        panelCapturedInput = true;
                        break;
                    }
                }

                if (!panelCapturedInput)
                {
                    Hide();
                }
            }
        }

        public static void UpdateTabActivation()
        {
            instance?.RefreshTabActivation();
        }

        public static void UpdateUpgradeActivation()
        {
            UpdateTabActivation();
        }

        public void LockTabs()
        {
            TabScrim.SetActive(true);
        }

        public void UnlockTabs()
        {
            TabScrim.SetActive(false);
        }

        public void PlayTabSelectSFX()
        {
            Audio.PlayOneShot(TabClickSFX);
        }

        public void PlayEnchantBonusSFX()
        {
            Audio.PlayOneShot(EnchantBonusSFX);
        }
    }
}
