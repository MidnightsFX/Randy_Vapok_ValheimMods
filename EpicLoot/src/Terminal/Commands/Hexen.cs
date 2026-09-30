using System.Collections.Generic;
using System.Globalization;
using EpicLoot.Transformation;

namespace EpicLoot;

public static partial class TerminalManager
{
    private static void Hexen(Terminal.ConsoleEventArgs args)
    {
        var player = Player.m_localPlayer;
        var form = player != null ? player.GetComponent<HexenForm>() : null;
        if (form == null)
        {
            Console.instance.Print("> No local player to transform");
            return;
        }

        float scale;
        if (args.Length < 2)
        {
            scale = form.Active ? 0f : HexenForm.DefaultScale;
        }
        else if (args[1].Equals("off", System.StringComparison.OrdinalIgnoreCase))
        {
            scale = 0f;
        }
        else if (!float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out scale) || scale <= 0f)
        {
            Console.instance.Print("> Usage: hexen [off|scale] (scale is a multiplier on her native size, 1 = full Hexen)");
            return;
        }

        if (!form.Set(scale))
        {
            Console.instance.Print("> Could not become a Hexen; see the log");
            return;
        }

        if (scale <= 0f)
        {
            Console.instance.Print("> Back to your own body");
            return;
        }

        Console.instance.Print($"> You are a Hexen at {scale.ToString(CultureInfo.InvariantCulture)}x her size");
        Console.instance.Print(">   attack: magic blast | secondary attack: lightning bolt");
        Console.instance.Print(">   jump: take off, then hold jump/crouch to rise/sink; sink onto the ground to land");
        Console.instance.Print(">   block+jump or sneak+jump: blink in the move direction");
    }

    private static List<string> GetHexenOptions(string[] args)
    {
        return args.Length == 2 ? ["off", "0.6", "1"] : [];
    }
}
