using System.IO;
using System.Text.RegularExpressions;
using GameProfileManager.Models;

namespace GameProfileManager.Services;

public static partial class SteamDetectionService
{
    private static readonly string[] DefaultVdfPaths =
    [
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            @"Steam\steamapps\libraryfolders.vdf")
    ];

    public static List<GameEntry> DetectGames()
    {
        var vdfPath = DefaultVdfPaths.FirstOrDefault(File.Exists);
        if (vdfPath is null)
            return [];

        var libraryPaths = ParseLibraryPaths(File.ReadAllText(vdfPath));
        var games = new List<GameEntry>();

        foreach (var libPath in libraryPaths)
        {
            var appsDir = Path.Combine(libPath, "steamapps");
            if (!Directory.Exists(appsDir))
                continue;

            foreach (var acfFile in Directory.GetFiles(appsDir, "appmanifest_*.acf"))
            {
                try
                {
                    var game = ParseAcf(File.ReadAllText(acfFile), appsDir);
                    if (game is not null)
                        games.Add(game);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        return games.OrderBy(g => g.Name).ToList();
    }

    private static List<string> ParseLibraryPaths(string vdfContent)
    {
        var paths = new List<string>();
        foreach (var match in PathPattern().Matches(vdfContent).Cast<Match>())
        {
            var path = match.Groups[1].Value.Replace(@"\\", @"\");
            if (Directory.Exists(path))
                paths.Add(path);
        }
        return paths;
    }

    private static GameEntry? ParseAcf(string acfContent, string appsDir)
    {
        var name = ExtractValue(acfContent, "name");
        var installDir = ExtractValue(acfContent, "installdir");
        var appId = ExtractValue(acfContent, "appid");

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(installDir))
            return null;

        // Skip Steamworks shared redistributables
        if (name.Contains("Steamworks") || name.Contains("Redistributable"))
            return null;

        var fullPath = Path.Combine(appsDir, "common", installDir);
        if (!Directory.Exists(fullPath))
            return null;

        return new GameEntry
        {
            Name = name,
            InstallDir = fullPath,
            AppId = appId ?? string.Empty,
            Source = GameSource.Steam
        };
    }

    private static string? ExtractValue(string content, string key)
    {
        var match = Regex.Match(content, $@"""{key}""\s+""([^""]+)""", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    [GeneratedRegex(@"""path""\s+""([^""]+)""")]
    private static partial Regex PathPattern();
}
