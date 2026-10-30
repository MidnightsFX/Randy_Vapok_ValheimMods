using UnityEngine;
using UnityEngine.EventSystems;

namespace EquipmentAndQuickSlots {
    // Placement of the vanilla chest window, run every frame the inventory is open (from
    // EquipmentPanel, so only on the vanilla layout). With no saved offset it follows the extra
    // inventory rows down, but only as far as the bottom of the screen; once the player has dragged
    // it, it sits at that offset from the game's own spot instead. Either way it stays on the
    // screen, so a position saved at one resolution or GUI scale still works at another.
    //
    // Vanilla geometry since Deep North: the window is 570x340, pivoted at its top-left corner and
    // hung 30 units under the bottom-left corner of the player panel, whose child it is. Vanilla
    // grows that panel for rows bought from Haldor (InventoryGui.SetInventorySize), so the window
    // follows those by itself; this mod's extra rows are drawn below the panel, so for those it is
    // moved here. The canvas is 1080 / GUI scale units tall on a 16:9 screen.
    internal static class ContainerWindow {
        private const float screenMargin = 10f;         // the window's background reaches 10 past its rect
        private const float pivotShiftPerExtraRow = 0.2f;

        private static readonly Vector3[] corners = new Vector3[4];

        private static Vector2? originalPivot;
        private static Vector2 originalPosition;
        private static bool offsetApplied;              // anchoredPosition holds a player offset
        private static Vector2? dragOffset;             // live offset while the window is being dragged

        private static Vector2 Offset => dragOffset ?? ValConfig.ContainerPosition.Value;

        private static bool DragKeyHeld => ValConfig.ContainerDragKey.Value.MainKey != KeyCode.None && ZInput.GetKey(ValConfig.ContainerDragKey.Value.MainKey);
        private static bool CanDrag => ValConfig.ContainerDraggable.Value || DragKeyHeld;

        internal static void UpdatePosition(int extraRows) {
            // Only while a chest is open: closed, it isn't drawn, and its canvas can't be looked up
            RectTransform container = InventoryGui.instance.m_container;
            if (!container || !container.gameObject.activeInHierarchy)
                return;

            if (originalPivot == null) {
                originalPivot = container.pivot;
                originalPosition = container.anchoredPosition;
            }
            if (!container.GetComponent<ContainerDragHandle>())
                container.gameObject.AddComponent<ContainerDragHandle>();

            Vector2 offset = Offset;
            if (offset != Vector2.zero) {
                container.pivot = originalPivot.Value;
                container.anchoredPosition = originalPosition + ClampOffset(container, offset);
                offsetApplied = true;
                return;
            }

            // Automatic placement leaves anchoredPosition alone unless a reset has to undo an offset
            if (offsetApplied) {
                container.anchoredPosition = originalPosition;
                offsetApplied = false;
            }
            container.pivot = new Vector2(originalPivot.Value.x, originalPivot.Value.y + PivotShift(container, extraRows));
        }

        internal static void Clear() {
            originalPivot = null;
            offsetApplied = false;
            dragOffset = null;
        }

        // A fifth of the window's height per extra row clears the taller inventory, but with several
        // extra rows and a large GUI scale the full shift pushed the window's last rows off the
        // bottom of the screen. The shift stops at the screen edge instead and the window overlaps
        // the bottom of the inventory (about two rows at GUI scale 1.25 with five extra rows), which
        // keeps every chest row and both buttons reachable. Raising the pivot by one moves the
        // window down one window height.
        private static float PivotShift(RectTransform container, int extraRows) {
            float shift = extraRows * pivotShiftPerExtraRow;
            float height = WindowSize(container).y;
            if (shift <= 0f || height <= 0f || !TryGetScreenRect(container, out Rect screen))
                return shift;

            float bottom = RestingPivotPosition(container).y - originalPivot.Value.y * height;
            float room = bottom - screenMargin - screen.yMin;
            return Mathf.Clamp(shift, 0f, Mathf.Max(0f, room / height));
        }

