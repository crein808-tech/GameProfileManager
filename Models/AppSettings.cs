using System.Text.Json.Serialization;

namespace GameProfileManager.Models;

public class AppSettings
{
    public List<ManualGameEntry> ManualGames { get; set; } = [];
    public string NpiExePath { get; set; } = "";
    public string DlssTweaksDll { get; set; } = "";
    public string DlssTweaksIni { get; set; } = "";
    public double WindowWidth { get; set; } = 1050;
    public double WindowHeight { get; set; } = 650;
    public double WindowLeft { get; set; } = double.NaN;
    public double WindowTop { get; set; } = double.NaN;
    public bool IsMaximized { get; set; }
}

public class ManualGameEntry
{
    public required string Name { get; set; }
    public required string InstallDir { get; set; }
}
