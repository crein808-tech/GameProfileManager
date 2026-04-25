using System.Diagnostics;
using System.IO;
using GameProfileManager.Models;

namespace GameProfileManager.Services;

public static class DllDetectionService
{
    private const int MaxDepth = 5;

    private static readonly Dictionary<string, DllType> FilenameToDllType =
        DllTypeMap.Filenames.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.OrdinalIgnoreCase);

    public static List<DetectedDll> ScanGameDirectory(string gameDir)
    {
        var results = new List<DetectedDll>();
        ScanRecursive(gameDir, gameDir, 0, results);
        return results;
    }

    private static void ScanRecursive(string rootDir, string currentDir, int depth, List<DetectedDll> results)
    {
        if (depth > MaxDepth)
            return;

        try
        {
            foreach (var file in Directory.GetFiles(currentDir))
            {
                var fileName = Path.GetFileName(file);
                if (FilenameToDllType.TryGetValue(fileName, out var dllType))
                {
                    var info = new FileInfo(file);
                    var detected = new DetectedDll
                    {
                        Type = dllType,
                        FullPath = file,
                        RelativePath = Path.GetRelativePath(rootDir, file),
                        FileSize = info.Length,
                        Version = GetFileVersion(file)
                    };
                    results.Add(detected);
                }
            }

            foreach (var subDir in Directory.GetDirectories(currentDir))
            {
                var dirName = Path.GetFileName(subDir);
                if (dirName.StartsWith('.') || dirName.Equals("__pycache__", StringComparison.OrdinalIgnoreCase))
                    continue;
                ScanRecursive(rootDir, subDir, depth + 1, results);
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (DirectoryNotFoundException) { }
    }

    private static string GetFileVersion(string filePath)
    {
        try
        {
            var vi = FileVersionInfo.GetVersionInfo(filePath);
            return !string.IsNullOrWhiteSpace(vi.FileVersion) ? vi.FileVersion : "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }
}
