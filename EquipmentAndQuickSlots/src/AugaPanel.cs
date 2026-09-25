using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static EquipmentAndQuickSlots.Slots;

namespace EquipmentAndQuickSlots {
    // Auga variant of the floating panel: an Auga-styled panel beside Auga's player panel, the equipment
    // cells as a diamond over the paperdoll, the quick row beneath a small divider and any API slots in
    // columns on the right. The cells are the same relocated grid elements as in the vanilla panel; only
    // the backdrop and the position table differ.
    //
    // Everything is laid out in slot-root space (EquipmentPanel.EnsureSlotRoot): the backdrop is the slot
    // root's first child, so it shares the cells' coordinate space and draws beneath them. The panel's top
    // left sits PanelGap to the right of the player panel's top right, measured from the live rects every
    // frame, so it follows Auga's panel however it is placed or sized. Offsets below are cell top-left
    // corners relative to the panel's top left, y growing downwards as negative numbers; Auga's slot
    // prefab is anchored and pivoted top left, so an offset is directly an anchoredPosition.
    public static class AugaPanel {
        private const string PanelName = "EAQS";

        private const float PanelGap = 20f;         // between Auga's player panel and this one
        private const float Padding = 16f;          // inside the panel background
        private const float Cell = 64f;             // Auga's InventoryElement
        private const float Pitch = 70f;            // Auga's player grid: 64 px cells, 6 px spacing
        private const float DividerGap = 20f;       // between the equipment diamond and the quick row
        private const float CustomGap = 16f;        // between the main content and the API columns
        private const int CustomSlotsPerColumn = 4;

        // The equipment diamond, by EquipmentSlotTypes order: Trinket and Utility level with the helmet
        // on either side, chest and shoulders half a cell in from the body column one row down, legs
        // under the helmet, and the extra utility cells down the right edge. Cells 8 px apart.
        private static readonly Vector2[] equipmentOffsets =
        {
            new Vector2(108f, 0f),      // Helmet
            new Vector2(72f, -72f),     // Chest
            new Vector2(108f, -144f),   // Legs
            new Vector2(144f, -72f),    // Shoulder
            new Vector2(216f, 0f),      // Utility
            new Vector2(0f, 0f),        // Trinket
            new Vector2(216f, -72f),    // Utility 2
            new Vector2(216f, -144f),   // Utility 3
        };
        private const float EquipmentWidth = 216f + Cell;   // symmetric about the body column's centre
        private const float EquipmentHeight = 144f + Cell;

        private static GameObject _panel;
        private static RectTransform _paperdollArea;
        private static RectTransform _divider;

        // Recomputed at the top of every UpdateInventorySlots pass (see Layout), before any cell is placed.
        private static Vector2 _origin;
        private static float _contentWidth;
        private static float _quickRowTop;
        private static float _customLeft;

        private static int ActiveQuickSlots => ValConfig.QuickSlotsEnabled.Value ? ValConfig.QuickSlotCount.Value : 0;
        private static bool EquipmentVisible => ValConfig.EquipmentSlotsEnabled.Value;
        private static Slot[] ActiveCustomSlots => GetCustomSlots().Where(slot => slot.IsActive).OrderBy(slot => slot.Index).ToArray();

        internal static Vector2 GetSlotPosition(Slot slot) {
            if (slot.IsEquipmentSlot) {
                float equipmentLeft = Padding + (_contentWidth - EquipmentWidth) / 2f;
                return _origin + new Vector2(equipmentLeft, -Padding) + equipmentOffsets[slot.Index - EquipmentSlotStartIndex];
            }

            if (slot.IsQuickSlot) {
                float rowLeft = Padding + (_contentWidth - QuickRowWidth(ActiveQuickSlots)) / 2f;
                return _origin + new Vector2(rowLeft + slot.Index * Pitch, _quickRowTop);
            }

            if (slot.IsCustomSlot) {
                // Columns down the right of the panel, packed with no gaps and wrapping once a column
                // is full: the API can register more slots than one column holds.
                int ordinal = System.Math.Max(0, System.Array.IndexOf(ActiveCustomSlots, slot));
                int row = ordinal % CustomSlotsPerColumn;
                int col = ordinal / CustomSlotsPerColumn;
                return _origin + new Vector2(_customLeft + col * Pitch, -Padding - row * Pitch);
            }

            return _origin;
        }

        private static float QuickRowWidth(int count) => count > 0 ? count * Pitch - (Pitch - Cell) : 0f;

