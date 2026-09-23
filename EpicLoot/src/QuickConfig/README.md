# Quick Configure: prefab ↔ code contract

The Quick Configure panel is **authored in Unity** (`ValheimUnity/Assets/EpicLoot/Prefabs/UI/QuickConfigure/`,
shipped in the `epicloot` bundle) and **bound by code** in this folder. Maintainers move, resize, restyle
and add rows in the editor; the code only finds rows by their GameObject name and wires the named
widget children below. Nothing here positions anything.

The editor-side generator that produced the initial prefabs is
`ValheimUnity/Assets/Scripts/Editor/QuickConfigPrefabBuilder.cs` (menu `Mod/Quick Configure/...`).
It only creates pages that do not exist yet, so hand edits survive; row templates and the shell can be
regenerated on purpose.

## Rows and pages are identified by GameObject name (no baked components)

The prefabs carry **no EpicLoot components**, by design: they stay independent of the mod assembly,
so a rebuilt or renamed `EpicLoot.dll` can never turn a row into a missing script, and the prefabs
open cleanly in an editor that has no mod DLLs at all. (Historically the editor could not load
`EpicLoot.dll` either, until the MagicaCloth 2 asset satisfied `assembly_valheim`'s reference.) Instead:

- A row's **GameObject name is its key** (`GlobalDropRateModifier`, `json:adventuredata:Gamble.GamblesCount`,
  `action:preset:balanced`). Rename the object to rebind it. Decorative rows keep their template name
  (`Row_Header`, `Row_Text`) and are not bound.
- An optional tooltip override is an **inactive child named `Tooltip`** holding a `TMP_Text`; when
  present its text replaces the setting's own description.
- A page prefab's root name is the page id (`Page_Balance`); its title token is derived by code
  (`$mod_epicloot_cfg_page_balance`). An optional inactive child `Title` with a `TMP_Text` overrides it.

## Row key rule (the row GameObject's name)

| Key shape | Meaning | Examples |
|---|---|---|
| `<ELConfig static field>` | a BepInEx entry on `EpicLoot.Config.ELConfig` | `GlobalDropRateModifier`, `_adventureModeEnabled`, `ShowQuickConfigButton` |
| `<field>.<part>` | one part of a composite entry | `AbilityKeyCodes.0`, `AbilityBarPosition.x` |
| `Common.<Entry>` | a Common-layer entry on `Common.ModContext` | `Common.EnableDebugMode`, `Common.ConfigApplyDelay` |
| `json:<file>:<path>` | a value in a baseconfig JSON (host only) | `json:adventuredata:Gamble.GamblesCount`, `json:loottables:SocketCounts.Magic`, `json:magiceffects:TripleBowShot.Chance`, `json:magiceffects:Riches` |
| `action:<verb>:<arg>` | a button | `action:preset:balanced`, `action:reset:TraderPanelPosition`, `action:url:discord` |
| `readout:<name>` | code-fed text | `readout:dropmix`, `readout:template` |

Unknown keys log one warning and the row is disabled. A registered key with no row is simply not shown.
`Row_Color` rows carry the colour key; the icon-index entry is derived (`_<rarity>MaterialIconColor`).

## Widget children (found by name inside the row)

Every row root: `RectTransform` + `LayoutElement`, named after its key, laid out by the column's
`VerticalLayoutGroup`. Inside the row a `HorizontalLayoutGroup` places the children; the code never
sets positions. Texts are TextMeshPro (`TMP_Text` / `TMP_InputField`); the placeholder font is replaced at
runtime by `QuickConfigStyle` (`MagicFontManager`).

