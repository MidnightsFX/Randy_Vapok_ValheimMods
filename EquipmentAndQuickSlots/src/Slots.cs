using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace EquipmentAndQuickSlots
{
    // The slot region is two hidden inventory rows below the visible grid. Every slot is a real
    // cell of the player's single Inventory; the panel UI relocates the grid elements, nothing
    // else. Index -> grid position is pure math, so the layout below is load-bearing for saved
    // characters: never reorder or repurpose an index that has shipped.
    //
    //   0- 5  Quick1..Quick6
    //   6- 7  reserved
    //   8-13  Helmet, Chest, Legs, Shoulder, Utility, Trinket
    //  14-15  reserved
    public static class Slots
    {
        public const string quickSlotID = "Quick";
        public const string helmetSlotID = "Helmet";
        public const string chestSlotID = "Chest";
        public const string legsSlotID = "Legs";
        public const string shoulderSlotID = "Shoulder";
        public const string utilitySlotID = "Utility";
        public const string trinketSlotID = "Trinket";
        public const string emptySlotID = "Empty";

        public const int SlotCount = 16;
        public const int HiddenRows = 2;
        public const int QuickSlotStartIndex = 0;
        public const int EquipmentSlotStartIndex = 8;

        public static readonly Vector2i emptyPosition = new Vector2i(-1, -1);

        // Per-item slot memory, so items coming back from a grave or a chest return to the slot
        // they were in. Only honored when the player ID matches the current profile.
        public const string customKeyPlayerID = "eaqs_player";
        public const string customKeySlotID = "eaqs_slot";
        internal const string customKeyWeaponShield = "eaqs_weaponshield";

        public class Slot
        {
            private readonly string _id;
            private readonly Func<string> _getName;
            private readonly Func<KeyboardShortcut> _getShortcut;
            private readonly Func<string> _getShortcutText;
            private readonly Func<ItemDrop.ItemData, bool> _itemIsValid;
            private readonly Func<bool> _isActive;

            private readonly int _index;
            private Vector2i _gridPos = emptyPosition;

            public string ID => _id;
            public string Name => _getName != null ? Localization.instance.Localize(_getName()) : "";
            public bool IsActive => _isActive == null || _isActive();
            public Vector2i GridPosition => _gridPos;
            public int Index => _index;

            public bool IsHotkeySlot => _getShortcut != null;
            public bool IsQuickSlot => _index >= QuickSlotStartIndex && _index < QuickSlotStartIndex + ValConfig.MaxQuickSlots;
            public bool IsEquipmentSlot => _index >= EquipmentSlotStartIndex && _index < EquipmentSlotStartIndex + EquipmentSlotCount;
            public bool IsCustomSlot => System.Array.IndexOf(ReservedIndices, _index) >= 0 && !IsEmptySlot;
            public bool IsEmptySlot => _id == emptySlotID;

            // Plugin GUID of the mod that registered this slot through the API; null for built-ins.
            public string OwnerGuid { get; internal set; }

            internal void UpdateGridPosition()
            {
                ItemDrop.ItemData item = Item;
                _gridPos = new Vector2i(_index % InventoryWidth, VisibleRows + _index / InventoryWidth);
                if (item != null)
                    item.m_gridPos = _gridPos;
            }

            public bool IsShortcutDown() => IsActive && _getShortcut != null && Player.m_localPlayer?.TakeInput() == true && PreventSimilarHotkeys.IsShortcutDown(_getShortcut());
            public bool IsShortcutDownWithItem() => Item != null && IsShortcutDown();
            public bool IsShortcutPressed() => IsActive && _getShortcut != null && Player.m_localPlayer?.TakeInput() == true && PreventSimilarHotkeys.IsShortcutPressed(_getShortcut());
            public bool IsShortcutPressedWithItem() => Item != null && IsShortcutPressed();

            public KeyboardShortcut GetShortcut() => _getShortcut == null ? KeyboardShortcut.Empty : _getShortcut();
            public string GetShortcutText() => _getShortcutText == null ? Name : Localization.instance.Localize(_getShortcutText());

            public ItemDrop.ItemData Item
            {
                get
                {
                    if (PlayerInventory == null || _gridPos == emptyPosition)
                        return null;

                    if (cachedItems.TryGetValue(_gridPos, out ItemDrop.ItemData item))
                        return item;

                    return CacheItem();
                }
            }

            internal ItemDrop.ItemData CacheItem()
            {
                if (PlayerInventory == null)
                    return null;

                // Cache is cleared on inventory change
                ItemDrop.ItemData item = PlayerInventory.GetItemAt(_gridPos.x, _gridPos.y);
                cachedItems[_gridPos] = item;
                return item;
            }

            public bool IsFree => Item == null;

            // Placement rule: may this item be put into this cell at all. For equipment cells this
            // is the item type only — vanilla unequips an item for the duration of a drag move, so
            // the equipped state must not gate placement.
            public bool ItemFits(ItemDrop.ItemData item) => item != null && IsActive && (_itemIsValid == null || _itemIsValid(item));

            // Residence rule: may this item stay in this cell. Equipment cells are equipped-only
            // (an item whose equip is queued and animating counts — it is about to be worn); the
            // validation sweep relocates anything that fits but doesn't belong.
            public bool ItemBelongs(ItemDrop.ItemData item) => ItemFits(item) && (!IsEquipmentSlot || IsEquippedByPlayer(item) || IsEquipQueued(item));

            public bool IsFreeQuickSlot() => IsQuickSlot && IsActive && IsFree;

            public void ClearItemCache()
            {
                cachedItems.Remove(_gridPos);
            }

            public Slot(string slotID, int slotIndex, Func<string> getName, Func<ItemDrop.ItemData, bool> itemIsValid, Func<bool> isActive)
            {
                _id = slotID;
                _index = slotIndex;
                _getName = getName;
                _itemIsValid = itemIsValid;
                _isActive = isActive;
            }

            public Slot(string slotID, int slotIndex, Func<string> getName, Func<ItemDrop.ItemData, bool> itemIsValid, Func<bool> isActive, Func<KeyboardShortcut> getShortcut, Func<string> getShortcutText)
                : this(slotID, slotIndex, getName, itemIsValid, isActive)
            {
                _getShortcut = getShortcut;
                _getShortcutText = getShortcutText;
            }

            public override string ToString() => (Name == "" ? ID : Name) + (IsActive ? "" : " (inactive)");
        }

        public static readonly Slot[] slots = new Slot[SlotCount];
        public static readonly Dictionary<Vector2i, ItemDrop.ItemData> cachedItems = new Dictionary<Vector2i, ItemDrop.ItemData>();

        // The reserved cells double as API custom-slot capacity. Growing capacity later means
        // adding a hidden row — save-safe, unlike ever shrinking one.
        public static readonly int[] ReservedIndices = { 6, 7, 14, 15 };

        public const string customSlotPrefix = "Custom";

        public const int VanillaInventoryHeight = 4;
        public const int VanillaInventoryWidth = 8;
        public const int EquipmentSlotCount = 6;

        // The visible rows are whatever the vanilla inventory (or a rows mod like moreslots)
        // established before we appended the hidden slot rows — captured once at Player.Awake —
        // plus the server-synced extra rows. The hidden slot rows always sit directly below.
        public static int BaseRows { get; private set; } = VanillaInventoryHeight;
        public const int MaxExtraRows = 5;
        public static int ExtraRows => Mathf.Clamp(ValConfig.ExtraInventoryRows?.Value ?? 0, 0, MaxExtraRows);
        public static int VisibleRows => BaseRows + ExtraRows;

        public static Player loadedPlayer;

        public static PlayerProfile CurrentPlayerProfile => Game.instance?.GetPlayerProfile() ?? FejdStartup.instance?.m_profiles[FejdStartup.instance.m_profileIndex];
        public static Player CurrentPlayer => Player.m_localPlayer ?? loadedPlayer;
        public static Inventory PlayerInventory => CurrentPlayer?.GetInventory();
        public static int InventoryWidth => PlayerInventory != null ? PlayerInventory.GetWidth() : VanillaInventoryWidth;
        public static int FullHeight => VisibleRows + HiddenRows;
        public static int InventorySizeVisible => VisibleRows * InventoryWidth;
        public static int InventorySizeFull => FullHeight * InventoryWidth;
        public static int InventorySizeActive => InventorySizeVisible + slots.Count(slot => slot.IsActive);

        public static bool IsValidPlayer(Character character) => character != null && character.IsPlayer() && Player.m_localPlayer == character && character.m_nview && character.m_nview.IsValid() && character.m_nview.IsOwner();

        private static bool _baseRowsCaptured;

        internal static void CaptureBaseRows(Inventory inventory)
        {
            // Captured exactly once, from the first Player.Awake before we ever extend a height —
            // after that any larger height we observe is our own FullHeight, not new base rows.
            if (_baseRowsCaptured)
                return;

            _baseRowsCaptured = true;
            BaseRows = Mathf.Max(1, inventory.m_height);
        }

        // The extra-row count changed (config edit, or the server's value arriving on join): the
        // slot region moves with the visible rows. Slot residents move with their slots; anything
        // from the visible grid that now finds itself on a slot cell it doesn't belong in is moved
        // back into the visible grid (or a free slot) — never dropped, never lost.
        internal static void OnExtraRowsChanged()
        {
            Inventory inventory = PlayerInventory;
            if (inventory == null)
            {
                UpdateSlotsGridPosition();
                return;
            }

            ClearCachedItems();
            Dictionary<Slot, ItemDrop.ItemData> residents = slots.ToDictionary(slot => slot, slot => slot.Item);

            UpdateSlotsGridPosition();

            bool changed = false;
            foreach (ItemDrop.ItemData item in inventory.m_inventory.ToList())
            {
                if (!IsGridPositionASlot(item.m_gridPos))
                    continue;

                Slot slot = GetSlotInGrid(item.m_gridPos);
                residents.TryGetValue(slot ?? slots[0], out ItemDrop.ItemData resident);
                bool intruder = slot == null
                                || (resident != null && resident != item)
                                || (resident == null && !slot.ItemBelongs(item));
                if (!intruder)
                    continue;

                Vector2i free = inventory.FindEmptySlot(true);
                if (free.x >= 0)
                    item.m_gridPos = free;
                else if (TryFindFreeSlotForItem(item, out Slot freeSlot))
                    item.m_gridPos = freeSlot.GridPosition;
                // else: leave it; the overlap sweep keeps trying to find it a home

                ClearCachedItems();
                changed = true;
            }

            if (changed)
                inventory.Changed();

            SlotValidation.ValidateItems();
            SlotValidation.ValidateSlots();
        }

        public static Slot[] GetEquipmentSlots(bool onlyActive = true) => slots.Where(slot => slot.IsEquipmentSlot && (!onlyActive || slot.IsActive)).ToArray();
        public static Slot[] GetQuickSlots() => Array.FindAll(slots, slot => slot.IsQuickSlot);

        public static bool TryGetSavedPlayerSlot(ItemDrop.ItemData item, out Slot slot)
        {
            slot = null;

            if (item.m_customData.TryGetValue(customKeyPlayerID, out string playerID) && item.m_customData.TryGetValue(customKeySlotID, out string slotID) && playerID == CurrentPlayerProfile?.GetPlayerID().ToString())
                if ((slot = FindSlot(slotID)) != null)
                    return true;

            return false;
        }

        public static Slot FindSlot(string slotID) => slots.FirstOrDefault(slot => slot.ID == slotID);

        public static bool TryFindFreeSlotForItem(ItemDrop.ItemData item, out Slot slot)
        {
            slot = null;

            if (item == null)
                return false;

            if (TryGetSavedPlayerSlot(item, out Slot prevSlot) && prevSlot.IsActive && prevSlot.ItemBelongs(item) && (prevSlot.IsFree || item == prevSlot.Item))
            {
                slot = prevSlot;
                return true;
            }

            int index = Array.FindIndex(slots, s => s.IsActive && s.IsFree && s.ItemBelongs(item));
            if (index == -1)
                return false;

            slot = slots[index];
            return true;
        }

        public static bool TryFindFreeEquipmentSlotForItem(ItemDrop.ItemData item, out Slot slot)
        {
            slot = null;

            if (item == null)
                return false;

            slot = GetEquipmentSlots().FirstOrDefault(s => s.IsFree && s.ItemBelongs(item));
            return slot != null;
        }

        public static bool TryFindFirstUnequippedSlotForItem(ItemDrop.ItemData item, out Slot slot)
        {
            slot = null;

            if (item == null)
                return false;

            // An occupant with a queued equip is about to be worn — don't swap it out
            slot = GetEquipmentSlots().FirstOrDefault(s => !s.IsFree && s.ItemFits(item) && !CurrentPlayer.IsItemEquiped(s.Item) && !IsEquipQueued(s.Item));
            return slot != null;
        }

        private static readonly List<ItemDrop.ItemData> itemsInGridOrder = new List<ItemDrop.ItemData>();

        // Walks the visible grid bottom-right to top-left looking for a free cell; failing that,
        // pushes one visible item into a free quick slot to make room. Equipment cells hold only
        // equipped items, so they are never a stash target here.
        public static bool TryMakeFreeSpaceInPlayerInventory(out Vector2i gridPos)
        {
            gridPos = emptyPosition;

            itemsInGridOrder.Clear();
            for (int i = VisibleRows - 1; i >= 0; i--)
                for (int j = InventoryWidth - 1; j >= 0; j--)
                    if (PlayerInventory.GetItemAt(j, i) is not ItemDrop.ItemData item)
                        return (gridPos = new Vector2i(j, i)) != emptyPosition;
                    else
                        itemsInGridOrder.Add(item);

            ClearCachedItems();

            foreach (ItemDrop.ItemData item in itemsInGridOrder)
            {
                if (TryFindFreeSlotForItem(item, out Slot slot))
                {
                    gridPos = item.m_gridPos;
                    item.m_gridPos = slot.GridPosition;
                    ClearCachedItems();
                    return true;
                }
            }

            return false;
        }

        public static bool HaveEmptyQuickSlot() => slots.Any(slot => slot.IsFreeQuickSlot());

        public static int GetEmptyQuickSlots() => slots.Count(slot => slot.IsFreeQuickSlot());

        public static Vector2i FindEmptyQuickSlot() => TryFindEmptyQuickSlot(out Slot slot) ? slot.GridPosition : emptyPosition;

        public static bool TryFindEmptyQuickSlot(out Slot slot)
        {
            slot = slots.FirstOrDefault(s => s.IsFreeQuickSlot());
            return slot != null;
        }

        internal static void SaveLastEquippedSlotsToItems()
        {
            if (!Game.instance)
                return;

            long playerID = Game.instance.GetPlayerProfile().GetPlayerID();

            foreach (Slot slot in slots)
            {
                ItemDrop.ItemData item = slot.Item;
                if (item != null)
                {
                    item.m_customData[customKeyPlayerID] = playerID.ToString();
                    item.m_customData[customKeySlotID] = slot.ID;
                }
            }
        }

        internal static void PruneLastEquippedSlotFromItem(ItemDrop.ItemData item)
        {
            if (item == null || !item.m_customData.ContainsKey(customKeySlotID))
                return;

            item.m_customData.Remove(customKeyPlayerID);
            item.m_customData.Remove(customKeySlotID);
        }

        public static bool IsGridPositionASlot(Vector2i gridPos) => gridPos.y >= VisibleRows;

        public static bool IsItemInSlot(ItemDrop.ItemData item) => item != null && IsGridPositionASlot(item.m_gridPos);

        public static bool IsItemInEquipmentSlot(ItemDrop.ItemData item) => GetItemSlot(item) is Slot slot && slot.IsEquipmentSlot;

        public static Slot GetSlotInGrid(Vector2i pos)
        {
            if (!IsGridPositionASlot(pos))
                return null;

            foreach (Slot slot in slots)
                if (slot.GridPosition == pos)
                    return slot;

            return null;
        }

        public static Slot GetItemSlot(ItemDrop.ItemData item)
        {
            if (!IsItemInSlot(item))
                return null;

            if (PlayerInventory == null || !PlayerInventory.ContainsItem(item))
                return null;

            foreach (Slot slot in slots)
                if (slot.GridPosition == item.m_gridPos)
                    return slot;

            return null;
        }

        public static IEnumerable<ItemDrop.ItemData> GetAllSlotItems()
        {
            foreach (Slot slot in slots)
                if (slot.Item is ItemDrop.ItemData item)
                    yield return item;
        }

        // Any equipped item whose type has a dedicated cell. Weapons and shields stay in the
        // visible grid: only the paperdoll types live in the slot region.
        public static bool IsEquipmentSlotItem(ItemDrop.ItemData item)
        {
            return slots.Any(slot => slot.IsEquipmentSlot && slot.IsActive && slot.ItemFits(item));
        }

        // Index-aligned with slots 8-13.
        public static readonly ItemDrop.ItemData.ItemType[] EquipmentSlotTypes =
        {
            ItemDrop.ItemData.ItemType.Helmet,
            ItemDrop.ItemData.ItemType.Chest,
            ItemDrop.ItemData.ItemType.Legs,
            ItemDrop.ItemData.ItemType.Shoulder,
            ItemDrop.ItemData.ItemType.Utility,
            ItemDrop.ItemData.ItemType.Trinket,
        };

        public static ItemDrop.ItemData.ItemType GetEquipmentSlotType(Slot slot) =>
            slot.IsEquipmentSlot ? EquipmentSlotTypes[slot.Index - EquipmentSlotStartIndex] : ItemDrop.ItemData.ItemType.None;

        // "Would this item belong in this cell once equipped" — the drag-to-equip check, which has
        // to pass before the item is equipped and therefore can't use the slot's own predicate.
        public static bool WouldFitEquipmentSlot(Slot slot, ItemDrop.ItemData item) =>
            item != null && slot.IsEquipmentSlot && slot.IsActive && item.m_shared.m_itemType == GetEquipmentSlotType(slot);

        internal static bool IsEquippedByPlayer(ItemDrop.ItemData item) => item != null && (item.m_equipped || CurrentPlayer?.IsItemEquiped(item) == true);

        // ---------------------------------------------------------------------------------------
        // Equip queue. Drag & drop equips go through vanilla's minor-action queue — the same
        // animated, interruptible path right-click uses — so the equip duration gate can't be
        // bypassed by dragging. Vanilla's QueueEquipAction/QueueUnequipAction toggle (queuing an
        // item that already has an action removes it), hence the explicit state checks here.

        private static bool IsActionQueued(ItemDrop.ItemData item, Player.MinorActionData.ActionType type)
        {
            Player player = CurrentPlayer;
            if (item == null || player == null || player.m_actionQueue == null)
                return false;

            foreach (Player.MinorActionData action in player.m_actionQueue)
                if (action.m_item == item && action.m_type == type)
                    return true;

            return false;
        }

        internal static bool IsEquipQueued(ItemDrop.ItemData item) => IsActionQueued(item, Player.MinorActionData.ActionType.Equip);

        internal static bool IsUnequipQueued(ItemDrop.ItemData item) => IsActionQueued(item, Player.MinorActionData.ActionType.Unequip);

        internal static void QueueEquip(Player player, ItemDrop.ItemData item)
        {
            if (player == null || item == null || player.IsItemEquiped(item) || IsEquipQueued(item))
                return;

            // A pending unequip is simply cancelled: the item stays worn
            if (IsUnequipQueued(item))
            {
                player.RemoveEquipAction(item);
                return;
            }

            player.QueueEquipAction(item);
        }

        internal static void QueueUnequip(Player player, ItemDrop.ItemData item)
        {
            if (player == null || item == null || !player.IsItemEquiped(item) || IsUnequipQueued(item))
                return;

            if (IsEquipQueued(item))
            {
                player.RemoveEquipAction(item);
                return;
            }

            player.QueueUnequipAction(item);
        }

        private static Func<ItemDrop.ItemData, bool> EquipmentSlotValidator(ItemDrop.ItemData.ItemType itemType)
        {
            // Type only. Equipped-only semantics live in Slot.ItemBelongs / the validation sweep,
            // never in the placement predicate (see ItemFits).
            return item => item != null && item.m_shared.m_itemType == itemType;
        }

        private static bool IsQuickSlotAvailable(int index) => ValConfig.QuickSlotsEnabled.Value && ValConfig.QuickSlotCount.Value > index;

        private static bool EquipmentSlotsAvailable() => ValConfig.EquipmentSlotsEnabled.Value;

        private static string GetQuickSlotText(int index)
        {
            string label = ValConfig.QuickSlotLabels[index].Value;
            if (!string.IsNullOrEmpty(label))
                return label;

            KeyboardShortcut key = ValConfig.QuickSlotKeys[index].Value;
            return key.Equals(KeyboardShortcut.Empty) ? "" : key.ToString();
        }

        internal static void InitializeSlots()
        {
            int index = 0;

            void AddSlot(string id, Func<string> getName, Func<ItemDrop.ItemData, bool> itemIsValid, Func<bool> isActive)
            {
                slots[index] = new Slot(id, index, getName, itemIsValid, isActive);
                index++;
            }

            void AddHotkeySlot(string id, Func<string> getName, Func<ItemDrop.ItemData, bool> itemIsValid, Func<bool> isActive, Func<KeyboardShortcut> getShortcut, Func<string> getShortcutText)
            {
                slots[index] = new Slot(id, index, getName, itemIsValid, isActive, getShortcut, getShortcutText);
                index++;
            }

            void AddReservedSlot()
            {
                AddSlot(emptySlotID, () => "", item => false, () => false);
            }

            for (int i = 0; i < ValConfig.MaxQuickSlots; i++)
            {
                int quickIndex = i;
                AddHotkeySlot($"{quickSlotID}{i + 1}",
                    () => GetQuickSlotText(quickIndex),
                    null,
                    () => IsQuickSlotAvailable(quickIndex),
                    () => ValConfig.QuickSlotKeys[quickIndex].Value,
                    () => GetQuickSlotText(quickIndex));
            }

            AddReservedSlot();
            AddReservedSlot();

            AddSlot(helmetSlotID, () => "Head", EquipmentSlotValidator(ItemDrop.ItemData.ItemType.Helmet), EquipmentSlotsAvailable);
            AddSlot(chestSlotID, () => "Chest", EquipmentSlotValidator(ItemDrop.ItemData.ItemType.Chest), EquipmentSlotsAvailable);
            AddSlot(legsSlotID, () => "Legs", EquipmentSlotValidator(ItemDrop.ItemData.ItemType.Legs), EquipmentSlotsAvailable);
            AddSlot(shoulderSlotID, () => "Shoulders", EquipmentSlotValidator(ItemDrop.ItemData.ItemType.Shoulder), EquipmentSlotsAvailable);
            AddSlot(utilitySlotID, () => "Utility", EquipmentSlotValidator(ItemDrop.ItemData.ItemType.Utility), EquipmentSlotsAvailable);
            AddSlot(trinketSlotID, () => "Trinket", EquipmentSlotValidator(ItemDrop.ItemData.ItemType.Trinket), EquipmentSlotsAvailable);

            AddReservedSlot();
            AddReservedSlot();

            UpdateSlotsGridPosition();
        }

        public static Slot[] GetCustomSlots() => slots.Where(slot => slot.IsCustomSlot).ToArray();

        internal static bool TryAddCustomSlot(string slotId, string ownerGuid, Func<string> getName, Func<ItemDrop.ItemData, bool> itemIsValid, Func<bool> isActive)
        {
            string internalId = customSlotPrefix + slotId;
            if (slots.Any(slot => slot.ID == internalId))
                return false;

            foreach (int index in ReservedIndices)
            {
                if (!slots[index].IsEmptySlot)
                    continue;

                slots[index] = new Slot(internalId, index, getName, itemIsValid, isActive) { OwnerGuid = ownerGuid };
                slots[index].UpdateGridPosition();
                ClearCachedItems();
                return true;
            }

            EquipmentAndQuickSlots.LogWarning($"Could not add custom slot {slotId}: all {ReservedIndices.Length} custom slots are taken");
            return false;
        }

        internal static bool TryRemoveCustomSlot(string slotId)
        {
            string internalId = customSlotPrefix + slotId;
            int index = Array.FindIndex(slots, slot => slot.IsCustomSlot && slot.ID == internalId);
            if (index == -1)
                return false;

            ItemDrop.ItemData item = slots[index].Item;
            slots[index] = new Slot(emptySlotID, index, () => "", i => false, () => false);
            slots[index].UpdateGridPosition();
            ClearCachedItems();

            if (item != null)
            {
                // Rescue the resident item: visible grid first, any other slot second, the
                // ground as a last resort — never delete.
                Vector2i free = PlayerInventory.FindEmptySlot(true);
                if (free.x >= 0)
                    item.m_gridPos = free;
                else if (TryFindFreeSlotForItem(item, out Slot slot))
                    item.m_gridPos = slot.GridPosition;
                else if (CurrentPlayer != null)
                    CurrentPlayer.DropItem(PlayerInventory, item, item.m_stack);

                PlayerInventory?.Changed();
            }

            return true;
        }

        internal static void UpdateSlotsGridPosition()
        {
            ClearCachedItems();
            foreach (Slot slot in slots)
                slot.CacheItem();

            foreach (Slot slot in slots)
                slot.UpdateGridPosition();

            ClearCachedItems();

            InventoryPatches.UpdatePlayerInventorySize();
        }

        internal static void ClearCachedItems() => cachedItems.Clear();
    }
}
