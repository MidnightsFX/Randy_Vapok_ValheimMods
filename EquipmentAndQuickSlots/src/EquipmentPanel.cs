using System;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static EquipmentAndQuickSlots.Slots;

namespace EquipmentAndQuickSlots {
    // The floating panel next to the inventory. The grid elements for the hidden slot rows already
    // exist (the player grid renders the full-height inventory); this class only shrinks the
    // visible grid and physically relocates the slot elements onto a cloned background panel.
    // All vanilla behavior — drag/drop, tooltips, gamepad selection, other mods' icon overlays —
    // keeps working because these are real InventoryGrid elements.
    public static class EquipmentPanel {
        private const string BackgroundName = "EaqsEquipmentPanel";

        private const float tileSpace = 6f;
        private const float tileSize = 64f + tileSpace;
        private const float interslotSpaceInTiles = 0.25f;
        private const float inventoryPanelOffset = 100f;
        private const int equipmentColumns = 3;
        private const int equipmentRows = 2;

        private static RectTransform inventoryDarken;
        private static RectTransform inventoryBackground;
        private static Image inventoryBackgroundImage;
        private static RectTransform equipmentBackground;
        private static Image equipmentBackgroundImage;
        private static RectTransform selectedFrame;
        private static RectTransform inventorySelectedFrame;

        private static Color normalColor = Color.clear;
        private static Color highlightedColor = Color.clear;

        private static int ActiveQuickSlots => ValConfig.QuickSlotsEnabled.Value ? ValConfig.QuickSlotCount.Value : 0;
        private static bool EquipmentVisible => ValConfig.EquipmentSlotsEnabled.Value;
        private static int ActiveCustomSlots => GetCustomSlots().Count(slot => slot.IsActive);
        private static int CustomColumns => (ActiveCustomSlots + equipmentRows - 1) / equipmentRows;

        private static float InventoryPanelWidth => InventoryGui.instance ? InventoryGui.instance.m_player.rect.width : 0;
        private static float PanelWidthInTiles => Math.Max((EquipmentVisible ? equipmentColumns : 0) + CustomColumns, ActiveQuickSlots);
        private static float PanelHeightInTiles => (EquipmentVisible ? equipmentRows : 0) + (ActiveQuickSlots > 0 ? 1f + interslotSpaceInTiles : 0f);
        private static float PanelWidth => PanelWidthInTiles * tileSize + tileSpace / 2;
        private static float PanelHeight => PanelHeightInTiles * tileSize + tileSpace / 2;
        private static Vector2 PanelPosition => new Vector2(InventoryPanelWidth + inventoryPanelOffset, 0f);

        internal static Vector2 GetSlotPosition(Slot slot) {
            if (slot.IsEquipmentSlot) {
                int i = slot.Index - EquipmentSlotStartIndex;
                return PanelPosition + new Vector2(i % equipmentColumns * tileSize, -(i / equipmentColumns * tileSize));
            }

            if (slot.IsQuickSlot) {
                float y = (EquipmentVisible ? equipmentRows + interslotSpaceInTiles : 0f) * tileSize;
                return PanelPosition + new Vector2(slot.Index * tileSize, -y);
            }

            if (slot.IsCustomSlot) {
                // Extra columns to the right of the equipment block, two per column
                int ordinal = System.Array.IndexOf(ReservedIndices, slot.Index);
                int col = (EquipmentVisible ? equipmentColumns : 0) + ordinal / equipmentRows;
                int row = ordinal % equipmentRows;
                return PanelPosition + new Vector2(col * tileSize, -(row * tileSize));
            }

            return PanelPosition;
        }