| Template | Children the code binds |
|---|---|
| `Row_Toggle` | `Label`, `Toggle` (uGUI `Toggle`) |
| `Row_Slider` | `Label`, `Slider` (uGUI `Slider`), `Value` (`TMP_InputField`, typed value, two-way) |
| `Row_Cycle` | `Label`, `Cycle` (`Button` whose child `Text` shows the current option) |
| `Row_Picker` | `Label`, `Field` (`TMP_InputField`, free text), `Pick` (`Button` opening the picker overlay) |
| `Row_TextField` | `Label`, `Field` (`TMP_InputField`), optional `Status` (`TMP_Text` under the field) |
| `Row_Flags` | `Label`, `Flags` (container) → `Flag` template (`Toggle` + `Label`); code clones `Flag` per enum member and hides the template |
| `Row_Button` | `Button` (`Button` + child `Text`), optional `Label` (text beside it) |
| `Row_Header` | `Label` |
| `Row_Text` | `Label` (wrapping; readouts and notes) |
| `Row_Color` | `Label`, `Swatch` (`Image`), `Field` (`TMP_InputField`, name or `#hex`), `Pick` (`Button`), `Cycle` (`Button` + `Text`, icon index) |
| `Row_EffectConfigs` | `Head` → `Label`, `Effect` (`Button` + child `Text`: the shown effect; opens the picker over every loaded effect with a Config, editable ones first sorted by name, read-only ones last marked "(read-only)"), `Add` (`Button`, shown only for open-key effects, i.e. Riches); `Items` (`ScrollRect`; `content` holds an inactive `Item` template: `Key` (`TMP_InputField`, read-only unless open-key), `Pick` (`Button`, ObjectDB item picker, open-key effects in a world only), `Value` (`TMP_InputField`, float), `Remove` (`Button`, open-key only)). Key `json:magiceffects:EffectConfigs`; writes `shardstones.json` and/or `magiceffects.json` depending on the effect. Each item row carries its key's localized label as tooltip; the row tooltip sits on `Head`. |
| `Row_Bounties` | `Head` → `Label`, `Biome` (`Button` + child `Text`: the shown biome; opens the picker over the file's biomes plus every registry biome), `Add` (`Button`); `Items` (`ScrollRect`; `content` holds an inactive `Item` template: `Target` (`TMP_InputField`, creature prefab name), `Pick` (`Button`, monster picker; hidden when nothing is known), `Iron`, `Gold`, `Coins` (`TMP_InputField` ints), `Remove` (`Button`)). Key `json:adventuredata:Bounties.Targets`; every biome is staged, the shown one is row UI state. The tooltip sits on `Head`. |
| `Row_BiomeCosts` | `Head` → `Label`; `Items` (`ScrollRect`; its `content` holds an inactive `Item` template, cloned per biome of `TreasureMap.BiomeInfo`: `Name` (`TMP_Text`, read-only biome name), `Cost` (`TMP_InputField` int coins, -1 = not offered), `Tokens` (`TMP_InputField` int forest tokens)). No Add/Remove: the biome set is the file's. Key `json:adventuredata:TreasureMap.BiomeInfo`. The tooltip sits on `Head`. |
| `Row_BiomeDrops` | `Head` → `Label`, `Biome` (`Button` + child `Text`: the shown biome; opens the picker over the biomes that have drop tables, plus "Other"); `Items` (`ScrollRect`; `content` holds an inactive `Item` template, cloned per drop target of the shown biome: `Name` (`TMP_Text`, e.g. "Tier1Mob · level 2 (Greydwarf, Skeleton)"), `Amount` (`TMP_InputField`, `count:weight` table), `Rarity` (`TMP_InputField`, one weight per rarity, comma-separated)). No Add/Remove. Key `json:loottables:LootTables`; every biome is staged, the shown one is row UI state. The tooltip sits on `Head`. |
| `Row_ItemCategories` | `Head` → `Label`, `Category` (`Button` + child `Text`: the shown category; opens the picker over every `ItemInfo[].Type`), `Add` (`Button`); `Items` (`ScrollRect`; `content` holds an inactive `Item` template: `Boss` (`Button` + child `Text`: boss key; opens the picker over `none` + the known keys), `Field` (`TMP_InputField` item prefab name), `Pick` (`Button`, ObjectDB item picker, hidden on the main menu), `Remove` (`Button`)). Key `json:iteminfo:ItemInfo`; every category is staged, the shown one is row UI state. The tooltip sits on `Head`. |

Overlays, instantiated by code under `GUIManager.CustomGUIFront`:

- `QuickConfigPicker`: `Panel` → `Title`, `Search` (`TMP_InputField`), `List` (`ScrollRect`, content holds
  an `Entry` template `Button` + `Text`), `Close` (`Button`).
- `QuickConfigConfirm`: `Panel` → `Title`, `Body`, `Keep`, `Discard`, `SaveClose` (`Button`s).

Shell `QuickConfigPanel` (root stretched full screen, carries vanilla `Localize`):
`Overlay` (dim `Image`, blocks clicks) → `Panel` → `Title`, `Close`, `Pages` (empty container the page
prefabs are instantiated into), `Status`, `Back`, `Save`, `Next`, `Finish`.

Pages `Pages/Page_<Name>` (root stretched, named after the page id): one `Column` or `Left` + `Right`
columns, each with a `VerticalLayoutGroup`; children are nested instances of the row templates, each
renamed to its key. Page order lives in code (`QuickConfigureTool.PageOrder`); a page prefab missing
from the bundle is skipped with a warning.

## Text and localization

Row `Label` text is plain English in the prefab (maintainers edit it in place). Any label, title or
button text that starts with `$` is run through `Localization.instance.Localize` by the code, so a
label can be switched to a token later. Page titles, nav buttons, the welcome text and status strings are
`$mod_epicloot_cfg_*` tokens in `localizations/English.json`.

## Rules for the save path

- `.cfg` writes happen in one batch (`SaveOnConfigSet` off, one `Save()`), only for entries whose staged
  value differs from the baseline taken when the panel opened.
- JSON writes edit the on-disk baseconfig as a `JObject` (only the changed tokens) and reload it through
  `ELConfig.ReloadBaseConfigsFromDisk`. They **never** call `ConfigVersionManager.RecordWrittenContent`:
  panel edits are the player's work, and stamping them as the mod's would let the next update overwrite
  them silently. The Balance preset rewrite from the embedded overhaul *does* record itself.
- JSON rows and the preset buttons are read-only off-host.
