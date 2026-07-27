using EpicLoot.Config;
using HarmonyLib;
using Jotunn.Managers;
using System;
using System.Linq;

namespace EpicLoot;

/// <summary>
/// Offers to refresh base configs that were left behind by an older version of the mod. Uses the
/// vanilla UnifiedPopup so no new UI assets are needed; FejdStartup itself relies on it, so it is
/// always present at the main menu.
/// </summary>
[HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Start))]
public static class ConfigUpdatePrompt_FejdStartup_Start_Patch
{
    private static bool _shownThisSession;

    public static void Postfix()
    {
        if (!ShouldPrompt())
        {
            return;
        }

        _shownThisSession = true;

        try
        {
            UnifiedPopup.Push(new YesNoPopup(
                Localization.instance.Localize("$el_configupdate_title"),
                BuildBody(),
                OnUpdate,
                OnNotNow,
                localizeText: false));
        }
        catch (Exception e)
        {
            // Never let a cosmetic prompt break the main menu; the detection warning is already in
            // the log, so the player still has a way to find out.
            EpicLoot.LogWarningForce($"Could not show the Epic Loot config update prompt.\n{e}");
        }
    }

    private static bool ShouldPrompt()
    {
        // Declines are recorded per file during detection, so anything still listed here is both
        // player-modified and unacknowledged for the current default.
        if (_shownThisSession || !ConfigVersionManager.DetectionRan || !ConfigVersionManager.HasOutdatedConfigs)
        {
            return false;
        }

        // A dedicated server has no main menu; the detection warning in the log is its only surface.
        if (GUIManager.IsHeadless() || !UnifiedPopup.IsAvailable())
        {
            return false;
        }

        // Don't stack on top of the first-run welcome panel.
        return !ConfigVersionManager.WelcomeMessageWillShow;
    }

    private static string BuildBody()
    {
        string fileList = string.Join("\n", ConfigVersionManager.OutdatedConfigs
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Select(x => $" - {x}.json"));

        return string.Format(Localization.instance.Localize("$el_configupdate_body"),
            EpicLoot.Version, fileList);
    }

    private static void OnUpdate()
    {
        ConfigVersionManager.BackupAndResetOutdatedConfigs();
        UnifiedPopup.Pop();
    }

    private static void OnNotNow()
    {
        ConfigVersionManager.DeclineOutdatedConfigs();
        UnifiedPopup.Pop();
    }
}
