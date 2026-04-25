namespace GameProfileManager.Models;

public class BackupEntry
{
    public required string FilePath { get; set; }
    public required string FileName { get; set; }
    public required BackupCategory Category { get; set; }
    public required DateTime CreatedUtc { get; set; }
    public long FileSize { get; set; }

    public string Display
    {
        get
        {
            var sizeMb = FileSize / (1024.0 * 1024.0);
            var age = DateTime.UtcNow - CreatedUtc;
            var ageStr = age.TotalDays >= 1 ? $"{age.TotalDays:F0}d ago"
                : age.TotalHours >= 1 ? $"{age.TotalHours:F0}h ago"
                : $"{age.TotalMinutes:F0}m ago";
            return $"[{Category}]  {FileName}  ({sizeMb:F1} MB)  —  {ageStr}";
        }
    }

    public override string ToString() => Display;
}

public enum BackupCategory
{
    NpiProfile,
    Dll
}
