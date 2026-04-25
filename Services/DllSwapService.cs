using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using GameProfileManager.Models;

namespace GameProfileManager.Services;

public class DllSwapService
{
    private readonly string _backupBaseDir;

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(60),
        DefaultRequestHeaders = { { "User-Agent", "GameProfileManager/1.0" } }
    };

    public DllSwapService(string backupBaseDir)
    {
        _backupBaseDir = backupBaseDir;
    }

    public async Task<SwapResult> SwapDllAsync(
        GameEntry game, DetectedDll target, DllRecord newVersion,
        IProgress<string>? progress = null)
    {
        var fullDir = Path.GetFullPath(game.InstallDir);
        var winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (!string.IsNullOrEmpty(winDir) && fullDir.StartsWith(winDir, StringComparison.OrdinalIgnoreCase))
            return SwapResult.Fail("BLOCKED: Cannot swap DLLs in a Windows system directory.");

        progress?.Report("Checking for anti-cheat...");
        var antiCheat = await Task.Run(() => AntiCheatDetector.Detect(game.InstallDir));
        if (antiCheat is not null)
            return SwapResult.Fail($"BLOCKED: {antiCheat} detected in {game.Name}. DLL swap aborted to protect your account.");

        progress?.Report("Backing up original DLL...");
        var backupResult = BackupOriginal(game, target);
        if (!backupResult.Success)
            return SwapResult.Fail($"Backup failed: {backupResult.Message}");

        progress?.Report($"Downloading v{newVersion.Version}...");
        byte[] zipBytes;
        try
        {
            zipBytes = await Http.GetByteArrayAsync(newVersion.DownloadUrl);
        }
        catch (Exception ex)
        {
            return SwapResult.Fail($"Download failed: {ex.Message}");
        }

        var zipHash = Convert.ToHexString(SHA256.HashData(zipBytes));
        var zipMd5 = Convert.ToHexString(MD5.HashData(zipBytes));
        if (!zipMd5.Equals(newVersion.ZipMd5Hash, StringComparison.OrdinalIgnoreCase))
            return SwapResult.Fail($"Download integrity check failed (MD5).\nExpected: {newVersion.ZipMd5Hash}\nGot: {zipMd5}");

        progress?.Report("Extracting and replacing DLL...");
        try
        {
            using var zipStream = new MemoryStream(zipBytes);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

            var expectedFilename = DllTypeMap.Filenames[target.Type];
            var entry = archive.Entries.FirstOrDefault(e =>
                e.FullName.Equals(expectedFilename, StringComparison.OrdinalIgnoreCase)
                && !e.FullName.Contains("..") && !Path.IsPathRooted(e.FullName));

            if (entry is null)
                return SwapResult.Fail($"Zip does not contain {expectedFilename} at root level");

            using var entryStream = entry.Open();
            using var dllBytes = new MemoryStream();
            await entryStream.CopyToAsync(dllBytes);
            var dllData = dllBytes.ToArray();

            var dllHash = Convert.ToHexString(SHA256.HashData(dllData));
            var dllMd5 = Convert.ToHexString(MD5.HashData(dllData));
            if (!dllMd5.Equals(newVersion.Md5Hash, StringComparison.OrdinalIgnoreCase))
                return SwapResult.Fail($"Extracted DLL hash mismatch (MD5).\nExpected: {newVersion.Md5Hash}\nGot: {dllMd5}");

            File.WriteAllBytes(target.FullPath, dllData);
        }
        catch (Exception ex)
        {
            return SwapResult.Fail($"Replace failed: {ex.Message}");
        }

        progress?.Report("Done.");
        return SwapResult.Ok($"Swapped {DllTypeMap.Filenames[target.Type]} to v{newVersion.Version}\nBackup: {backupResult.Message}");
    }

    public SwapResult RevertDll(GameEntry game, DetectedDll target)
    {
        var backupPath = GetBackupDllPath(game, target);
        if (backupPath is null || !File.Exists(backupPath))
            return SwapResult.Fail("No backup found for this DLL.");

        try
        {
            File.Copy(backupPath, target.FullPath, overwrite: true);
            return SwapResult.Ok($"Restored original {DllTypeMap.Filenames[target.Type]}");
        }
        catch (Exception ex)
        {
            return SwapResult.Fail($"Restore failed: {ex.Message}");
        }
    }

    public bool HasBackup(GameEntry game, DetectedDll target)
    {
        var path = GetBackupDllPath(game, target);
        return path is not null && File.Exists(path);
    }

    private SwapResult BackupOriginal(GameEntry game, DetectedDll target)
    {
        var backupDir = GetGameDllBackupDir(game);
        Directory.CreateDirectory(backupDir);

        var filename = DllTypeMap.Filenames[target.Type];
        var backupPath = Path.Combine(backupDir, filename);
        var hashPath = backupPath + ".md5";

        if (File.Exists(backupPath))
            return SwapResult.Ok(backupPath);

        try
        {
            File.Copy(target.FullPath, backupPath);
            var hash = Convert.ToHexString(MD5.HashData(File.ReadAllBytes(target.FullPath)));
            File.WriteAllText(hashPath, hash);
            return SwapResult.Ok(backupPath);
        }
        catch (Exception ex)
        {
            return SwapResult.Fail(ex.Message);
        }
    }

    private string? GetBackupDllPath(GameEntry game, DetectedDll target)
    {
        var backupDir = GetGameDllBackupDir(game);
        var filename = DllTypeMap.Filenames[target.Type];
        var path = Path.Combine(backupDir, filename);
        return File.Exists(path) ? path : null;
    }

    private string GetGameDllBackupDir(GameEntry game)
    {
        var safeName = string.Join("_", game.Name.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_backupBaseDir, safeName, "dlls");
    }
}

public record SwapResult(bool Success, string Message)
{
    public static SwapResult Ok(string message) => new(true, message);
    public static SwapResult Fail(string message) => new(false, message);
}
