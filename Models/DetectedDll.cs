namespace GameProfileManager.Models;

public class DetectedDll
{
    public required DllType Type { get; set; }
    public required string FullPath { get; set; }
    public required string RelativePath { get; set; }
    public long FileSize { get; set; }
    public string Version { get; set; } = "Unknown";
}
