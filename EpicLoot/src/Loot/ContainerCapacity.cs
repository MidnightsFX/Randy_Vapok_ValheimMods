using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot
{
    /// <summary>
    /// Puts rolled loot into a chest that may be too small for it. Prosperity's bonus rolls can hand a
    /// 4x2 loot chest more items than it has slots, and Inventory.AddItem just returns false for the rest.
    ///
    /// The chest grows by whole rows instead. Nothing has to persist the new size: Inventory.Load keeps
    /// items below the grid (it skips the position check) and Container.Load then calls UpdateRows, which
    /// raises the height to fit them. So every client, and every reload, rebuilds the taller grid from the
    /// item positions alone, and the container UI scrolls.
    /// </summary>
    public static class ContainerCapacity
    {
        // Enough for a treasure-map chest at the default BonusRollsMax (four passes of up to five items,
        // plus its token and coin stacks). Past this, loot is dropped at the chest rather than grown into
        // an unwieldy grid.
        private const int MaxRows = 8;

        /// <param name="reservedSlots">Slots to leave free for stacks the caller adds afterwards.</param>
        public static void AddItems(Container container, List<ItemDrop.ItemData> items, int reservedSlots = 0)
        {
            if (container == null || container.m_inventory == null || items == null || items.Count == 0)
            {
                return;
            }

            var inventory = container.m_inventory;
            var width = Mathf.Max(1, inventory.GetWidth());
            var slotsNeeded = inventory.NrOfItems() + items.Count + Mathf.Max(0, reservedSlots);
            var rowsNeeded = Mathf.Min(MaxRows, Mathf.CeilToInt(slotsNeeded / (float)width));
            if (rowsNeeded > inventory.GetHeight())
            {
                EpicLoot.Log($"Growing {container.m_name} from {inventory.GetHeight()} to {rowsNeeded} rows " +
                    $"to fit {items.Count} rolled items.");
                inventory.SetHeight(rowsNeeded);
            }

            foreach (var item in items)
            {
                if (inventory.AddItem(item))
                {
                    continue;
                }

                EpicLoot.LogWarning($"{container.m_name} is full; dropping {item.m_shared.m_name} beside it.");
                ItemDrop.DropItem(item, item.m_stack, container.transform.position + Vector3.up,
                    Quaternion.identity);
            }
        }
    }
}
