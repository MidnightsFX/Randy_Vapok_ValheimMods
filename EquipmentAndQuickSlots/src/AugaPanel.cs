using UnityEngine;
using static EquipmentAndQuickSlots.Slots;

namespace EquipmentAndQuickSlots {
    // Auga variant of the floating panel: an Auga-styled panel with the paperdoll backdrop and a
    // small divider, with the equipment cells laid out over the doll and the quick row beneath.
    // Uses the same element-relocation mechanism as the vanilla panel — only the background
    // construction and the position table differ.
    public static class AugaPanel {
        private const string PanelName = "EAQS";

        // All layout constants are in player-grid-root space, matching where the relocated
        // elements live. Tune in-game with Auga installed.
        private static readonly Vector2 panelBase = new Vector2(752, -166);
        private const float panelWidth = 255f;
        private const float panelHeight = 352f;
        private const float paperdollHeight = 157f;
        private const float tileSize = 74f;

        // Equipment diamond over the paperdoll (relative to the equipment cluster center):
        // Head top, Chest/Shoulder at the sides, Legs bottom, Utility right, Trinket left.
        private static readonly Vector2 equipClusterCenter = new Vector2(110.5f, -57f);
        private static readonly Vector2[] equipPositions =
        {
            new Vector2(0f, 0f),        // Helmet
            new Vector2(-36f, -72f),    // Chest
            new Vector2(0f, -144f),     // Legs
            new Vector2(36f, -72f),     // Shoulder
            new Vector2(104f, 0f),      // Utility
            new Vector2(-104f, 0f),     // Trinket
        };

        private static GameObject _panel;

        private static int ActiveQuickSlots => ValConfig.QuickSlotsEnabled.Value ? ValConfig.QuickSlotCount.Value : 0;

        private static float PanelWidth => Mathf.Max(panelWidth, ActiveQuickSlots * tileSize + 20f);

        internal static Vector2 GetSlotPosition(Slot slot) {
            if (slot.IsEquipmentSlot)
                return panelBase + equipClusterCenter + equipPositions[slot.Index - EquipmentSlotStartIndex];

            if (slot.IsQuickSlot) {
                float rowStart = (PanelWidth - ActiveQuickSlots * tileSize) / 2f + 5f;
                return panelBase + new Vector2(rowStart + slot.Index * tileSize, -(paperdollHeight + 30f));
            }

            if (slot.IsCustomSlot) {
                // A column down the right edge of the panel
                int ordinal = System.Array.IndexOf(ReservedIndices, slot.Index);
                return panelBase + new Vector2(PanelWidth + 10f, -(ordinal * tileSize));
            }

            return panelBase;
        }

        // Runs from InventoryGui.Update while visible.
        internal static void UpdatePanel() {
            if (!InventoryGui.instance || !InventoryGui.instance.m_player)
                return;

            if (_panel == null) {
                _panel = Auga.API.Panel_Create(InventoryGui.instance.m_player, new Vector2(PanelWidth, panelHeight), PanelName, false);
                if (_panel == null)
                    return;

                var rt = (RectTransform)_panel.transform;
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.anchoredPosition = panelBase;
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, PanelWidth);
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panelHeight);

                var paperdolls = Object.Instantiate(EquipmentAndQuickSlots.Paperdolls, _panel.transform, false);
                paperdolls.name = "Paperdolls";

                var divider = Auga.API.Divider_CreateSmall(_panel.transform, "Divider", PanelWidth - 40);
                ((RectTransform)divider.transform).anchoredPosition = new Vector2(0, -paperdollHeight);
            }

            _panel.SetActive(ValConfig.EquipmentSlotsEnabled.Value || ActiveQuickSlots > 0);

            UpdatePaperdollGender();
        }

        private static void UpdatePaperdollGender() {
            var player = Player.m_localPlayer;
            if (player == null || _panel == null)
                return;

            var paperdolls = _panel.transform.Find("Paperdolls");
            if (paperdolls == null)
                return;

            bool female = player.m_visEquipment != null && player.m_visEquipment.GetModelIndex() == 1;
            paperdolls.Find("Male")?.gameObject.SetActive(!female);
            paperdolls.Find("Female")?.gameObject.SetActive(female);
        }

        internal static void Clear() {
            _panel = null;
        }
    }
}
