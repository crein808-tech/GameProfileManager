using System.Diagnostics;
using System.IO;
using GameProfileManager.Models;

namespace GameProfileManager.Services;

public class DlssTweaksService
{
    private readonly string _proxyDllSource;
    private readonly string _iniTemplateSource;

    private static readonly string[] ProxyDllNames =
    [
        "dxgi.dll",
        "XInput1_3.dll",
        "XInput1_4.dll",
        "XInput9_1_0.dll",
        "XAPOFX1_5.dll",
        "X3DAudio1_7.dll",
        "winmm.dll"
    ];

    public DlssTweaksService(string proxyDllSource, string iniTemplateSource)
    {
        _proxyDllSource = proxyDllSource;
        _iniTemplateSource = iniTemplateSource;
    }

    public bool IsAvailable => File.Exists(_proxyDllSource) && File.Exists(_iniTemplateSource);

    public TweaksStatus GetStatus(GameEntry game)
    {
        var deployedDll = FindDeployedDll(game.InstallDir);
        var iniPath = Path.Combine(game.InstallDir, "dlsstweaks.ini");
        var hasIni = File.Exists(iniPath);

        if (deployedDll is not null && hasIni)
            return new TweaksStatus(true, deployedDll, iniPath, Path.GetFileName(deployedDll));
        if (deployedDll is not null || hasIni)
            return new TweaksStatus(false, deployedDll, hasIni ? iniPath : null,
                "Partial install — missing " + (deployedDll is null ? "proxy DLL" : "ini"));

        return new TweaksStatus(false, null, null, "Not deployed");
    }

    public string Deploy(GameEntry game)
    {
        var targetDir = game.InstallDir;
        var dllTarget = Path.Combine(targetDir, "dxgi.dll");
        var iniTarget = Path.Combine(targetDir, "dlsstweaks.ini");

        if (File.Exists(dllTarget))
            return $"dxgi.dll already exists in {targetDir}. Remove it first or choose a different proxy name.";

        try
        {
            File.Copy(_proxyDllSource, dllTarget);
            File.Copy(_iniTemplateSource, iniTarget, overwrite: true);
            return $"Deployed to {targetDir}\n  dxgi.dll + dlsstweaks.ini";
        }
        catch (Exception ex)
        {
            return $"Deploy failed: {ex.Message}";
        }
    }

    public string Remove(GameEntry game)
    {
        var removed = new List<string>();
        var targetDir = game.InstallDir;

        foreach (var name in ProxyDllNames)
        {
            var path = Path.Combine(targetDir, name);
            if (!File.Exists(path)) continue;
            if (!IsDlssTweaksProxy(path)) continue;
            File.Delete(path);
            removed.Add(name);
        }

        var iniPath = Path.Combine(targetDir, "dlsstweaks.ini");
        if (File.Exists(iniPath))
        {
            File.Delete(iniPath);
            removed.Add("dlsstweaks.ini");
        }

        return removed.Count > 0
            ? $"Removed: {string.Join(", ", removed)}"
            : "No DLSSTweaks files found to remove.";
    }

    public void OpenConfig(GameEntry game)
    {
        var iniPath = Path.Combine(game.InstallDir, "dlsstweaks.ini");
        if (!File.Exists(iniPath))
            return;

        Process.Start(new ProcessStartInfo(iniPath) { UseShellExecute = true });
    }

    private static string? FindDeployedDll(string gameDir)
    {
        foreach (var name in ProxyDllNames)
        {
            var path = Path.Combine(gameDir, name);
            if (File.Exists(path) && IsDlssTweaksProxy(path))
                return path;
        }
        return null;
    }

    private static bool IsDlssTweaksProxy(string dllPath)
    {
        try
        {
            var vi = FileVersionInfo.GetVersionInfo(dllPath);
            return vi.ProductName?.Contains("DLSSTweaks", StringComparison.OrdinalIgnoreCase) == true
                || vi.FileDescription?.Contains("DLSSTweaks", StringComparison.OrdinalIgnoreCase) == true;
        }
        catch
        {
            return false;
        }
    }
}

public record TweaksStatus(bool IsDeployed, string? DllPath, string? IniPath, string Summary);
