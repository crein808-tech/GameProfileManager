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
}
