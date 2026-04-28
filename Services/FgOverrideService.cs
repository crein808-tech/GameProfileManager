using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using GameProfileManager.Models;

namespace GameProfileManager.Services;

public class FgOverrideService(string cacheDir)
{
    private const string FgDllName = "nvngx_dlssg.dll";

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(60),
        DefaultRequestHeaders = { { "User-Agent", "GameProfileManager/1.0" } }
    };

    public string? GetCachedPath(DllRecord record)
    {
        var path = GetDllPath(record);
        return File.Exists(path) ? path : null;
    }

    public async Task<string> EnsureCachedAsync(DllRecord record, IProgress<string>? progress = null)
    {
        var path = GetDllPath(record);
        if (File.Exists(path))
            return path;

        progress?.Report($"Downloading FG v{record.Version}...");
        byte[] zipBytes;
        try
        {
            zipBytes = await Http.GetByteArrayAsync(record.DownloadUrl);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Download failed: {ex.Message}", ex);
        }

        var zipMd5 = Convert.ToHexString(MD5.HashData(zipBytes));
        if (!zipMd5.Equals(record.ZipMd5Hash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Download integrity check failed.\nExpected: {record.ZipMd5Hash}\nGot: {zipMd5}");

        progress?.Report("Extracting...");
        using var zipStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        var entry = archive.Entries.FirstOrDefault(e =>
            e.FullName.Equals(FgDllName, StringComparison.OrdinalIgnoreCase)
            && !e.FullName.Contains("..")
            && !Path.IsPathRooted(e.FullName));

        if (entry is null)
            throw new InvalidOperationException($"Zip does not contain {FgDllName} at root level.");

        using var entryStream = entry.Open();
        using var ms = new MemoryStream();
        await entryStream.CopyToAsync(ms);
        var dllData = ms.ToArray();

        var dllMd5 = Convert.ToHexString(MD5.HashData(dllData));
        if (!dllMd5.Equals(record.Md5Hash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Extracted DLL hash mismatch.\nExpected: {record.Md5Hash}\nGot: {dllMd5}");

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, dllData);
        progress?.Report("Done.");
        return path;
    }

    private string GetDllPath(DllRecord record) =>
        Path.Combine(cacheDir, record.Version, FgDllName);
}
