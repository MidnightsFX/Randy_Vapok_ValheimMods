**3.0.0**
* **Complete rewrite of the slot system.** Equipment and quick slots are now real cells of your
  inventory in hidden rows below the visible grid — one inventory, no wrapper layer. Most of the
  historical item-loss and duplication bugs are impossible by construction in this model.
* **Your items are migrated automatically.** On first login per character, items from the old
  2.x slot storage (including pre-Mistlands saves) move into the new slots. **There is no
  downgrade path** — after a character has saved under 3.0.0, going back to 2.x will not restore
  the old format (the automatic backup still protects the items themselves).
* New **Trinket** equipment slot.
* **Quick slot count is configurable** (0–6, default 3); hotkeys 4–6 are unbound by default.
* **Server-synced balance settings** (requires Jotunn; new hard dependency): slot toggles, quick
  slot count, every Gravestone option, extra inventory rows and base carry weight are
  admin-controlled when the server runs the mod. The mod remains fully client-side installable —
  settings just stay local then.
* **Extra inventory rows** (`Inventory / Extra Inventory Rows`, 0–5): more visible rows; the
  equipment and quick slots move down with the grid and items are re-homed safely when the value
  changes.
* **Base carry weight** (`Inventory / Base Carry Weight`): the player's carry capacity before
  belts; 300 (vanilla) leaves other mods' carry-weight changes untouched.
* **Death fixes**: the "Dont drop … on death" options now actually work (they were inverted),
  kept gear can no longer be deleted by the Hammer/Hardcore world modifiers, the single enlarged
  tombstone can never destroy items when full, and grave items return to their exact slots on
  pickup. New auto-equip-on-pickup options for armor, carry-weight belts and the weapon/shield
  you died holding.
* **Hotkey conflict prevention**: quick slot hotkeys no longer trigger vanilla actions bound to
  the same key (Z no longer makes you sit).
* **Automatic slot backup** to the character save, restored when the slots load empty (e.g.
  after the mod was temporarily removed). `eaqs_restorebackup` console command (cheat).
* **Protections**: container "Stack All" never grabs slot items; optional auto-pickup blocking
  for quick slots.
* **Public API for other mods** — add custom slots, query slot contents, subscribe to changes.
  See `docs/API.md`; an embeddable typed shim (`EquipmentAndQuickSlotsAPI.dll`) is available
  from the repository.
* Console commands reworked: `eaqs_validate` repairs misplaced items (replaces the broken
  `fixinventory`); destructive commands now require cheats.
* Removed the Creature Level and Loot Control integration remnants and the second death
  tombstone (one grave now holds everything, sized to fit).
* Crafting, upgrade and stat tracking paths now run pure vanilla code (fixes missing skill gain
  on upgrades, wrong stat counters, `NoCraftCost` handling and a multi-craft ingredient exploit).

**2.1.14**
* Updated for Valheim 0.219.13 Patch (Bog Witch)

<details>
<summary><b>Changelog History</b> (<i>click to expand</i>)</summary>

**2.1.13**
* Updated for Valheim 0.217.38 Patch
**2.1.12**
* Updated for Valheim 0.217.27
* Updated Auga and CLLC API's
* If Auga is loaded, set the default position of Quick Slot Bar accordingly
* Updated for World Modifiers and Hard/Hardcore Settings
**2.1.11**
* Adjusting InventoryGrid Initialization to prevent Awake from happening before variables are set.
  * This has fixed a compatibility issue that was found with Jewelcrafting allowing EAQS to now be used with Smoothbrain's Jewelcrafting
**2.1.10**
* Fixing Hotkey Bar Binding Texts
## Release 2.1.8 & 2.1.9
* Hildir's Request Updates 0.217.24
* Updated version from 2.1.7 to 2.1.9 because I forgot to change it.
**2.1.7**
* Hildir's Request Updates 0.217.14
**2.1.6**
* Updates needed for Valheim 0.216.7
**2.1.5**
* Fixing Keybinds to defaults if config is messed up and showing None.
  * This was caused by a change in how keybinds are stored.
* Tooltips when using Controllers are now visible and not hiding behind the Equipment Slots
**2.1.4**
* DLL packaged with 2.1.3 was incorrectly built as 2.1.2 and might not have had all the changes in it.
* Bumping version by 1 and reuploading correct version.
**2.1.3**
  * Improved Controller Support Between Hotbars/Inventories
    * Known Bug: The weight calculation is still not working when using controller to transfer items.
  * Updated Keybindings to Support Controllers
  * Rebuilt QuickSlotHotkeyBar from the Ground Up
    * No longer a Prefix that blocks UpdateIcons
    * Allows other mods to affect item icons in the Hotkeybar (like EpicLoot)
    * Potential for Performance Improvement
**2.1.2**
  * Updated for Valheim 0.214.2 Patch
**2.1.1**
  * Fixed compatibility issues with JewelCrafting and MultiUserChest
**2.1.0**
  * Updated for Mistlands!
  * Now uses player.m_customData instead of knownTexts
  * On death, drops equipment in second gravestone
  * Fixed bug where you couldn't move items out of your quickslots
  * Added new config features: DontDropEquipmentOnDeath, DontDropQuickslotsOnDeath, InstantlyReequipArmorOnPickup, InstantlyReequipQuickslotsOnPickup

* 1.0.3
    * Integrated fix for larger containers (this mod was not allowing the same row to be used in containers as it uses in the Inventory)
* 1.0.4
    * Fixed issue where gamepad could not use quick slots
* 1.0.5
    * Fixed issue where the previous fix broke the #8 hotkey...
* 2.0.0 Stability Update
    * Items are saved even if accidentally uninstalling or having an error
    * UIs work better with controller
    * Never drop or lose items on death
* 2.0.1
    * Hotfix for not being able to craft when fully equipped
* 2.0.2
    * Fixed an issue where some equipment was lost on death
    * Re-added the toggles to disable and enable features
* 2.0.3
    * Fixed a bug where players could teleport with items they normally can't
* 2.0.4
    * Fixed gamepad navigation for the crafting recipe list
    * Fixed bug preventing new characters from being created
* 2.0.5
    * Put in a potential fix for "losing items" on tombstone pickup (they're in your inventory, just outside the grid. This version should fix that.)
* 2.0.6
    * Fix for double upgrade when using EpicLoot with EAQS
    * Fix for items lost in tombstone by [jsza](https://github.com/jsza).
* 2.0.7
    * (This entire update provided by [jsza](https://github.com/jsza))
    * Fix a variety of equipment bugs
    * Fix a variety of pickup/stacking bugs
* 2.0.8
    * Had to update the number to re-upload to ThunderStore
* 2.0.9
    * Quick slots position is now configurable
* 2.0.10
    * Fixed an encumberance bug
* 2.0.11
    * Added support for Project Auga
* 2.0.12
    * Better Valheim+ and Auga positioning for the inventory
* 2.0.14
    * Updated for H&H
* 2.0.15
    * Yet Another Attempt at fixing the lost-equipment-on-death bug

</details>