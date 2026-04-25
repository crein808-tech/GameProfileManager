using System.IO;

namespace GameProfileManager.Services;

public static class AntiCheatDetector
{
    private static readonly string[] FileMarkers =
    [
        "EasyAntiCheat_EOS.sys",
        "easyanticheat_x64.dll",
        "easyanticheat_x86.dll",
        "BEService.exe",
        "BEClient_x64.dll",
        "vgk.sys",
        "vanguard.exe",
        "vgtray.exe"
    ];

    private static readonly string[] DirMarkers =
    [
        "EasyAntiCheat",
        "BattlEye"
    ];

    public static string? Detect(string gameDir)
    {
        return ScanRecursive(gameDir, 0);
    }

    private static string? ScanRecursive(string dir, int depth)
    {
        if (depth > 4)
            return null;

        try
        {
            foreach (var file in Directory.GetFiles(dir))
            {
                var name = Path.GetFileName(file);
                foreach (var marker in FileMarkers)
                {
                    if (name.Equals(marker, StringComparison.OrdinalIgnoreCase))
                        return Classify(marker);
                }
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }

        try
        {
            foreach (var subDir in Directory.GetDirectories(dir))
            {
                var name = Path.GetFileName(subDir);
                foreach (var marker in DirMarkers)
                {
                    if (name.Equals(marker, StringComparison.OrdinalIgnoreCase))
                        return Classify(marker);
                }

                var found = ScanRecursive(subDir, depth + 1);
                if (found is not null)
                    return found;
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }

        return null;
    }

    private static string Classify(string marker)
    {
        if (marker.Contains("EasyAntiCheat", StringComparison.OrdinalIgnoreCase) ||
            marker.Contains("easyanticheat", StringComparison.OrdinalIgnoreCase))
            return "Easy Anti-Cheat (EAC)";

        if (marker.Contains("BattlEye", StringComparison.OrdinalIgnoreCase) ||
            marker.Contains("BEService", StringComparison.OrdinalIgnoreCase) ||
            marker.Contains("BEClient", StringComparison.OrdinalIgnoreCase))
            return "BattlEye";

        if (marker.Contains("vgk", StringComparison.OrdinalIgnoreCase) ||
            marker.Contains("vanguard", StringComparison.OrdinalIgnoreCase) ||
            marker.Contains("vgtray", StringComparison.OrdinalIgnoreCase))
            return "Vanguard";

        return marker;
    }
}