        // The nearest offset that keeps the whole window on the screen. A window bigger than the
        // screen keeps its left and top edges visible, where the name and the buttons are.
        private static Vector2 ClampOffset(RectTransform container, Vector2 offset) {
            if (!TryGetScreenRect(container, out Rect screen))
                return offset;

            Vector2 size = WindowSize(container);
            Vector2 corner = RestingPivotPosition(container) - Vector2.Scale(originalPivot.Value, size);    // lower left, no offset

            float x = Mathf.Min(offset.x, screen.xMax - screenMargin - size.x - corner.x);
            x = Mathf.Max(x, screen.xMin + screenMargin - corner.x);
            float y = Mathf.Max(offset.y, screen.yMin + screenMargin - corner.y);
            y = Mathf.Min(y, screen.yMax - screenMargin - size.y - corner.y);
            return new Vector2(x, y);
        }

        // The screen as a rect in the window's parent space. The parent is the player panel, not
        // anything screen-sized, so its own rect says nothing about the room left; the root
        // canvas covers the screen. Converted with position and scale only, because BetterUI
        // zeroes the player panel's z scale and InverseTransformPoint through it gives NaN.
        private static bool TryGetScreenRect(RectTransform container, out Rect screen) {
            screen = default;
            Canvas canvas = container.GetComponentInParent<Canvas>();
            Transform parent = container.parent;
            if (!canvas || !parent)
                return false;

            Vector3 scale = parent.lossyScale;
            if (Mathf.Approximately(scale.x, 0f) || Mathf.Approximately(scale.y, 0f))
                return false;

            ((RectTransform)canvas.rootCanvas.transform).GetWorldCorners(corners);
            Vector3 origin = parent.position;
            screen = Rect.MinMaxRect((corners[0].x - origin.x) / scale.x, (corners[0].y - origin.y) / scale.y,
                                     (corners[2].x - origin.x) / scale.x, (corners[2].y - origin.y) / scale.y);
            return true;
        }

        // Where the pivot sits in the parent's space at the game's own position. The anchors are a
        // single point, so this is the same whatever pivot and offset are applied right now.
        private static Vector2 RestingPivotPosition(RectTransform container) =>
            (Vector2)container.localPosition - container.anchoredPosition + originalPosition;

        private static Vector2 WindowSize(RectTransform container) => Vector2.Scale(container.rect.size, container.localScale);

        // The offset that draws the window where it is now, so a drag that starts from the
        // automatic placement doesn't make it jump. Lowering the pivot by d moves the window by d
        // window sizes the other way.
        private static Vector2 CurrentOffset(RectTransform container) =>
            container.anchoredPosition - originalPosition - Vector2.Scale(container.pivot - originalPivot.Value, WindowSize(container));

        // Drag-to-move on the chest window, the same way as the equipment panel. It sits on the
        // window root, so drags anywhere on the background, the name or the weight reach it; the
        // cells and the scrollbar keep their own drags. The offset is clamped as it moves, so
        // dragging back from the screen edge responds at once, and the config is written on release.
        private class ContainerDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler {
            private bool _dragging;
            private float _scaleFactor = 1f;

            public void OnBeginDrag(PointerEventData eventData) {
                if (!CanDrag || eventData.button != PointerEventData.InputButton.Left || originalPivot == null)
                    return;

                _dragging = true;
                dragOffset = CurrentOffset((RectTransform)transform);
                _scaleFactor = GetComponentInParent<Canvas>()?.scaleFactor ?? 1f;
                if (_scaleFactor <= 0f)
                    _scaleFactor = 1f;
            }

            public void OnDrag(PointerEventData eventData) {
                if (!_dragging || dragOffset == null)
                    return;

                dragOffset = ClampOffset((RectTransform)transform, dragOffset.Value + eventData.delta / _scaleFactor);
            }

            public void OnEndDrag(PointerEventData eventData) {
                if (!_dragging)
                    return;

                _dragging = false;
                if (dragOffset != null)
                    ValConfig.ContainerPosition.Value = dragOffset.Value;
                dragOffset = null;
            }

            // Only with the key held, so the always-draggable toggle can't make a stray right-click reset it
            public void OnPointerClick(PointerEventData eventData) {
                if (eventData.button == PointerEventData.InputButton.Right && DragKeyHeld)
                    ValConfig.ContainerPosition.Value = Vector2.zero;
            }

            private void OnDisable() {
                // Chest closed mid-drag: keep whatever was dragged so far
                if (_dragging)
                    OnEndDrag(null);
            }
        }
    }
}
