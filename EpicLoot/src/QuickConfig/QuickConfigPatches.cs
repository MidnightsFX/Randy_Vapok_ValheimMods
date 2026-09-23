using HarmonyLib;

namespace EpicLoot.QuickConfig;

// Start runs before the intro cinematic; QueueTutorial only queues a coroutine on the FejdStartup
// that opens the wizard once the main menu is actually ready. Replaces the old WelcomeMessage patch.
[HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Start))]
internal static class QuickConfig_FejdStartup_Start_Patch {
    private static void Postfix(FejdStartup __instance) {
        QuickConfigureTool.QueueTutorial(__instance);
    }
}

// Valheim reads Escape inline in Menu.Update, so while the panel is open the press must be swallowed
// for that frame or it would also collapse the pause menu the panel was opened from. The broker's own
// Menu.Update prefix handles only its mod list, and the Menu.Show prefix in EnchantingUI_Patches only
// blocks the pause menu over the enchanting table, so neither conflicts.
[HarmonyPatch(typeof(Menu), nameof(Menu.Update))]
internal static class QuickConfig_Menu_Update_Patch {
    private static bool Prefix() {
        return QuickConfigureTool.TakeEscape() == false;
    }
}
