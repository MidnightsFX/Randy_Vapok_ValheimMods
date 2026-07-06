using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.ShardStones
{
    // Press Use over a socketed magic item in the inventory to toggle a synthetic container whose slots are the item's sockets.
    // Drag Runestones and Shards in and out. The synthetic inventory holds reconstructed runestone/shard items; whenever it
    // changes, we reconcile the equipment's MagicItem.Sockets to match.
    public static class SocketsUI
    {
        public static ItemDrop.ItemData OpenEquipment;
        public static Inventory OpenInventory;
        private static bool _reconciling;

        private static bool IsSocketGridOpen => OpenEquipment != null && OpenInventory != null;

        private static Inventory BuildSocketInventory(ItemDrop.ItemData equipment, MagicItem magicItem)
        {
            var inv = new Inventory("Sockets", null, Mathf.Max(1, magicItem.SocketCount), 1);
            foreach (var socketed in magicItem.Sockets)
            {
                var item = ShardSocketManager.ReconstructShardItem(socketed);
                if (item != null)
                {
                    inv.AddItem(item);
                }
            }
            return inv;
        }

        // Reconcile MagicItem.Sockets to mirror the synthetic inventory's current contents.
        private static void SaveSockets()
        {
            if (_reconciling || OpenEquipment == null || OpenInventory == null)
            {
                return;
            }

            if (!OpenEquipment.IsMagic(out var magicItem))
            {
                return;
            }

            _reconciling = true;
            try
            {
                magicItem.Sockets.Clear();
                foreach (var item in OpenInventory.GetAllItems())
                {
                    // effect may be null for an inert shard; it still occupies the socket.
                    if (!ShardSocketManager.ResolveSocketedEffect(OpenEquipment, item, out var effect, out var color, out var rarity))
                    {
                        continue;
                    }
                    magicItem.Sockets.Add(new SocketedEffect(
                        effect, ShardSocketManager.GetSourcePrefabName(item), rarity) { ShardType = color });
                }
                OpenEquipment.SaveMagicItem(magicItem);

                if (Player.m_localPlayer != null)
                {
                    EquipmentEffectCache.Reset(Player.m_localPlayer);
                }
            }
            finally
            {
                _reconciling = false;
            }
        }

        // Builds and shows the socket overlay for the given equipment. The overlay reuses the
        // InventoryGui container panel; it stays open (keyed off OpenEquipment/OpenInventory) until
        // CloseContainer/Hide reconciles and clears it.
        private static void OpenSocketOverlay(InventoryGui invGui, ItemDrop.ItemData item)
        {
            if (item == null || !item.IsMagic(out var magicItem) || !magicItem.HasSockets())
            {
                return;
            }

            // Close any real container so its inventory doesn't linger behind the overlay.
            if (invGui.IsContainerOpen())
            {
                invGui.CloseContainer();
            }

            var inv = BuildSocketInventory(item, magicItem);
            inv.m_onChanged += SaveSockets;

            if (invGui.m_takeAllButton != null)
            {
                invGui.m_takeAllButton.gameObject.SetActive(false);
            }
            if (invGui.m_stackAllButton != null)
            {
                invGui.m_stackAllButton.gameObject.SetActive(false);
            }

            OpenEquipment = item;
            OpenInventory = inv;
            invGui.m_firstContainerUpdate = true;
        }

        // Claim the "Use" press before vanilla InventoryGui.Update consumes it. Vanilla Update reads
        // GetButtonDown("Use") and, while the inventory is visible, resets the button and Hide()s the
        // inventory -- all before it calls UpdateContainer. So the overlay's open/toggle has to happen
        // in an Update prefix (which runs first) and consume the button, or the press never survives to
        // reach the render path in UpdateContainer.
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
        public static class InventoryGui_Update_Patch
        {
            [UsedImplicitly]
            private static void Prefix(InventoryGui __instance)
            {
                if (!InventoryGui.IsVisible())
                {
                    return;
                }

                if (!ZInput.GetButtonDown("Use") && !ZInput.GetButtonDown("JoyUse"))
                {
                    return;
                }

                var pos = Input.mousePosition;
                var item = __instance.m_playerGrid.GetItem(
                    new Vector2i(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y)));

                // Not a socketable item under the cursor: leave the press for vanilla (closes the inventory).
                if (item == null || !item.IsMagic(out var magicItem) || !magicItem.HasSockets())
                {
                    return;
                }

                // We own this Use press. Consume it so vanilla's later GetButtonDown("Use") is false and
                // it won't Hide() the inventory out from under the overlay.
                ZInput.ResetButtonStatus("Use");
                ZInput.ResetButtonStatus("JoyUse");

                // Toggle: pressing Use again on the already-open item closes the overlay.
                if (IsSocketGridOpen && item == OpenEquipment)
                {
                    __instance.CloseContainer();
                }
                else
                {
                    OpenSocketOverlay(__instance, item);
                }
            }
        }

        // Render path only. Opening/toggling is handled by InventoryGui_Update_Patch; here we just draw
        // the open overlay into the container panel and take over UpdateContainer while it is up.
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateContainer))]
        public static class InventoryGui_UpdateContainer_Patch
        {
            [UsedImplicitly]
            private static bool Prefix(InventoryGui __instance)
            {
                if (!IsSocketGridOpen)
                {
                    return true;
                }

                __instance.m_containerHoldTime = 0;

                // If the equipment moved out of its slot or was consumed, close the overlay.
                var stillThere = __instance.m_playerGrid.GetInventory()
                    .GetItemAt(OpenEquipment.m_gridPos.x, OpenEquipment.m_gridPos.y);
                if (stillThere != OpenEquipment)
                {
                    __instance.CloseContainer();
                    return true;
                }

                __instance.m_container.gameObject.SetActive(true);
                __instance.m_containerGrid.UpdateInventory(OpenInventory, null, __instance.m_dragItem);
                __instance.m_containerName.text =
                    Localization.instance.Localize("$mod_epicloot_sockets") + ": " +
                    Localization.instance.Localize(OpenEquipment.m_shared.m_name);

                if (__instance.m_firstContainerUpdate)
                {
                    __instance.m_containerGrid.ResetView();
                    __instance.m_firstContainerUpdate = false;
                }

                return false;
            }
        }

        [HarmonyPatch]
        public static class InventoryGui_Close_Patch
        {
            [UsedImplicitly]
            private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
            {
                yield return AccessTools.DeclaredMethod(typeof(InventoryGui), nameof(InventoryGui.Hide));
                yield return AccessTools.DeclaredMethod(typeof(InventoryGui), nameof(InventoryGui.CloseContainer));
            }

            [UsedImplicitly]
            private static void Prefix(InventoryGui __instance)
            {
                if (!IsSocketGridOpen)
                {
                    return;
                }

                if (Player.m_localPlayer != null)
                {
                    SaveSockets();
                }

                if (__instance.m_takeAllButton != null)
                {
                    __instance.m_takeAllButton.gameObject.SetActive(true);
                }
                if (__instance.m_stackAllButton != null)
                {
                    __instance.m_stackAllButton.gameObject.SetActive(true);
                }

                OpenEquipment = null;
                OpenInventory = null;
            }
        }

        private static void ShowSocketMessage(string reason)
        {
            if (Player.m_localPlayer != null && !string.IsNullOrEmpty(reason))
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize(reason));
            }
        }

        // Only allow legal Runestones/Shards to be dropped into a socket slot, and never more than one
        // per slot. `amount` is `ref` so we can clamp it to 1 and let vanilla's stack-split logic move a
        // single unit into the slot, leaving the remainder of the dragged stack in the source inventory.
        [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.DropItem))]
        public static class InventoryGrid_DropItem_Patch
        {
            [UsedImplicitly]
            private static bool Prefix(InventoryGrid __instance, Inventory fromInventory, ItemDrop.ItemData item,
                ref int amount, Vector2i pos, ref bool __result)
            {
                if (OpenInventory == null)
                {
                    return true;
                }

                // Case 1: dropping an item INTO the socket grid. Only legal socketables, one per slot.
                if (__instance.m_inventory == OpenInventory)
                {
                    if (!ShardSocketManager.CanSocket(OpenEquipment, item, out var reason))
                    {
                        ShowSocketMessage(reason);
                        __result = false;
                        return false;
                    }

                    // A socket holds exactly one shard: move a single unit regardless of the dragged stack size.
                    amount = 1;
                    return true;
                }

                // Case 2: dragging a socketed item OUT onto an occupied slot triggers vanilla's swap, which
                // pushes the destination item back INTO the socket (via fromInventory.MoveItemToThis). The
                // pushed item bypasses Case 1's CanSocket gate, so validate it here against the same rules --
                // otherwise a same-effect shard could be smuggled in past the duplicate check by swapping it
                // for an unrelated socketed shard. Measure duplicates against the sockets that remain after
                // the dragged item leaves.
                if (fromInventory == OpenInventory)
                {
                    var itemAt = __instance.m_inventory.GetItemAt(pos.x, pos.y);
                    if (itemAt != null && itemAt != item)
                    {
                        var remaining = OpenInventory.GetAllItems().FindAll(i => i != item);
                        if (!ShardSocketManager.CanCoexist(OpenEquipment, itemAt, remaining, out var reason))
                        {
                            ShowSocketMessage(reason);
                            __result = false;
                            return false;
                        }

                        // Vanilla's swap moves itemAt's ENTIRE stack into the single socket slot, but a socket
                        // holds one shard and reconstruction hands back a stack of 1 -- the remainder would be
                        // lost. Only a different-named item takes that swap path (a same-named one merges back
                        // onto the stack harmlessly), so reject the lossy case and ask the player to split first.
                        if (itemAt.m_stack > 1 && itemAt.m_shared.m_name != item.m_shared.m_name)
                        {
                            ShowSocketMessage("$mod_epicloot_socket_singlestackonly");
                            __result = false;
                            return false;
                        }
                    }
                }

                return true;
            }
        }
    }
}
