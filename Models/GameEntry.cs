namespace GameProfileManager.Models;

public class GameEntry
{
    public required string Name { get; set; }
    public required string InstallDir { get; set; }
    public string AppId { get; set; } = string.Empty;
    public GameSource Source { get; set; }

    public override string ToString() => Name;
}

public enum GameSource
{
    Steam,
    Manual
}
