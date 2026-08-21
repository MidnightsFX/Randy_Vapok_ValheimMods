using BepInEx.Configuration;
using UnityEngine;

namespace EquipmentAndQuickSlots {
    internal class ValConfig {
        public static ConfigFile cfg;

        // Add Client sided config entries under here
        public static ConfigEntry<bool> LoggingEnabled;
        public static ConfigEntry<bool> ViewDebugSaveData;
        public static ConfigEntry<TextAnchor> QuickSlotsAnchor;
        public static ConfigEntry<Vector2> QuickSlotsPosition;
        public static ConfigEntry<bool> InstantlyReequipArmorOnPickup;
        public static ConfigEntry<bool> AutoEquipCarryWeightItems;
        public static ConfigEntry<bool> AutoEquipWeaponShield;
        public static ConfigEntry<bool> PreventStackAll;
        public static ConfigEntry<bool> PreventAutoPickup;
        public static ConfigEntry<bool> BackupEnabled;
        public static ConfigEntry<Vector2> EquipmentPanelPosition;
        public static ConfigEntry<bool> EquipmentPanelDraggable;
        public static ConfigEntry<KeyboardShortcut> EquipmentPanelDragKey;
        public static ConfigEntry<bool> ShowPaperdoll;
        public static readonly ConfigEntry<KeyboardShortcut>[] QuickSlotKeys = new ConfigEntry<KeyboardShortcut>[MaxQuickSlots];
        public static readonly ConfigEntry<string>[] QuickSlotLabels = new ConfigEntry<string>[MaxQuickSlots];

        // Add Server synced config entries under here
        public static ConfigEntry<bool> EquipmentSlotsEnabled;
        public static ConfigEntry<bool> QuickSlotsEnabled;
        public static ConfigEntry<int> QuickSlotCount;
        public static ConfigEntry<bool> DontDropEquipmentOnDeath;
        public static ConfigEntry<bool> DontDropQuickslotsOnDeath;

        public const int MaxQuickSlots = 6;

        // The keys the first three hotkeys shipped with in 2.x; kept so existing config files
        // carry their values (and their None-repair behavior) forward unchanged.
        private static readonly KeyCode[] DefaultQuickSlotKeys = { KeyCode.Z, KeyCode.V, KeyCode.B, KeyCode.None, KeyCode.None, KeyCode.None };

        public ValConfig(ConfigFile cf) {
            // ensure all the config values are created
            cfg = cf;
            cfg.SaveOnConfigSet = true;
            CreateConfigValues(cf);
        }

        public static void SaveOnSet(bool enabled) {
            cfg.SaveOnConfigSet = enabled;
            cfg.Save();
        }

