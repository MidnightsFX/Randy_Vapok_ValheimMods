using EpicLoot.Abilities;
using EpicLoot.Adventure;
using EpicLoot.Crafting;
using EpicLoot.CraftingV2;
using EpicLoot.GatedItemType;
using EpicLoot.LegendarySystem;
using EpicLoot.ShardStones;
using EpicLoot_UnityLib;
using Jotunn.Entities;
using Jotunn.Managers;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Globalization;

namespace EpicLoot.General
{
    internal class TerminalCommands
    {
        internal static void AddTerminalCommands()
        {
            CommandManager.Instance.AddConsoleCommand(new LuckTestCommand());
            CommandManager.Instance.AddConsoleCommand(new PrintConfig());
            CommandManager.Instance.AddConsoleCommand(new CheatSocketsCommand());
            CommandManager.Instance.AddConsoleCommand(new SpawnShardCommand());
            CommandManager.Instance.AddConsoleCommand(new MagicShardsCommand());
            CommandManager.Instance.AddConsoleCommand(new SocketInfoCommand());
            CommandManager.Instance.AddConsoleCommand(new SocketCommand());
            CommandManager.Instance.AddConsoleCommand(new UnsocketCommand());
        }

        internal class LuckTestCommand : ConsoleCommand
        {
            public override string Name => "lucktest";
            public override string Help => "Rolls an example loot table with the specified luck eg: lucktest Greydwarf 1.0";
            public override bool IsCheat => true;

            public override void Run(string[] args)
            {
                string lootTable = "Greydwarf";
                float luckFactor = 0f;
                if (args.Length >= 2)
                {
                    lootTable = args[0];
                    if (!float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out luckFactor))
                    {
                        PrintError(args, lootTable, luckFactor);
                        return;
                    }
                }
                else if (args.Length == 1)
                {
                    if (!float.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out luckFactor))
                    {
                        PrintError(args, lootTable, luckFactor);
                        return;
                    }
                }
                else
                {
                    Console.instance.Print($"Using Lucktest Defaults: lucktest {lootTable} {luckFactor}");
                }

                LootRoller.PrintLuckTest(lootTable, luckFactor);
            }