        // Runs from InventoryGui.Update while visible: builds the cloned background once, keeps
        // its size and skin in sync afterwards.
        internal static void UpdateEquipmentBackground() {
            if (!InventoryGui.instance)
                return;

            if (inventoryBackground == null)
                inventoryBackground = InventoryGui.instance.m_player?.Find("Bkg")?.GetComponent<RectTransform>();
            if (inventoryBackground == null)
                return;

            if (!equipmentBackground && InventoryGui.instance.m_player) {
                Transform selectedFrames = InventoryGui.instance.m_player.GetComponent<UIGroupHandler>()?.m_enableWhenActiveAndGamepad.transform;
                inventoryDarken = InventoryGui.instance.m_player.Find("Darken").GetComponent<RectTransform>();

                equipmentBackground = new GameObject(BackgroundName, typeof(RectTransform)).GetComponent<RectTransform>();
                equipmentBackground.gameObject.layer = inventoryBackground.gameObject.layer;
                equipmentBackground.SetParent(InventoryGui.instance.m_player, worldPositionStays: false);
                equipmentBackground.SetSiblingIndex(1 + (selectedFrames == null ? inventoryDarken.GetSiblingIndex() : selectedFrames.GetSiblingIndex()));
                equipmentBackground.offsetMin = Vector2.zero;
                equipmentBackground.offsetMax = Vector2.zero;
                equipmentBackground.sizeDelta = Vector2.zero;
                equipmentBackground.anchoredPosition = Vector2.zero;
                equipmentBackground.anchorMin = new Vector2(0f, 1f);
                equipmentBackground.anchorMax = new Vector2(0f, 1f);

                RectTransform equipmentDarken = UnityEngine.Object.Instantiate(inventoryDarken, equipmentBackground);
                equipmentDarken.name = "Darken";
                equipmentDarken.sizeDelta = Vector2.one * 70f;

                Transform equipmentBkg = UnityEngine.Object.Instantiate(inventoryBackground.transform, equipmentBackground);
                equipmentBkg.name = "Bkg";

                equipmentBackgroundImage = equipmentBkg.GetComponent<Image>();
                inventoryBackgroundImage = inventoryBackground.transform.GetComponent<Image>();

                if (selectedFrames != null) {
                    inventorySelectedFrame = selectedFrames.GetChild(0) as RectTransform;
                    selectedFrame = UnityEngine.Object.Instantiate(inventorySelectedFrame, selectedFrames);
                    selectedFrame.name = "selected (EAQS)";

                    selectedFrame.offsetMin = equipmentBackground.offsetMin;
                    selectedFrame.offsetMax = equipmentBackground.offsetMax;
                    selectedFrame.anchorMin = equipmentBackground.anchorMin;
                    selectedFrame.anchorMax = equipmentBackground.anchorMax;
                }
            }

            if (equipmentBackgroundImage && inventoryBackgroundImage) {
                equipmentBackgroundImage.sprite = inventoryBackgroundImage.sprite;
                equipmentBackgroundImage.overrideSprite = inventoryBackgroundImage.overrideSprite;
                equipmentBackgroundImage.color = inventoryBackgroundImage.color;
            }

            if (equipmentBackground) {
                bool anySlots = PanelHeightInTiles > 0;
                equipmentBackground.gameObject.SetActive(anySlots);
                equipmentBackground.sizeDelta = new Vector2(PanelWidth, PanelHeight);
                equipmentBackground.anchoredPosition = PanelPosition + new Vector2(PanelWidth / 2, -PanelHeight / 2);

                if (selectedFrame) {
                    selectedFrame.sizeDelta = equipmentBackground.sizeDelta + Vector2.one * 26f;
                    selectedFrame.anchoredPosition = equipmentBackground.anchoredPosition;
                }
            }
        }

        // Runs from InventoryGrid.UpdateGui on the player grid: shrink the visible grid, relocate
        // slot elements, label them, tint unfit targets while dragging.
        internal static void UpdateInventorySlots() {
            InventoryGrid grid = InventoryGui.instance.m_playerGrid;

            grid.m_gridRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, VisibleRows * grid.m_elementSpace);

            int startIndex = InventorySizeVisible;
            ItemDrop.ItemData dragItem = InventoryGui.instance.m_dragItem;

            for (int i = 0; i < Math.Min(slots.Length, grid.m_elements.Count - startIndex); ++i) {
                InventoryGrid.Element element = grid.m_elements[startIndex + i];
                Slot slot = slots[i];

                GameObject go = element?.m_go;
                if (!go)
                    continue;

                go.SetActive(slot.IsActive);
                if (!slot.IsActive)
                    continue;

                go.GetComponent<RectTransform>().anchoredPosition = EquipmentAndQuickSlots.HasAuga ? AugaPanel.GetSlotPosition(slot) : GetSlotPosition(slot);
                SetSlotLabel(go.transform.Find("binding"), slot);
                SetSlotColor(go.GetComponent<Button>(), dragItem != null && !DragItemFits(slot, dragItem));
            }

