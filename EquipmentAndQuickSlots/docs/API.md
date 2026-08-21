# EquipmentAndQuickSlots integration API

A supported contract for other plugins, so integrating with EAQS does not mean Harmony-patching
its internals and re-breaking on every release.

There are two layers, and you can use either:

| | What it is | When to use it |
|---|---|---|
| **`EquipmentAndQuickSlots.API`** | A `static partial class` in `EquipmentAndQuickSlots.dll`. The actual ABI. | You are comfortable with `Type.GetType("EquipmentAndQuickSlots.API, EquipmentAndQuickSlots")` and reflection, or you already have your own glue. |
| **`EquipmentAndQuickSlotsAPI.dll`** | A small shim that wraps every endpoint in typed methods and does the reflection for you. Bundle it with ILRepack. | Almost always. See [the shim README](../../EquipmentAndQuickSlotsAPI/EquipmentAndQuickSlotsAPI/README.md). |

EAQS's plugin GUID is `randyknapp.mods.equipmentandquickslots`. Declare it as a soft dependency:

```csharp
[BepInDependency("randyknapp.mods.equipmentandquickslots", BepInDependency.DependencyFlags.SoftDependency)]
```

## The signature rule

Nothing but primitives, `string`, vanilla/Unity types (`ItemDrop.ItemData`, `Vector2i`) and
`System.Func`/`Action` built from those ever crosses this boundary. **No EAQS type appears in
any signature.** Structured data travels as JSON strings; slots are identified by string ids.
Endpoints use `ref` rather than `out`, so a reflection transport can read mutated arguments back
out of the `object[]` (the shim presents them as normal `out` parameters).

## Versioning

```csharp
API.ApiVersion            // const int, currently 1
API.GetApiVersion()       // same value, as a call
API.GetPluginVersion()    // "3.0.0"
API.GetPluginId()         // "randyknapp.mods.equipmentandquickslots"
API.HasEndpoint(name)     // feature probe for a specific method
API.GetEndpointNames()    // every public endpoint, for diagnostics
```

`ApiVersion` is bumped on every additive change and is independent of the plugin version. If you
rely on something newer than the oldest EAQS you support, gate it on `HasEndpoint`. Through the
shim, a missing endpoint logs a warning and no-ops rather than throwing.

## The slot model

The player inventory is `8 × (visibleRows + 2)`; the two hidden rows are the slot region.
Built-in slot ids: `Quick1`–`Quick6`, `Helmet`, `Chest`, `Legs`, `Shoulder`, `Utility`,
`Trinket`. Four reserved cells are available for custom slots. Slot items are ordinary inventory
items at their slot's grid position — they persist through vanilla save/load, and anything that
enumerates the player inventory sees them.

Equipment cells are **equipped-only**: they hold exactly the items the player is wearing.
Unequipping moves the item to the visible grid automatically.

## Custom slots

```csharp
bool AddSlot(string slotId, string ownerPluginGuid, string nameToken,
             Func<ItemDrop.ItemData, bool> isValid, Func<bool> isActive);
bool RemoveSlot(string slotId);
```

- `slotId` must be unique — namespace it with your plugin name (`"MyModQuiver"`, not `"Quiver"`).
- `ownerPluginGuid` is recorded on the slot for diagnostics (`eaqs_api` prints it).
- `nameToken` may be a localization token (`$mymod_quiver`) or plain text; shown as the cell label.
- `isValid` decides which items the player may place in the slot (null = any item).
- `isActive` gates the slot's visibility live (config toggles, progression, ...). An item sitting
  in a slot that becomes inactive is relocated to the regular inventory, never lost.
- `AddSlot` returns `false` when the id is taken or all four custom cells are in use. Capacity
  can grow in a future version (adding a hidden row is save-safe), so do not treat four as a
  contract.
- `RemoveSlot` rescues a resident item into the inventory (ground as a last resort) and returns
  `false` for unknown ids. Re-register your slots on every launch — slot *definitions* are not
  persisted, only their contents are.
- Every consumer delegate runs inside a try/catch; one that throws is logged with your id —
  always, not gated by EAQS's logging config, because it is your bug — and treated as `false`.

## Queries

```csharp
string GetSlotIdsJson();                            // ["Quick1", ..., "Trinket", "MyModQuiver"]
string GetSlotInfoJson(string slotId);              // {"id":..., "index":..., "active":..., "gridX":..., "gridY":..., "ownerPluginGuid":..., "occupied":...}
bool   TryGetSlotItem(string slotId, ref ItemDrop.ItemData item);
bool   IsSlotCell(int x, int y, ref string slotId); // is this grid position a slot cell?
List<ItemDrop.ItemData> GetQuickSlotItems();
List<ItemDrop.ItemData> GetEquipmentSlotItems();
int    GetVisibleRows();
int    GetFullHeight();
```

## Listeners

Add and remove listeners with plain method calls, so a reflection-only consumer can subscribe
without binding to an event field:

```csharp
API.AddSlotChangedListener(slotId => { /* topology: a custom slot was added/removed */ });
API.AddSlotItemChangedListener((slotId, oldItem, newItem) => { /* item entered/left a slot */ });
// plus a matching Remove* for each
```

`SlotItemChanged` fires once per frame after EAQS's own validation has settled, with `null` for
an empty side of the transition. A listener that throws is logged and does not stop the other
listeners.

## Migration table

| Instead of patching / reflecting | Call |
|---|---|
| Scanning `m_customData["QuickSlotInventory"]` (the 2.x format — gone after migration) | `GetQuickSlotItems` |
| Reflecting on `ExtendedInventory` / `ExtendedPlayerData` (deleted in 3.0) | `GetQuickSlotItems`, `GetEquipmentSlotItems` |
| Assuming the inventory is 4 rows / hardcoding `m_height` | `GetVisibleRows`, `GetFullHeight` |
| Guessing whether a grid position is a slot | `IsSlotCell` |
| Polling for "did this slot change?" | `AddSlotItemChangedListener` |

## Testing your integration

EAQS ships a console command that exercises the API from in-game:

```
eaqs_api    — API version, endpoint list, and per-slot state JSON
```
