namespace GameProfileManager.Models;

public class TweaksSetting
{
    public required string Section { get; set; }
    public required string Key { get; set; }
    public required string Label { get; set; }
    public required string Description { get; set; }
    public required TweaksSettingType Type { get; set; }
    public required string DefaultValue { get; set; }
    public string[] Options { get; set; } = [];
    public string Value { get; set; } = string.Empty;
}

public enum TweaksSettingType
{
    Bool,
    Dropdown,
    Numeric,
    Text
}