            for (int i = startIndex + slots.Length; i < grid.m_elements.Count; i++)
                grid.m_elements[i]?.m_go?.SetActive(false);
        }

        private static bool DragItemFits(Slot slot, ItemDrop.ItemData dragItem) {
            // For equipment cells the drag lands via drag-to-equip, so the tint should follow the
            // type check, not the equipped-state predicate.
            if (slot.IsEquipmentSlot)
                return WouldFitEquipmentSlot(slot, dragItem);

            return slot.ItemFits(dragItem);
        }

        private static void SetSlotLabel(Transform binding, Slot slot) {
            if (!binding)
                return;

            TMP_Text text = binding.GetComponent<TMP_Text>();
            if (!text)
                return;

            // The paperdoll itself communicates the equipment slots; no labels there under Auga
            if (EquipmentAndQuickSlots.HasAuga && slot.IsEquipmentSlot) {
                text.enabled = false;
                return;
            }

            binding.gameObject.SetActive(true);
            text.enabled = true;
            text.text = slot.IsHotkeySlot ? slot.GetShortcutText() : slot.Name;
            RectTransform rect = binding.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(30f, 10f);
            rect.sizeDelta = new Vector2(64f, 20f);
        }

        private static void SetSlotColor(Button button, bool unfit) {
            if (!button)
                return;

            if (normalColor == Color.clear) {
                normalColor = button.colors.normalColor;
                highlightedColor = button.colors.highlightedColor;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = unfit ? new Color(0.8f, 0.2f, 0.2f, 0.5f) : normalColor;
            colors.highlightedColor = unfit ? new Color(0.9f, 0.3f, 0.3f, 0.7f) : highlightedColor;
            button.colors = colors;
        }

        private static void ClearPanel() {
            inventoryDarken = null;
            inventoryBackground = null;
            inventoryBackgroundImage = null;
            equipmentBackground = null;
            equipmentBackgroundImage = null;
            selectedFrame = null;
            inventorySelectedFrame = null;
            normalColor = Color.clear;
            highlightedColor = Color.clear;
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnDestroy))]
        private static class InventoryGui_OnDestroy_ClearPanel {
            private static void Postfix() {
                ClearPanel();
                AugaPanel.Clear();
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
        private static class InventoryGui_Update_UpdateEquipmentPanel {
            private static void Postfix() {
                if (!Player.m_localPlayer)
                    return;

                if (!InventoryGui.IsVisible())
                    return;

                if (EquipmentAndQuickSlots.HasAuga)
                    AugaPanel.UpdatePanel();
                else
                    UpdateEquipmentBackground();
            }
        }

        // Vanilla gamepad navigation walks the raw grid; steer the selection off reserved and
        // inactive slot cells (their elements are hidden) onto the nearest active slot.
        [HarmonyPatch(typeof(InventoryGrid), "UpdateGamepad")]
        private static class InventoryGrid_UpdateGamepad_SkipInactiveSlotCells {
            private static void Postfix(InventoryGrid __instance) {
                if (!InventoryGui.instance || __instance != InventoryGui.instance.m_playerGrid)
                    return;

                Vector2i sel = __instance.m_selected;
                if (sel.y < VisibleRows)
                    return;

                int slotIndex = (sel.y - VisibleRows) * InventoryWidth + sel.x;
                if (slotIndex >= 0 && slotIndex < slots.Length && slots[slotIndex].IsActive)
                    return;

                int best = -1;
                int bestDist = int.MaxValue;
                for (int i = 0; i < slots.Length; i++) {
                    if (!slots[i].IsActive)
                        continue;

                    int dist = Math.Abs(i - slotIndex);
                    if (dist < bestDist) {
                        bestDist = dist;
                        best = i;
                    }
                }

                __instance.m_selected = best >= 0 ? slots[best].GridPosition : new Vector2i(Math.Min(sel.x, InventoryWidth - 1), VisibleRows - 1);
            }
        }

        [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
        private static class InventoryGrid_UpdateGui_RelocateSlotElements {
            private static void Postfix(InventoryGrid __instance) {
                if (!InventoryGui.instance || __instance != InventoryGui.instance.m_playerGrid)
                    return;

                if (Player.m_localPlayer == null)
                    return;

                UpdateInventorySlots();
            }
        }
    }
}
