using System.Text.Json;
using mp4Parser;

namespace mp4Parser.Cli;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args is ["--help"] or ["-h"])
        {
            PrintHelp();
            return 64; // EX_USAGE
        }

        string input = args[0];
        bool json = args.Contains("--json", StringComparer.OrdinalIgnoreCase);
        bool strict = args.Contains("--strict", StringComparer.OrdinalIgnoreCase);

        int maxDepth = TryGetIntOption(args, "--max-depth") ?? 64;

        var options = new Mp4ParseOptions
        {
            Strict = strict,
            MaxDepth = maxDepth,
        };

        string localPath = input;

        if (Uri.TryCreate(input, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
        {
            localPath = Path.Combine(Path.GetTempPath(), $"mp4parser_{Guid.NewGuid():N}.mp4");

            // NOTE: FOR VERY LARGE FILES YOU MAY WANT TO ADD PROGRESS REPORTING AND/OR CANCELLATION SUPPORT.
            using var http = new HttpClient();
            using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            await using (var fs = File.Create(localPath))
            {
                await response.Content.CopyToAsync(fs);
            }
        }

        if (!File.Exists(localPath))
        {
            Console.Error.WriteLine($"FILE NOT FOUND: {localPath}");
            return 66; // EX_NOINPUT
        }

        var boxes = Parser.Parse(localPath, options);

        if (json)
        {
            var jsonText = JsonSerializer.Serialize(boxes, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(jsonText);
        }
        else
        {
            Parser.PrintTree(boxes, Console.Out);
        }

        return 0;
    }

    private static int? TryGetIntOption(string[] args, string optionName)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (!args[i].Equals(optionName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (int.TryParse(args[i + 1], out int value))
            {
                return value;
            }
        }

        return null;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("MP4PARSER (NET10 / C#14) - CLI");
        Console.WriteLine();
        Console.WriteLine("USAGE:");
        Console.WriteLine("  mp4parser <PATH_OR_URL> [--json] [--strict] [--max-depth N]");
        Console.WriteLine();
        Console.WriteLine("EXAMPLES:");
        Console.WriteLine("  mp4parser sample.mp4");
        Console.WriteLine("  mp4parser https://example.com/video.mp4 --json");
    }
}
