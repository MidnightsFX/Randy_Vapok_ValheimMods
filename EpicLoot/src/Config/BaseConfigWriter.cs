using System;
using System.IO;

namespace EpicLoot.Config
{
    /// <summary>
    /// Writes a baseconfig file the mod itself rebuilds from the live config (the equipment auto-add pass,
    /// the creature sorter), and leaves it alone when it already holds exactly that text, so a pass that
    /// changed nothing neither touches the file nor needs it read back.
    /// </summary>
    internal static class BaseConfigWriter
    {
        internal enum Result
        {
            Written,
            Unchanged,
            Failed
        }

        /// <param name="configName">The file's name without ".json", as ConfigVersionManager keys it.</param>
        internal static Result Write(string configName, string contents, string reason)
        {
            try
            {
                string path = Path.Combine(ELConfig.GetOverhaulDirectoryPath(), $"{configName}.json");
                string previousContents = File.Exists(path) ? File.ReadAllText(path) : null;
                if (contents == previousContents)
                {
                    return Result.Unchanged;
                }

                File.WriteAllText(path, contents);
                // Claim this as the mod's own output ONLY when the baseline it merged over was also
                // ours -- a player-edited baseline must stay flagged as the player's.
                ConfigVersionManager.RecordWrittenContent(configName, contents, previousContents);
                return Result.Written;
            }
            catch (Exception e)
            {
                EpicLoot.LogError($"Failed to write {configName}.json ({reason}): {e.Message}");
                return Result.Failed;
            }
        }
    }
}