            private void PrintError(string[] args, string lootTable, float luckFactor)
            {
                Console.instance.Print($"lucktest invalid arguments, was 'lucktest {string.Join(" ", args)}' " +
                    $"using the default: 'lucktest {lootTable} {luckFactor}'\n" +
                    $"Supported formats are:\n" +
                    $"  lucktest\n" +
                    $"  lucktest 1.2\n" +
                    $"  lucktest Neck 1.2");
            }
        }

        internal class PrintConfig : ConsoleCommand
        {
            public override string Name => "printconfig";
            public override string Help => "Prints out the Epic Loot current configuration of the specified type";
            public override bool IsCheat => false;

            readonly List<string> ConfigNames = new List<string>() 
            {
                "loottable", "abilities", "adventuredata", "enchantcosts",
                "enchantingupgrades", "iteminfo", "itemnames", "legendaries",
                "magiceffects", "materialconversion", "recipes"
            };

            public override void Run(string[] args)
            {
                string patchType = "loottable";

                if (args.Length >= 1)
                {
                    string type = args[0].Trim().ToLower();
                    if (ConfigNames.Contains(type))
                    {
                        patchType = type;
                    }
                    else
                    {
                        Console.instance.Print($"printconfig argument must be one of [{string.Join(", ", ConfigNames)}]");
                        return;
                    }
                }
                else
                {
                    Console.instance.Print($"Using printconfig Defaults: printconfig {patchType}");
                }

                switch (patchType)
                {
                    case "loottable":
                        EpicLoot.LogWarningForce(JsonConvert.SerializeObject(LootRoller.Config, Formatting.Indented));
                        break;
                    case "abilities":
                        EpicLoot.LogWarningForce(JsonConvert.SerializeObject(AbilityDefinitions.Config, Formatting.Indented));
                        break;
                    case "adventuredata":
                        EpicLoot.LogWarningForce(JsonConvert.SerializeObject(AdventureDataManager.Config, Formatting.Indented));
                        break;
                    case "enchantcosts":
                        EpicLoot.LogWarningForce(JsonConvert.SerializeObject(EnchantCostsHelper.Config, Formatting.Indented));
                        break;
                    case "enchantingupgrades":
                        EpicLoot.LogWarningForce(JsonConvert.SerializeObject(EnchantingTableUpgrades.Config, Formatting.Indented));
                        break;
                    case "iteminfo":
                        EpicLoot.LogWarningForce(JsonConvert.SerializeObject(GatedItemTypeHelper.GatedConfig, Formatting.Indented));
                        break;
                    case "itemnames":
                        EpicLoot.LogWarningForce(JsonConvert.SerializeObject(MagicItemNames.Config, Formatting.Indented));
                        break;
                    case "legendaries":
                        EpicLoot.LogWarningForce(JsonConvert.SerializeObject(UniqueLegendaryHelper.Config, Formatting.Indented));
                        break;
                    case "magiceffects":
                        EpicLoot.LogWarningForce(JsonConvert.SerializeObject(MagicItemEffectDefinitions.AllDefinitions, Formatting.Indented));
                        break;
                    case "materialconversion":
                        EpicLoot.LogWarningForce(JsonConvert.SerializeObject(MaterialConversions.Config, Formatting.Indented));
                        break;
                    case "recipes":
                        EpicLoot.LogWarningForce(JsonConvert.SerializeObject(RecipesHelper.Config, Formatting.Indented));
                        break;
                }
            }
        }

        internal class CheatSocketsCommand : ConsoleCommand
        {
            public override string Name => "cheatsockets";
            public override string Help => "Forces the number of sockets on rolled items (-1 = use rarity caps) eg: cheatsockets 3";
            public override bool IsCheat => true;

            public override void Run(string[] args)
            {
                int n = args.Length > 0 && int.TryParse(args[0], out var v) ? v : -1;
                LootRoller.CheatSocketCount = n;
                Console.instance.Print($"> Cheat socket count set to {n} (-1 = use rarity caps)");
            }
        }

        internal class SpawnShardCommand : ConsoleCommand
        {
            public override string Name => "spawnshard";
            public override string Help => "Spawns a socket shard in your inventory eg: spawnshard <rarity> <color>";
            public override bool IsCheat => true;

            public override void Run(string[] args)
            {
                // SocketDebug.SpawnShard expects the command name at index 0, but Jotunn strips it from args.
                var fullArgs = new string[args.Length + 1];
                fullArgs[0] = Name;
                args.CopyTo(fullArgs, 1);
                ShardDebug.SpawnShard(Console.instance, fullArgs);
            }
        }

        internal class MagicShardsCommand : ConsoleCommand
        {
            public override string Name => "magicshards";
            public override string Help => "Spawns one of each shard color in your inventory eg: magicshards <rarity>";
            public override bool IsCheat => true;

            public override void Run(string[] args)
            {
                // ShardDebug.SpawnAllShards expects the command name at index 0, but Jotunn strips it from args.
                var fullArgs = new string[args.Length + 1];
                fullArgs[0] = Name;
                args.CopyTo(fullArgs, 1);
                ShardDebug.SpawnAllShards(Console.instance, fullArgs);
            }
        }

        internal class SocketInfoCommand : ConsoleCommand
        {
            public override string Name => "socketinfo";
            public override string Help => "Prints socket info for your equipped magic items";
            public override bool IsCheat => true;

            public override void Run(string[] args)
            {
                ShardDebug.PrintSocketInfo(Console.instance);
            }
        }

        internal class SocketCommand : ConsoleCommand
        {
            public override string Name => "socket";
            public override string Help => "Sockets the first eligible shard into the first equipped item with an open socket";
            public override bool IsCheat => true;

            public override void Run(string[] args)
            {
                ShardDebug.SocketFirstEligible(Console.instance);
            }
        }

        internal class UnsocketCommand : ConsoleCommand
        {
            public override string Name => "unsocket";
            public override string Help => "Removes the first socketed shard from your equipped items";
            public override bool IsCheat => true;

            public override void Run(string[] args)
            {
                ShardDebug.UnsocketFirst(Console.instance);
            }
        }
    }
}