        /// <summary>
        /// Works out where the panel goes and how it is divided, for the cells placed after it this pass.
        /// <paramref name="cellRoot"/> is the slot root; false when there is nothing to show.
        /// </summary>
        internal static bool Layout(RectTransform cellRoot) {
            RectTransform player = InventoryGui.instance ? InventoryGui.instance.m_player : null;
            if (!player || !cellRoot)
                return false;

            // The player panel's top right, carried into the slot root's space. Both are children of the
            // player panel (EnsureSlotRoot), and the slot root has unit scale, so this is a subtraction;
            // the slot root's pivot is its top left, which is where its top-left-anchored children start.
            Rect rect = player.rect;
            Vector2 slotRootTopLeft = cellRoot.localPosition;
            _origin = new Vector2(rect.xMax + PanelGap, rect.yMax) - slotRootTopLeft;

            int quickCount = ActiveQuickSlots;
            int customCount = ActiveCustomSlots.Length;
            _contentWidth = Mathf.Max(EquipmentVisible ? EquipmentWidth : 0f, QuickRowWidth(quickCount));

            float y = -Padding;
            if (EquipmentVisible)
                y -= EquipmentHeight;
            float dividerY = y - DividerGap / 2f;
            if (EquipmentVisible && quickCount > 0)
                y -= DividerGap;
            _quickRowTop = y;
            if (quickCount > 0)
                y -= Cell;

            int customColumns = (customCount + CustomSlotsPerColumn - 1) / CustomSlotsPerColumn;
            int customRows = System.Math.Min(customCount, CustomSlotsPerColumn);
            _customLeft = Padding + _contentWidth + (_contentWidth > 0f ? CustomGap : 0f);

            float width = customColumns > 0 ? _customLeft + QuickRowWidth(customColumns) + Padding : Padding + _contentWidth + Padding;
            float height = Mathf.Max(-y + Padding, customRows > 0 ? 2f * Padding + QuickRowWidth(customRows) : 0f);
            bool visible = EquipmentVisible || quickCount > 0 || customCount > 0;

            UpdateBackground(cellRoot, visible, width, height, dividerY, EquipmentVisible && quickCount > 0);
            return visible;
        }

        private static void UpdateBackground(RectTransform cellRoot, bool visible, float width, float height, float dividerY, bool showDivider) {
            if (_panel == null) {
                _panel = Auga.API.Panel_Create(cellRoot, new Vector2(width, height), PanelName, false);
                if (_panel == null)
                    return;

                // The background keeps its raycast target: a click on it must not reach the world, where
                // vanilla drops a dragged item on the ground.

                _paperdollArea = new GameObject("PaperdollArea", typeof(RectTransform)).GetComponent<RectTransform>();
                _paperdollArea.SetParent(_panel.transform, false);
                _paperdollArea.anchorMin = _paperdollArea.anchorMax = _paperdollArea.pivot = new Vector2(0f, 1f);
                if (EquipmentAndQuickSlots.Paperdolls != null) {
                    var paperdolls = Object.Instantiate(EquipmentAndQuickSlots.Paperdolls, _paperdollArea, false);
                    paperdolls.name = "Paperdolls";
                    Stretch((RectTransform)paperdolls.transform);
                    foreach (var image in paperdolls.GetComponentsInChildren<Image>(true)) {
                        // The doll fills the diamond's height, centred on the body column.
                        Stretch(image.rectTransform);
                        image.preserveAspect = true;
                        image.raycastTarget = false;
                    }
                }

                var divider = Auga.API.Divider_CreateSmall(_panel.transform, "Divider");
                _divider = divider != null ? (RectTransform)divider.transform : null;
                if (_divider != null) {
                    _divider.anchorMin = _divider.anchorMax = new Vector2(0.5f, 1f);
                    _divider.pivot = new Vector2(0.5f, 0.5f);
                }
            }

            // First under the slot root, so every cell (re-added to it each frame) draws on top.
            if (_panel.transform.parent != cellRoot)
                _panel.transform.SetParent(cellRoot, false);
            _panel.transform.SetAsFirstSibling();
            _panel.SetActive(visible);
            if (!visible)
                return;

            var rt = (RectTransform)_panel.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = _origin;
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            _paperdollArea.gameObject.SetActive(EquipmentVisible);
            _paperdollArea.anchoredPosition = new Vector2(Padding + (_contentWidth - EquipmentWidth) / 2f, -Padding);
            _paperdollArea.sizeDelta = new Vector2(EquipmentWidth, EquipmentHeight);

            if (_divider != null) {
                _divider.gameObject.SetActive(showDivider);
                _divider.anchoredPosition = new Vector2(Padding + _contentWidth / 2f - width / 2f, dividerY);
                _divider.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(0f, _contentWidth - 20f));
            }

            UpdatePaperdollGender();
        }

        private static void Stretch(RectTransform rect) {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // Runs from InventoryGui.Update while visible. Auga sizes its player panel for vanilla's rows only
        // (InventoryGui.SetInventorySize, from Player.SetInventorySize), so the extra visible rows this mod
        // adds would sit under the panel's grid mask, cut off. The panel is kept tall enough for them.
        internal static void UpdatePanel() {
            InventoryGui gui = InventoryGui.instance;
            if (!gui || !gui.m_player || gui.m_invGridHeight <= 0f)
                return;

            // Vanilla's own formula (InventoryGui.SetInventorySize), which counts rows above four.
            float height = gui.m_playerHeight + (VisibleRows - 4) * gui.m_invGridHeight;
            if (!Mathf.Approximately(gui.m_player.sizeDelta.y, height))
                gui.SetInventorySize(VisibleRows);
        }

        private static void UpdatePaperdollGender() {
            var player = Player.m_localPlayer;
            if (player == null || _paperdollArea == null)
                return;

            var paperdolls = _paperdollArea.Find("Paperdolls");
            if (paperdolls == null)
                return;

            bool female = player.m_visEquipment != null && player.m_visEquipment.GetModelIndex() == 1;
            paperdolls.Find("Male")?.gameObject.SetActive(!female);
            paperdolls.Find("Female")?.gameObject.SetActive(female);
        }

        internal static void Clear() {
            _panel = null;
            _paperdollArea = null;
            _divider = null;
        }
    }
}
