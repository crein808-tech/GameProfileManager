using System.IO;
using System.Text.Json;
using GameProfileManager.Models;

namespace GameProfileManager.Services;

public static class EpicDetectionService
{
    private static readonly string ManifestDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        @"Epic\EpicGamesLauncher\Data\Manifests");

    public static List<GameEntry> DetectGames()
    {
        if (!Directory.Exists(ManifestDir))
            return [];

        var games = new List<GameEntry>();

        foreach (var file in Directory.GetFiles(ManifestDir, "*.item"))
        {
            try
            {
                using var stream = File.OpenRead(file);
                using var doc = JsonDocument.Parse(stream);
                var root = doc.RootElement;

                var name = root.TryGetProperty("DisplayName", out var n) ? n.GetString() : null;
                var installDir = root.TryGetProperty("InstallLocation", out var d) ? d.GetString() : null;

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(installDir))
                    continue;
                if (!Directory.Exists(installDir))
                    continue;

                games.Add(new GameEntry
                {
                    Name = name,
                    InstallDir = installDir,
                    Source = GameSource.Epic
                });
            }
            catch (JsonException) { }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        return games.OrderBy(g => g.Name).ToList();
    }
}
