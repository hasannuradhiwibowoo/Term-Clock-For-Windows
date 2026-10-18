using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace TermClock;

internal static class Program
{
    private const int StdOutputHandle = -11;
    private const uint EnableVirtualTerminalProcessing = 0x0004;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int standardHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetConsoleMode(IntPtr consoleHandle, out uint mode);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetConsoleMode(IntPtr consoleHandle, uint mode);

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

        var use24Hour = !args.Contains("--12-hour", StringComparer.OrdinalIgnoreCase)
            || args.Contains("--24-hour", StringComparer.OrdinalIgnoreCase);
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
        var alternateScreen = EnterAlternateScreen();
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
                var clockText = now.ToString(use24Hour ? "HH:mm" : "hh:mm", CultureInfo.InvariantCulture);
                var width = Console.WindowWidth;
                var height = Console.WindowHeight;
                if (width != previousWidth || height != previousHeight)
                {
                    Console.Clear();
                    previousWidth = width;
                    previousHeight = height;
                    lastClockText = string.Empty;
                }

                if (clockText != lastClockText)
                {
                    Render(use24Hour, now);
                    lastClockText = clockText;
                }

                Thread.Sleep(80);
            }
        }
        finally
        {
            Console.ResetColor();
            Console.CursorVisible = true;
            if (alternateScreen is { } session)
            {
                Console.Write("\u001b[?1049l");
                Console.Out.Flush();
                SetConsoleMode(session.Handle, session.OriginalMode);
            }
            else
            {
                Console.Clear();
            }
        }
    }

    private static (IntPtr Handle, uint OriginalMode)? EnterAlternateScreen()
    {
        var handle = GetStdHandle(StdOutputHandle);
        if (handle == IntPtr.Zero || handle == new IntPtr(-1) || !GetConsoleMode(handle, out var originalMode))
            return null;

        if (!SetConsoleMode(handle, originalMode | EnableVirtualTerminalProcessing))
            return null;

        Console.Write("\u001b[?1049h");
        Console.Out.Flush();
        return (handle, originalMode);
    }

    private static void Render(bool use24Hour, DateTimeOffset now)
    {
        var clockText = now.ToString(use24Hour ? "HH:mm" : "hh:mm", CultureInfo.InvariantCulture);
        var width = Math.Max(1, Console.WindowWidth);
        var height = Math.Max(1, Console.WindowHeight);
        var unit = Math.Clamp((width - 2) / 28, 1, 8);
        while (unit > 1 && GetDigitalClockHeight(unit) + 2 > height)
            unit--;

        var date = now.ToString("ddd MMM dd", CultureInfo.GetCultureInfo("en-US"));
        var digitLines = BuildDigitalClock(clockText, unit);
        var contentHeight = digitLines.Length + 2;
        var top = Math.Max(0, (height - contentHeight) / 2);

        for (var i = 0; i < digitLines.Length; i++)
            WriteCentered(digitLines[i], width, top + i, ConsoleColor.White);

        WriteCentered(date, width, top + digitLines.Length + 1, ConsoleColor.DarkGray);
    }

    private static int GetDigitalClockHeight(int unit)
    {
        var thickness = Math.Max(1, (unit + 1) / 2);
        var vertical = Math.Max(1, (3 * unit + 1) / 2);
        return 3 * thickness + 2 * vertical;
    }

    private static string[] BuildDigitalClock(string time, int unit)
    {
        var thickness = Math.Max(1, (unit + 1) / 2);
        var vertical = Math.Max(1, (3 * unit + 1) / 2);
        var height = GetDigitalClockHeight(unit);
        var output = Enumerable.Range(0, height).Select(_ => new StringBuilder()).ToArray();
        var gap = new string(' ', Math.Max(1, unit / 2));

        for (var index = 0; index < time.Length; index++)
        {
            if (index > 0)
            {
                foreach (var row in output)
                    row.Append(gap);
            }

            if (time[index] == ':')
            {
                AppendColon(output, unit, thickness);
                continue;
            }

            AppendDigit(output, time[index] - '0', unit, thickness, vertical);
        }

        return output.Select(row => row.ToString()).ToArray();
    }

    private static void AppendDigit(StringBuilder[] output, int digit, int unit, int thickness, int vertical)
    {
        const int a = 1, b = 2, c = 4, d = 8, e = 16, f = 32, g = 64;
        var segments = digit switch
        {
            0 => a | b | c | d | e | f,
            1 => b | c,
            2 => a | b | d | e | g,
            3 => a | b | c | d | g,
            4 => b | c | f | g,
            5 => a | c | d | f | g,
            6 => a | c | d | e | f | g,
            7 => a | b | c,
            8 => a | b | c | d | e | f | g,
            9 => a | b | c | d | f | g,
            _ => 0
        };

        var height = output.Length;
        var width = digit == 1 ? unit : 6 * unit;
        var glyph = Enumerable.Range(0, height)
            .Select(_ => Enumerable.Repeat(' ', width).ToArray())
            .ToArray();

        if (digit == 1)
        {
            Fill(glyph, 0, height, 0, unit);
            for (var row = 0; row < glyph.Length; row++)
                output[row].Append(glyph[row]);
            return;
        }

        if ((segments & a) != 0) Fill(glyph, 0, thickness, unit, 4 * unit);
        if ((segments & g) != 0) Fill(glyph, thickness + vertical, thickness, unit, 4 * unit);
        if ((segments & d) != 0) Fill(glyph, 2 * thickness + 2 * vertical, thickness, unit, 4 * unit);
        if ((segments & f) != 0) Fill(glyph, thickness, vertical, 0, unit);
        if ((segments & b) != 0) Fill(glyph, thickness, vertical, 5 * unit, unit);
        if ((segments & e) != 0) Fill(glyph, 2 * thickness + vertical, vertical, 0, unit);
        if ((segments & c) != 0) Fill(glyph, 2 * thickness + vertical, vertical, 5 * unit, unit);

        for (var row = 0; row < glyph.Length; row++)
            output[row].Append(glyph[row]);
    }

    private static void AppendColon(StringBuilder[] output, int unit, int thickness)
    {
        var glyph = Enumerable.Range(0, output.Length)
            .Select(_ => Enumerable.Repeat(' ', 2 * unit).ToArray())
            .ToArray();
        var dotLeft = (2 * unit - unit) / 2;
        var firstDot = output.Length / 3 - thickness / 2;
        var secondDot = 2 * output.Length / 3 - thickness / 2;
        Fill(glyph, firstDot, thickness, dotLeft, unit);
        Fill(glyph, secondDot, thickness, dotLeft, unit);

        for (var row = 0; row < glyph.Length; row++)
            output[row].Append(glyph[row]);
    }

    private static void Fill(char[][] glyph, int top, int height, int left, int width)
    {
        for (var row = top; row < top + height; row++)
            for (var column = left; column < left + width; column++)
                glyph[row][column] = '█';
    }

    private static void WriteCentered(string text, int width, int row, ConsoleColor color)
    {
        if (row < 0 || row >= Console.WindowHeight)
            return;

        var clipped = text.Length > width ? text[..width] : text;
        var left = Math.Max(0, (width - clipped.Length) / 2);
        Console.SetCursorPosition(0, row);
        Console.Write(new string(' ', Math.Max(0, width - 1)));
        Console.SetCursorPosition(left, row);
        Console.ForegroundColor = color;
        Console.Write(clipped);
    }

    private static void PrintHelp()
    {
        Console.WriteLine("TermClock 0.1.0 — a digital terminal clock");
        Console.WriteLine();
        Console.WriteLine("Usage: termclock [--12-hour] [--utc] [--once]");
        Console.WriteLine("  24-hour format is the default");
        Console.WriteLine("  --12-hour  Show a 12-hour clock");
        Console.WriteLine("  --24-hour  Use 24-hour format");
        Console.WriteLine("  --utc      Show UTC instead of local time");
        Console.WriteLine("  --once     Print the time once and exit");
        Console.WriteLine("  --version  Show version");
        Console.WriteLine("  --help     Show this help");
        Console.WriteLine();
        Console.WriteLine("Press Q or Esc to quit the live clock.");
    }
}
