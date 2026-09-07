using BepInEx;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace EpicLoot.Config;

// Fails the Json.NET dependency loudly instead of cryptically.
//
// Newtonsoft.Json is a shared Thunderstore package (ValheimModding-JsonDotNET), not something this
// mod ships, so whatever sits in BepInEx/plugins is whatever the player's profile resolved. Every
// config Epic Loot has is a json file -- loot tables, magic effects, biome data, the whole patching
// system -- and all of it goes through JsonConvert, so a missing or stale copy takes the mod out
// entirely rather than degrading it.
//
// Neither failure names Json.NET when it happens. A missing assembly throws out of the first config
// class Mono has to build a deserializer for, and an old one throws MissingMethodException from
// inside FilePatching; both surface as one of OUR type names and tell a player nothing about the
// real problem. Both are a player-side install problem, and both are invisible from that message,
// so check it up front and say so.
//
// Nothing in here may reference a Newtonsoft type by name: naming one is exactly what triggers the
// load this is trying to get in front of. Reflection over strings only.
internal static class JsonDotNetCheck
{
    private const string JsonAssembly = "Newtonsoft.Json";
    private const string MergeSettingsType = "Newtonsoft.Json.Linq.JsonMergeSettings";
    // JsonMergeSettings.MergeNullValueHandling arrived in 10.0.1 and is the newest Json.NET API this
    // mod uses (FilePatching's AppendAll action); everything else it touches -- JsonConvert,
    // JObject.Parse, SelectTokens, JObject.Merge itself -- is older, so a copy carrying this property
    // carries the rest. That property IS the compatibility test, and a better one than a version
    // number: the Thunderstore package version (13.0.4) and the assembly version it ships (13.0.0.0)
    // do not match, so a version comparison invites an off-by-one against whichever of the two
    // someone had in mind. It also catches a future Json.NET that drops the property, which an "is it
    // new enough" test would wave through.
    private const string RequiredProperty = "MergeNullValueHandling";

    private const string Needed = "the 'JsonDotNET' package by ValheimModding, 13.0.4 or newer";

    private enum Compat
    {
        Ok,
        NoMergeSettings,
        NoRequiredProperty,
    }

    // False means: do not initialize anything. Every setting this mod has lives in a json file, so
    // there is no degraded mode worth running -- carrying on would just trade one clear error for a
    // stream of null references out of half-loaded config, with Harmony patches already applied on
    // top of it.
    internal static bool Verify()
    {
        Assembly json = Find();
        if (json == null)
        {
            EpicLoot.LogErrorForce("Epic Loot did not start: Json.NET is not installed. Every config " +
                "this mod has is a json file, so nothing loads without it. Install " + Needed +
                " -- your mod manager can add it as a dependency of Epic Loot, or you can drop " +
                "Newtonsoft.Json.dll into BepInEx/plugins by hand.");
            return false;
        }

        Compat compat = Probe(json);
        if (compat == Compat.Ok)
        {
            return true;
        }

        string what = compat == Compat.NoMergeSettings
            ? $"it has no {MergeSettingsType}, so it is not a Json.NET this mod can use"
            : $"its JsonMergeSettings has no {RequiredProperty} property and Epic Loot was built " +
              $"against the one that has it, so the config patcher cannot load against it (a missing " +
              $"property means a pre-10.0.1 copy, which is the usual cause)";
        EpicLoot.LogErrorForce($"Epic Loot did not start: the Json.NET loaded in this profile is the " +
            $"wrong one -- {what}. Found assembly version {json.GetName().Version}{Origin(json)}; " +
            $"Epic Loot needs {Needed}, whose assembly reports version 13.0.0.0. Update it, and " +
            $"delete any Newtonsoft.Json.dll another mod dropped inside its own folder -- the shared " +
            $"package is the only copy that should be installed.{Copies()}");
        return false;
    }

    private static Assembly Find()
    {
        Assembly[] loaded = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < loaded.Length; i++)
        {
            if (string.Equals(loaded[i].GetName().Name, JsonAssembly, StringComparison.OrdinalIgnoreCase))
            {
                return loaded[i];
            }
        }

        // Newtonsoft.Json.dll carries no plugin of its own (the package's version detector is a
        // separate assembly), so whether BepInEx has resolved it by this point in Awake depends on
        // which other mod asked for it first. Ask now.
        try { return Assembly.Load(JsonAssembly); }
        catch (Exception) { return null; }
    }

    private static Compat Probe(Assembly json)
    {
        try
        {
            Type mergeSettings = json.GetType(MergeSettingsType, false);
            if (mergeSettings == null)
            {
                return Compat.NoMergeSettings;
            }

            return mergeSettings.GetProperty(RequiredProperty) == null
                ? Compat.NoRequiredProperty
                : Compat.Ok;
        }
        catch (Exception)
        {
            return Compat.NoMergeSettings;
        }
    }

    private static string Origin(Assembly json)
    {
        try
        {
            string location = json.Location;
            return string.IsNullOrEmpty(location) ? "" : $" at '{location}'";
        }
        catch (Exception)
        {
            return "";
        }
    }

    // Names every folder under BepInEx/plugins holding a Newtonsoft.Json.dll. Two copies is the usual
    // shape of this -- the shared package plus one a mod bundled despite the package telling authors
    // not to -- and this line is the difference between a player reporting "which mod?" and reporting
    // the folder that shipped the stale one. Failure path only.
    private static string Copies()
    {
        try
        {
            string plugins = Paths.PluginPath;
            if (string.IsNullOrEmpty(plugins) || Directory.Exists(plugins) == false)
            {
                return "";
            }

            string[] found = Directory.GetFiles(plugins, "Newtonsoft.Json.dll", SearchOption.AllDirectories);
            if (found.Length == 0)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(" Newtonsoft.Json.dll under BepInEx/plugins:");
            for (int i = 0; i < found.Length; i++)
            {
                // The containing folder is the mod package name, which is the useful part.
                sb.Append($" [{VersionOf(found[i])}] {Path.GetFileName(Path.GetDirectoryName(found[i]))}");
            }

            return sb.ToString();
        }
        catch (Exception)
        {
            return "";
        }
    }

    // Reads the version out of the file's metadata rather than loading it -- loading a second
    // Newtonsoft.Json into the domain is the last thing this should do while diagnosing which one is
    // live.
    private static string VersionOf(string path)
    {
        try { return AssemblyName.GetAssemblyName(path).Version.ToString(); }
        catch (Exception) { return "unreadable"; }
    }
}
