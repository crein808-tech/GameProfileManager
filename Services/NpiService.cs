using System.Diagnostics;
using System.IO;

namespace GameProfileManager.Services;

public class NpiService
{
    private readonly string _npiExePath;

    public NpiService(string npiExePath)
    {
        _npiExePath = npiExePath;
    }

    public bool IsAvailable => File.Exists(_npiExePath);

    public string ProfilesDir
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GameProfileManager", "profiles");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public void LaunchNpi()
    {
        if (!IsAvailable) return;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _npiExePath,
                WorkingDirectory = Path.GetDirectoryName(_npiExePath) ?? "",
                UseShellExecute = true
            };
            Process.Start(psi)?.Dispose();
        }
        catch { }
    }

    public async Task<(bool Success, string Message)> SilentImportAsync(string nipPath)
    {
        if (!IsAvailable)
            return (false, "NPI not found.");
        if (!File.Exists(nipPath))
            return (false, $"Profile not found: {Path.GetFileName(nipPath)}");

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _npiExePath,
                Arguments = $"-silentImport \"{nipPath}\"",
                WorkingDirectory = Path.GetDirectoryName(_npiExePath) ?? "",
                UseShellExecute = true
            };
            using var proc = Process.Start(psi);
            if (proc is not null)
                await proc.WaitForExitAsync();
            return (true, $"Profile applied: {Path.GetFileName(nipPath)}");
        }
        catch (Exception ex)
        {
            return (false, $"Silent import failed: {ex.Message}");
        }
    }
}
