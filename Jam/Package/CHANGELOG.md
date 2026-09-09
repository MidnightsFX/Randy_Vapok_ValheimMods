**2.0.0**

* **Breaking: all config entries are renamed and existing settings reset to their defaults.** Jam now
  registers its items through the shared item loader, so every jam has its own `Food - <Jam Name>`
  section instead of the old `Jam N - <Prefab>` sections. Note down any values you want to keep before
  updating.
* **Breaking: the recipe format changed** from `Item:Amount,Item:Amount` to
  `Item,Amount,AmountPerLevel|Item,Amount,AmountPerLevel` (e.g. `Raspberry,14,0`).
* The crafting station, minimum station level and craft amount are now configurable per jam - they
  were hardcoded to cauldron / their shipped level / 4.
* Config changes apply live and are batched, so editing several values (or receiving a server config
  sync) no longer causes a hitch.
* Recipes are now re-applied when the ObjectDB is rebuilt, fixing jam recipes reverting to their
  defaults after joining a server whose config differs.
* Jams are shown as a single collapsible entry per jam in Configuration Manager rather than a wall of
  loose settings.
* Removed the long-unused `config/recipes.json`.

**1.1.0**

* Existing release; changelog begins here.
