using System.IO;
using GameProfileManager.Models;

namespace GameProfileManager.Services;

public class BackupService
{
    private readonly string _backupBaseDir;

    public BackupService(string backupBaseDir)
    {
        _backupBaseDir = backupBaseDir;
    }

    public List<BackupEntry> GetAllBackups(GameEntry game)
    {
        var gameDir = GetGameBackupDir(game);
        if (!Directory.Exists(gameDir))
            return [];

        var entries = new List<BackupEntry>();

        var npiDir = Path.Combine(gameDir, "npi");
        if (Directory.Exists(npiDir))
        {
            foreach (var file in Directory.GetFiles(npiDir, "*.nip"))
            {
                var info = new FileInfo(file);
                entries.Add(new BackupEntry
                {
                    FilePath = file,
                    FileName = info.Name,
                    Category = BackupCategory.NpiProfile,
                    CreatedUtc = info.CreationTimeUtc,
                    FileSize = info.Length
                });
            }
        }

        var dllDir = Path.Combine(gameDir, "dlls");
        if (Directory.Exists(dllDir))
        {
            foreach (var file in Directory.GetFiles(dllDir).Where(f => !f.EndsWith(".md5")))
            {
                var info = new FileInfo(file);
                entries.Add(new BackupEntry
                {
                    FilePath = file,
                    FileName = info.Name,
                    Category = BackupCategory.Dll,
                    CreatedUtc = info.CreationTimeUtc,
                    FileSize = info.Length
                });
            }
        }

        return entries.OrderByDescending(e => e.CreatedUtc).ToList();
    }

    public bool DeleteBackup(BackupEntry entry)
    {
        try
        {
            if (File.Exists(entry.FilePath))
                File.Delete(entry.FilePath);

            var hashFile = entry.FilePath + ".md5";
            if (File.Exists(hashFile))
                File.Delete(hashFile);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public string GetBackupSummary(GameEntry game)
    {
        var all = GetAllBackups(game);
        if (all.Count == 0)
            return "No backups.";

        var npiCount = all.Count(e => e.Category == BackupCategory.NpiProfile);
        var dllCount = all.Count(e => e.Category == BackupCategory.Dll);
        var totalMb = all.Sum(e => e.FileSize) / (1024.0 * 1024.0);
        return $"{all.Count} backup(s): {npiCount} NPI profile(s), {dllCount} DLL(s) — {totalMb:F1} MB total";
    }

    private string GetGameBackupDir(GameEntry game)
    {
        var safeName = string.Join("_", game.Name.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_backupBaseDir, safeName);
    }
}
