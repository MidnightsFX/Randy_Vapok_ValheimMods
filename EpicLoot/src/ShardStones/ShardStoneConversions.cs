using EpicLoot.CraftingV2;

namespace EpicLoot.ShardStones {
    // Previously generated the ShardStone rarity-upgrade recipes shown in the enchanting table's "Upgrade"
    // tab (one recipe per color per step: {color}_{From}_ShardStone -> {color}_{To}_ShardStone).
    //
    // DISABLED by the one-prefab-per-color / metadata-rarity refactor: a shard's rarity is no longer a
    // distinct prefab but per-instance metadata (MagicItem.Rarity, mirrored to m_quality), so there is only
    // one prefab per color ({color}_ShardStone). A MaterialConversion is prefab -> prefab and cannot read or
    // change per-instance rarity, so it can't express "bump this shard's rarity". Registering the old
    // recipes would point the Upgrade tab at prefabs that no longer exist.
    //
    // Until a metadata-aware upgrade path is built, this only strips any previously-generated entries (e.g.
    // from a stale on-disk materialconversions.json) so nothing references missing prefabs. It stays wired to
    // MaterialConversions.OnSetupMaterialConversions so the cleanup re-runs on every config (re)load.
    public static class ShardStoneConversions {
        private const string NamePrefix = "ShardStoneUpgrade_";

        public static void RegisterShardStoneUpgradeConversions() {
            var config = MaterialConversions.Config;
            if (config == null) {
                return;
            }

            var removed = config.MaterialConversions.RemoveAll(c => c.Name != null && c.Name.StartsWith(NamePrefix));
            if (removed == 0) {
                return;
            }

            // Rebuild the live lookup so the removal takes effect immediately on the defensive call path.
            MaterialConversions.Conversions.Clear();
            foreach (var entry in config.MaterialConversions) {
                MaterialConversions.Conversions.Add(entry.Type, entry);
            }
        }
    }
}
