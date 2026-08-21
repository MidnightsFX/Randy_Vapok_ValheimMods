using HarmonyLib;

namespace EquipmentAndQuickSlots {
    // Pre-Mistlands versions stored slot inventories in the player's known texts, prefixed with
    // this sentinel. Until a character migrates, those entries would clutter the compendium.
    [HarmonyPatch(typeof(TextsDialog), "UpdateTextsList")]
    public static class TextsDialog_UpdateTextsList_Patch {
        public const string LegacySentinel = "<|>";

        public static void Postfix(TextsDialog __instance) {
            if (!ValConfig.ViewDebugSaveData.Value) {
                __instance.m_texts.RemoveAll(x => x.m_topic.StartsWith(LegacySentinel));
            }
        }
    }
}
