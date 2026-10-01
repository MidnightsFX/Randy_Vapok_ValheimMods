using System.Collections.Generic;
using System.Linq;

namespace EpicLoot;

public static partial class TerminalManager
{
    private static void CheatEffectRarity(Terminal.ConsoleEventArgs args)
    {
        if (args.Length >= 2 && int.TryParse(args[1], out var tier))
        {
            LootRoller.CheatEffectRarityTier = UnityEngine.Mathf.Clamp(tier, 0, MagicEffectRarity.MaxTier);
        }

        LootRoller.CheatEffectRarityFirstOnly =
            args.Length >= 3 && args[2].Equals("first", System.StringComparison.OrdinalIgnoreCase);

        if (LootRoller.CheatEffectRarityTier <= 0)
        {
            Console.instance.Print("> Effect rarity cheat off; effects roll on their normal SelectionWeight");
            PrintTierBreakdown();
            return;
        }

        var scope = LootRoller.CheatEffectRarityFirstOnly ? "the first effect" : "every effect";
        Console.instance.Print($"> Forcing {scope} to be at least {LootRoller.CheatEffectRarityTier} star" +
            $"{(LootRoller.CheatEffectRarityTier == 1 ? "" : "s")} ({DescribeTierPool()})");
        PrintTierBreakdown();
    }

    private static void PrintTierBreakdown()
    {
        var byTier = MagicItemEffectDefinitions.AllDefinitions.Values
            .GroupBy(MagicEffectRarity.GetTier)
            .ToDictionary(g => g.Key, g => g.Count());

        Console.instance.Print($"> anchor (median rollable SelectionWeight) = {MagicEffectRarity.GetAnchorWeight()}");
        for (var tier = MagicEffectRarity.MaxTier; tier >= 1; tier--)
        {
            byTier.TryGetValue(tier, out var count);
            Console.instance.Print($">   {tier} star: {count} effects");
        }

        byTier.TryGetValue(0, out var plain);
        Console.instance.Print($">   unstarred: {plain} (includes every NoRoll and deprecated effect)");
    }

    // The pool an item can actually draw from is narrower than the global count -- effects are gated by
    // item type and rarity -- so this is an upper bound, and says plainly when a tier is empty in the
    // loaded config. The legendary and minimal overhauls have no 3-star effects at all.
    private static string DescribeTierPool()
    {
        var count = MagicItemEffectDefinitions.AllDefinitions.Values
            .Count(x => MagicEffectRarity.GetTier(x) >= LootRoller.CheatEffectRarityTier);

        return count == 0
            ? "no effect in the loaded config qualifies, rolls will fall back to normal weights"
            : $"{count} of {MagicItemEffectDefinitions.AllDefinitions.Count} effects qualify";
    }

    private static List<string> GetCheatEffectRarityOptions(string[] args)
    {
        return args.Length switch
        {
            2 => ["0", "1", "2", "3"],
            3 => ["all", "first"],
            _ => []
        };
    }
}
