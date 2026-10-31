using BepInEx;
using BepInEx.Bootstrap;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace EpicLoot
{
    /// <summary>
    /// The game build and every loaded plugin, as text for the caches that must start over when either
    /// changes: the creature sorter's location cache (for mod content it cannot pin to a file) and the
    /// equipment auto-add pass's last-run record.
    /// </summary>
    internal static class InstalledMods
    {
        /// <summary>
        /// One line for the game build, then one per plugin sorted by GUID: GUID@version plus its DLL's size
        /// and write time, so a rebuilt DLL that kept its version (a dev build) counts as a change too.
        /// </summary>
        internal static string Describe()
        {
            StringBuilder text = new StringBuilder();
            text.Append(global::Version.GetVersionString(true)).Append('\n');
            foreach (PluginInfo plugin in Chainloader.PluginInfos.Values
                .Where(p => p?.Metadata != null)
                .OrderBy(p => p.Metadata.GUID, StringComparer.Ordinal))
            {
                text.Append(plugin.Metadata.GUID).Append('@').Append(plugin.Metadata.Version);
                AppendFileStamp(text, plugin.Location);
                text.Append('\n');
            }

            return text.ToString();
        }

        /// <summary>|length|write time of a file, or nothing when it cannot be read.</summary>
        internal static void AppendFileStamp(StringBuilder text, string path)
        {
            try
            {
                FileInfo file = string.IsNullOrEmpty(path) ? null : new FileInfo(path);
                if (file != null && file.Exists)
                {
                    text.Append('|').Append(file.Length).Append('|').Append(file.LastWriteTimeUtc.Ticks);
                }
            }
            catch (Exception)
            {
                // A path the OS will not stat stamps as nothing; the GUID and version still count.
            }
        }

        internal static string Hash(string text)
        {
            using (SHA256 sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "");
            }
        }
    }
}