        private void CreateConfigValues(ConfigFile Config) {
            LoggingEnabled = Config.Bind("Logging", "Logging Enabled", false,
                new ConfigDescription("Enable logging.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true }));

            for (int i = 0; i < MaxQuickSlots; ++i) {
                QuickSlotKeys[i] = Config.Bind("Hotkeys", $"Quick slot hotkey {i + 1}", new KeyboardShortcut(DefaultQuickSlotKeys[i]),
                    new ConfigDescription($"Hotkey for Quick Slot {i + 1}.", null, new ConfigurationManagerAttributes { }));
                // A Valheim keybind-storage format change once wiped stored MainKeys to None; restore the
                // shipped default when that happens, but only for the slots that have a default at all.
                if (QuickSlotKeys[i].Value.MainKey == KeyCode.None && DefaultQuickSlotKeys[i] != KeyCode.None)
                    QuickSlotKeys[i].Value = new KeyboardShortcut(DefaultQuickSlotKeys[i]);

                QuickSlotLabels[i] = Config.Bind("Hotkeys", $"Quick slot hotkey label {i + 1}", "",
                    new ConfigDescription($"Hotkey Label for Quick Slot {i + 1}. Leave blank to use the hotkey itself.", null, new ConfigurationManagerAttributes { }));

                // A rebind changes which vanilla actions collide with the hotkey
                QuickSlotKeys[i].SettingChanged += (_, _) => PreventSimilarHotkeys.FillSimilarHotkey();
            }

            ViewDebugSaveData = Config.Bind("Toggles", "View Debug Save Data", false,
                new ConfigDescription("Enable to view the raw legacy save data in the compendium.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true }));

            QuickSlotsAnchor = Config.Bind("Quick Slots", "Quick Slots Anchor", TextAnchor.LowerLeft,
                new ConfigDescription("The point on the HUD to anchor the Quick Slots bar. Changing this also changes the pivot of the Quick Slots to that corner.", null, new ConfigurationManagerAttributes { }));
            QuickSlotsPosition = Config.Bind("Quick Slots", "Quick Slots Position", new Vector2(216, 150),
                new ConfigDescription("The position offset from the Quick Slots Anchor at which to place the Quick Slots.", null, new ConfigurationManagerAttributes { }));

            EquipmentPanelPosition = Config.Bind("Equipment Panel", "Equipment Panel Position", new Vector2(615f, 28f),
                new ConfigDescription("Position of the equipment and quick slot panel, relative to the inventory grid. Drag the panel by its background in-game to move it (hold the drag key, or enable 'Equipment Panel Draggable').", null, new ConfigurationManagerAttributes { }));
            EquipmentPanelDraggable = Config.Bind("Equipment Panel", "Equipment Panel Draggable", false,
                new ConfigDescription("Allow dragging the panel by its background at any time, without holding the drag key.", null, new ConfigurationManagerAttributes { }));
            EquipmentPanelDragKey = Config.Bind("Equipment Panel", "Equipment Panel Drag Key", new KeyboardShortcut(KeyCode.LeftAlt),
                new ConfigDescription("Hold this key while dragging the panel background to move the panel.", null, new ConfigurationManagerAttributes { }));
            ShowPaperdoll = Config.Bind("Equipment Panel", "Show Paperdoll", false,
                new ConfigDescription("Draw the character paperdoll image behind the equipment slots.", null, new ConfigurationManagerAttributes { }));

            InstantlyReequipArmorOnPickup = Config.Bind("Gravestone", "Instantly re-equip armor on pickup", true,
                new ConfigDescription("If set to true, when you pickup your gravestone the armor that was in your equipment slots is instantly re-equipped, if possible. Only valid when Equipment Slots are enabled.", null, new ConfigurationManagerAttributes { }));
            AutoEquipCarryWeightItems = Config.Bind("Gravestone", "Auto-equip carry weight items on pickup", true,
                new ConfigDescription("If set to true, belts and other carry-weight gear from your gravestone are equipped immediately on pickup so the rest of the loot stays carryable.", null, new ConfigurationManagerAttributes { }));
            AutoEquipWeaponShield = Config.Bind("Gravestone", "Auto-equip weapon and shield on pickup", true,
                new ConfigDescription("If set to true, the weapon and shield you were holding when you died are re-equipped when you pick up your gravestone.", null, new ConfigurationManagerAttributes { }));

            PreventStackAll = Config.Bind("Protections", "Prevent Stack All", true,
                new ConfigDescription("Items in equipment and quick slots are not moved by the container Stack All button.", null, new ConfigurationManagerAttributes { }));
            PreventAutoPickup = Config.Bind("Protections", "Prevent auto-pickup into quick slots", false,
                new ConfigDescription("Picked up items never land directly in a quick slot; they go to the regular inventory only.", null, new ConfigurationManagerAttributes { }));
            BackupEnabled = Config.Bind("Protections", "Slots backup enabled", true,
                new ConfigDescription("Automatically back up equipment and quick slot contents into the character save on every save, and restore them when the slots load empty (e.g. after the mod was temporarily removed).", null, new ConfigurationManagerAttributes { }));

            // Instantiate server synced config entries here
            EquipmentSlotsEnabled = BindServerConfig("Toggles", "Enable Equipment Slots", true, "Enable the equipment slots. Disabling this while items are equipped will attempt to move them to your inventory.");
            QuickSlotsEnabled = BindServerConfig("Toggles", "Enable Quick Slots", true, "Enable the quick slots. Disabling this while items are in the slots will attempt to move them to your inventory.");
            QuickSlotCount = BindServerConfig("Quick Slots", "Quick Slot Count", 3, "Number of quick slots available.", false, 0, MaxQuickSlots);
            DontDropEquipmentOnDeath = BindServerConfig("Gravestone", "Dont drop equipment on death", false, "If set to true, your equipped items stay with you when you die instead of dropping into the gravestone.");
            DontDropQuickslotsOnDeath = BindServerConfig("Gravestone", "Dont drop quickslot items on death", false, "If set to true, the items in the quickslots stay with you when you die instead of dropping into the gravestone.");
        }

        // Every overload below marks the entry IsAdminOnly, which is what makes Jotunn's
        // SynchronizationManager push the server's value to every client. Use cfg.Bind directly for
        // anything that is genuinely per-machine (UI, logging, client-side toggles).

        /// <summary>
        /// Binds a server-synced bool.
        /// </summary>
        /// <param name="category">Config file section.</param>
        /// <param name="key">Entry name within the section.</param>
        /// <param name="value">Default value.</param>
        /// <param name="description">Shown in the config file and the Configuration Manager.</param>
        /// <param name="acceptableValues">Optional constraint. Normally null for a bool.</param>
        /// <param name="advanced">Hides the entry behind the Advanced toggle.</param>
        public static ConfigEntry<bool> BindServerConfig(string category, string key, bool value, string description, AcceptableValueBase acceptableValues = null, bool advanced = false) {
            return cfg.Bind(category, key, value,
                new ConfigDescription(description,
                    acceptableValues,
                new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced })
                );
        }

        /// <summary>
        /// Binds a server-synced int constrained to a range.
        /// </summary>
        /// <param name="category">Config file section.</param>
        /// <param name="key">Entry name within the section.</param>
        /// <param name="value">Default value.</param>
        /// <param name="description">Shown in the config file and the Configuration Manager.</param>
        /// <param name="advanced">Hides the entry behind the Advanced toggle.</param>
        /// <param name="valMin">Lowest accepted value.</param>
        /// <param name="valMax">Highest accepted value.</param>
        public static ConfigEntry<int> BindServerConfig(string category, string key, int value, string description, bool advanced = false, int valMin = 0, int valMax = 150) {
            return cfg.Bind(category, key, value,
                new ConfigDescription(description,
                new AcceptableValueRange<int>(valMin, valMax),
                new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced })
                );
        }

        /// <summary>
        /// Binds a server-synced float constrained to a range.
        /// </summary>
        /// <param name="category">Config file section.</param>
        /// <param name="key">Entry name within the section.</param>
        /// <param name="value">Default value.</param>
        /// <param name="description">Shown in the config file and the Configuration Manager.</param>
        /// <param name="advanced">Hides the entry behind the Advanced toggle.</param>
        /// <param name="valMin">Lowest accepted value.</param>
        /// <param name="valMax">Highest accepted value.</param>
        public static ConfigEntry<float> BindServerConfig(string category, string key, float value, string description, bool advanced = false, float valMin = 0, float valMax = 150) {
            return cfg.Bind(category, key, value,
                new ConfigDescription(description,
                new AcceptableValueRange<float>(valMin, valMax),
                new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced })
                );
        }

        /// <summary>
        /// Binds a server-synced string, optionally restricted to a fixed list of values.
        /// </summary>
        /// <param name="category">Config file section.</param>
        /// <param name="key">Entry name within the section.</param>
        /// <param name="value">Default value.</param>
        /// <param name="description">Shown in the config file and the Configuration Manager.</param>
        /// <param name="acceptableValues">Allowed values, or null to accept anything.</param>
        /// <param name="advanced">Hides the entry behind the Advanced toggle.</param>
        public static ConfigEntry<string> BindServerConfig(string category, string key, string value, string description, AcceptableValueList<string> acceptableValues = null, bool advanced = false) {
            return cfg.Bind(category, key, value,
                new ConfigDescription(
                    description,
                    acceptableValues,
                new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced })
                );
        }
    }
}
