using System.IO;
using Microsoft.Win32;
using GameProfileManager.Models;

namespace GameProfileManager.Services;

public static class GogDetectionService
{
    private const string GogKey = @"SOFTWARE\WOW6432Node\GOG.com\Games";

    public static List<GameEntry> DetectGames()
    {
        var games = new List<GameEntry>();

        try
        {
            using var rootKey = Registry.LocalMachine.OpenSubKey(GogKey);
            if (rootKey is null)
                return [];

            foreach (var subKeyName in rootKey.GetSubKeyNames())
            {
                try
                {
                    using var gameKey = rootKey.OpenSubKey(subKeyName);
                    if (gameKey is null) continue;

                    var path = gameKey.GetValue("path") as string;
                    var name = gameKey.GetValue("gameName") as string;

                    if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(name))
                        continue;
                    if (!Directory.Exists(path))
                        continue;

                    games.Add(new GameEntry
                    {
                        Name = name,
                        InstallDir = path,
                        Source = GameSource.Gog
                    });
                }
                catch (Exception) { }
            }
        }
        catch (Exception) { }

        return games.OrderBy(g => g.Name).ToList();
    }
}
