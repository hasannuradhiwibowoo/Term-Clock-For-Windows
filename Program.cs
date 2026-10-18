using System.Globalization;
using System.Text;

namespace TermClock;

internal static class Program
{
    private static readonly string[][] Digits =
    [
        [" ███ ", "█   █", "█   █", "█   █", " ███ "],
        ["  █  ", " ██  ", "  █  ", "  █  ", " ███ "],
        [" ███ ", "█   █", "   █ ", "  █  ", "█████"],
        ["████ ", "    █", " ███ ", "    █", "████ "],
        ["█   █", "█   █", "█████", "    █", "    █"],
        ["█████", "█    ", "████ ", "    █", "████ "],
        [" ███ ", "█    ", "████ ", "█   █", " ███ "],
        ["█████", "    █", "   █ ", "  █  ", "  █  "],
        [" ███ ", "█   █", " ███ ", "█   █", " ███ "],
        [" ███ ", "█   █", " ████", "    █", " ███ "]
    ];

    private static readonly string[] Colon = ["   ", " ░ ", "   ", " ░ ", "   "];

    private static int Main(string[] args)
    {
        if (args.Contains("--help", StringComparer.OrdinalIgnoreCase) || args.Contains("-h", StringComparer.OrdinalIgnoreCase))
        {
            PrintHelp();
            return 0;
        }

        if (args.Contains("--version", StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine("TermClock 0.1.0");
            return 0;
        }

        var use24Hour = args.Contains("--24-hour", StringComparer.OrdinalIgnoreCase);
        var utc = args.Contains("--utc", StringComparer.OrdinalIgnoreCase);
        var once = args.Contains("--once", StringComparer.OrdinalIgnoreCase);

        if (Console.IsOutputRedirected || once)
        {
            var now = utc ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
            Console.WriteLine(now.ToString(use24Hour ? "HH:mm:ss" : "hh:mm:ss tt", CultureInfo.InvariantCulture));
            return 0;
        }

        RunClock(use24Hour, utc);
        return 0;
    }

    private static void RunClock(bool use24Hour, bool utc)
    {
        Console.CursorVisible = false;
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            Environment.Exit(0);
        };

        try
        {
            Console.Clear();
            var lastClockText = string.Empty;
            var previousWidth = Console.WindowWidth;
            var previousHeight = Console.WindowHeight;

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true).Key;
                    if (key is ConsoleKey.Q or ConsoleKey.Escape)
                        break;
                }

                var now = utc ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
                var clockText = now.ToString(use24Hour ? "HH:mm:ss" : "hh:mm:ss", CultureInfo.InvariantCulture);
                var width = Console.WindowWidth;
                var height = Console.WindowHeight;
                if (width != previousWidth || height != previousHeight)
                {
                    Console.Clear();
                    previousWidth = width;
                    previousHeight = height;
                }

                if (clockText != lastClockText)
                {
                    Render(use24Hour, utc, now);
                    lastClockText = clockText;
                }

                Thread.Sleep(80);
            }
        }
        finally
        {
            Console.ResetColor();
            Console.CursorVisible = true;
            Console.Clear();
        }
    }

    private static void Render(bool use24Hour, bool utc, DateTimeOffset now)
    {
        var clockText = now.ToString(use24Hour ? "HH:mm:ss" : "hh:mm:ss", CultureInfo.InvariantCulture);
        var hourSuffix = use24Hour ? string.Empty : now.ToString("tt", CultureInfo.InvariantCulture);
        var width = Math.Max(1, Console.WindowWidth);
        var height = Math.Max(1, Console.WindowHeight);

        var lines = BuildClock(clockText);
        var date = now.ToString("dddd, dd MMMM yyyy", CultureInfo.GetCultureInfo("en-US"));
        var zone = utc ? "UTC" : TimeZoneInfo.Local.StandardName;
        var contentHeight = lines.Length + 6;
        var top = Math.Max(0, (height - contentHeight) / 2);

        WriteCentered("TERMCLOCK", width, top, ConsoleColor.DarkCyan);
        for (var i = 0; i < lines.Length; i++)
            WriteCentered(lines[i] + (i == 2 && hourSuffix.Length > 0 ? "  " + hourSuffix : string.Empty), width, top + i + 1, i == 2 ? ConsoleColor.Cyan : ConsoleColor.DarkCyan);

        WriteCentered(date, width, top + lines.Length + 2, ConsoleColor.White);
        WriteCentered(zone, width, top + lines.Length + 3, ConsoleColor.DarkGray);
        WriteCentered("Q / Esc  ·  quit", width, top + lines.Length + 5, ConsoleColor.DarkGray);
    }

    private static string[] BuildClock(string time)
    {
        var output = Enumerable.Range(0, 5).Select(_ => new StringBuilder()).ToArray();
        foreach (var character in time)
        {
            var glyph = character == ':' ? Colon : Digits[character - '0'];
            for (var row = 0; row < glyph.Length; row++)
                output[row].Append(glyph[row]).Append("  ");
        }
        return output.Select(row => row.ToString().TrimEnd()).ToArray();
    }

    private static void WriteCentered(string text, int width, int row, ConsoleColor color)
    {
        if (row < 0 || row >= Console.WindowHeight)
            return;

        var clipped = text.Length > width ? text[..width] : text;
        var left = Math.Max(0, (width - clipped.Length) / 2);
        Console.SetCursorPosition(left, row);
        Console.ForegroundColor = color;
        Console.Write(clipped);
    }

    private static void PrintHelp()
    {
        Console.WriteLine("TermClock 0.1.0 — a colorful terminal clock");
        Console.WriteLine();
        Console.WriteLine("Usage: termclock [--24-hour] [--utc] [--once]");
        Console.WriteLine("  --24-hour  Show a 24-hour clock");
        Console.WriteLine("  --utc      Show UTC instead of local time");
        Console.WriteLine("  --once     Print the time once and exit");
        Console.WriteLine("  --version  Show version");
        Console.WriteLine("  --help     Show this help");
        Console.WriteLine();
        Console.WriteLine("Press Q or Esc to quit the live clock.");
    }
}
