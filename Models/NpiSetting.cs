namespace GameProfileManager.Models;

public class NpiSettingDef
{
    public required string Name { get; set; }
    public required string Category { get; set; }
    public required string Description { get; set; }
    public required uint SettingId { get; set; }
    public required NpiValueOption[] Options { get; set; }
    public required string DefaultHex { get; set; }
    public string SelectedHex { get; set; } = "";
    public bool IsString { get; set; }
}

public record NpiValueOption(string Label, string HexValue);
