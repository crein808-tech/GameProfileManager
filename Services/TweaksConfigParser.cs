using System.IO;
using System.Text.RegularExpressions;
using GameProfileManager.Models;

namespace GameProfileManager.Services;

public static partial class TweaksConfigParser
{
    public static List<TweaksSetting> GetSettingsTemplate()
    {
        return
        [
            // [DLSS] section
            new()
            {
                Section = "DLSS", Key = "ForceDLAA", Label = "Force DLAA",
                Description = "Forces all DLSS modes to render at full resolution with DLAA anti-aliasing. Enable any DLSS quality in-game — all will render at full res instead.",
                Type = TweaksSettingType.Bool, DefaultValue = "false"
            },
            new()
            {
                Section = "DLSS", Key = "OverrideAutoExposure", Label = "Auto-Exposure Override",
                Description = "Force DLSS auto-exposure on or off. Enabling can reduce ghosting in some games.",
                Type = TweaksSettingType.Dropdown, DefaultValue = "0",
                Options = ["0 (Default)", "1 (Force Enable)", "-1 (Force Disable)"]
            },
            new()
            {
                Section = "DLSS", Key = "OverrideAlphaUpscaling", Label = "Alpha Upscaling (DLSS 3.6+)",
                Description = "Force RGBA alpha-channel upscaling. May improve transparency effects but costs 15-25% performance.",
                Type = TweaksSettingType.Dropdown, DefaultValue = "0",
                Options = ["0 (Default)", "1 (Force Enable)", "-1 (Force Disable)"]
            },
            new()
            {
                Section = "DLSS", Key = "OverrideSharpening", Label = "Sharpening Override",
                Description = "Override DLSS sharpening (-1.0 to 1.0). Only affects pre-2.5.1 DLSS. Set to 'Default' to leave unchanged, 'disable' to force off.",
                Type = TweaksSettingType.Dropdown, DefaultValue = "Default",
                Options = ["Default", "disable", "-0.5", "-0.25", "0", "0.25", "0.5", "0.64", "0.75", "1.0"]
            },
            new()
            {
                Section = "DLSS", Key = "OverrideDlssHud", Label = "Debug HUD Overlay",
                Description = "Show DLSS debug overlay on screen. Useful for verifying DLAA is active (rendered and upscaled resolutions should match).",
                Type = TweaksSettingType.Dropdown, DefaultValue = "0",
                Options = ["0 (Default)", "1 (Force Enable)", "2 (Alt Method)", "-1 (Force Disable)"]
            },
            new()
            {
                Section = "DLSS", Key = "OverrideHDR", Label = "HDR Override",
                Description = "Force DLSS to run in HDR linear space. Useful for HDR-modded games where DLSS runs in SDR mode.",
                Type = TweaksSettingType.Dropdown, DefaultValue = "0",
                Options = ["0 (Default)", "1 (Force Enable)", "-1 (Force Disable)"]
            },
            new()
            {
                Section = "DLSS", Key = "DisableDevWatermark", Label = "Disable Dev Watermark",
                Description = "Remove on-screen watermark from DLSS Frame Gen dev builds. Only useful with dev DLLs.",
                Type = TweaksSettingType.Bool, DefaultValue = "false"
            },
            new()
            {
                Section = "DLSS", Key = "VerboseLogging", Label = "Verbose Logging",
                Description = "Write extra debug info to dlsstweaks.log. Useful for troubleshooting.",
                Type = TweaksSettingType.Bool, DefaultValue = "false"
            },

            // [DLSSQualityLevels] section
            new()
            {
                Section = "DLSSQualityLevels", Key = "Enable", Label = "Custom Quality Levels",
                Description = "Enable custom resolution ratios for each DLSS quality level. Overrides ForceDLAA when enabled.",
                Type = TweaksSettingType.Bool, DefaultValue = "false"
            },
            new()
            {
                Section = "DLSSQualityLevels", Key = "UltraPerformance", Label = "Ultra Performance Ratio",
                Description = "Resolution multiplier for Ultra Performance mode (0.0 - 1.0). Default 0.33.",
                Type = TweaksSettingType.Dropdown, DefaultValue = "0.33333334",
                Options = ["0.33333334", "0.4", "0.5", "0.58", "0.66666667", "0.77", "1.0"]
            },
            new()
            {
                Section = "DLSSQualityLevels", Key = "Performance", Label = "Performance Ratio",
                Description = "Resolution multiplier for Performance mode (0.0 - 1.0). Default 0.5.",
                Type = TweaksSettingType.Dropdown, DefaultValue = "0.5",
                Options = ["0.33333334", "0.4", "0.5", "0.58", "0.66666667", "0.77", "1.0"]
            },
            new()
            {
                Section = "DLSSQualityLevels", Key = "Balanced", Label = "Balanced Ratio",
                Description = "Resolution multiplier for Balanced mode (0.0 - 1.0). Default 0.58.",
                Type = TweaksSettingType.Dropdown, DefaultValue = "0.58",
                Options = ["0.33333334", "0.4", "0.5", "0.58", "0.66666667", "0.77", "1.0"]
            },
            new()
            {
                Section = "DLSSQualityLevels", Key = "Quality", Label = "Quality Ratio",
                Description = "Resolution multiplier for Quality mode (0.0 - 1.0). Default 0.67.",
                Type = TweaksSettingType.Dropdown, DefaultValue = "0.66666667",
                Options = ["0.33333334", "0.4", "0.5", "0.58", "0.66666667", "0.77", "1.0"]
            },

            // [DLSSPresets] section
            new()
            {
                Section = "DLSSPresets", Key = "DLAA", Label = "DLAA Preset",
                Description = "DLSS model preset for DLAA. J/K are transformer models, L/M are 2nd-gen transformers.",
                Type = TweaksSettingType.Dropdown, DefaultValue = "Default",
                Options = ["Default", "A", "B", "C", "D", "E", "F", "J", "K", "L", "M"]
            },
            new()
            {
                Section = "DLSSPresets", Key = "Quality", Label = "Quality Preset",
                Description = "DLSS model preset for Quality mode.",
                Type = TweaksSettingType.Dropdown, DefaultValue = "Default",
                Options = ["Default", "A", "B", "C", "D", "E", "F", "J", "K", "L", "M"]
            },
            new()
            {
                Section = "DLSSPresets", Key = "Balanced", Label = "Balanced Preset",
                Description = "DLSS model preset for Balanced mode.",
                Type = TweaksSettingType.Dropdown, DefaultValue = "Default",
                Options = ["Default", "A", "B", "C", "D", "E", "F", "J", "K", "L", "M"]
            },
            new()
            {
                Section = "DLSSPresets", Key = "Performance", Label = "Performance Preset",
                Description = "DLSS model preset for Performance mode.",
                Type = TweaksSettingType.Dropdown, DefaultValue = "Default",
                Options = ["Default", "A", "B", "C", "D", "E", "F", "J", "K", "L", "M"]
            },
            new()
            {
                Section = "DLSSPresets", Key = "UltraPerformance", Label = "Ultra Performance Preset",
                Description = "DLSS model preset for Ultra Performance mode.",
                Type = TweaksSettingType.Dropdown, DefaultValue = "Default",
                Options = ["Default", "A", "B", "C", "D", "E", "F", "J", "K", "L", "M"]
            },
            new()
            {
                Section = "DLSSPresets", Key = "UltraQuality", Label = "Ultra Quality Preset",
                Description = "DLSS model preset for Ultra Quality mode.",
                Type = TweaksSettingType.Dropdown, DefaultValue = "Default",
                Options = ["Default", "A", "B", "C", "D", "E", "F", "J", "K", "L", "M"]
            },

            // [Compatibility] section
            new()
            {
                Section = "Compatibility", Key = "ResolutionOffset", Label = "Resolution Offset",
                Description = "Offset resolution axes when DLAA is active. RE Engine and Crysis 3 Remastered need -1.",
                Type = TweaksSettingType.Dropdown, DefaultValue = "0",
                Options = ["0", "-1", "-2"]
            },
            new()
            {
                Section = "Compatibility", Key = "DynamicResolutionOverride", Label = "Dynamic Resolution Override",
                Description = "Override dynamic resolution range to match custom DLSS scales. Recommended to leave enabled.",
                Type = TweaksSettingType.Bool, DefaultValue = "true"
            },
            new()
            {
                Section = "Compatibility", Key = "OverrideAppId", Label = "Override App ID",
                Description = "Legacy workaround for preset overrides. Only try if DLSSPresets aren't being applied.",
                Type = TweaksSettingType.Bool, DefaultValue = "false"
            },
            new()
            {
                Section = "Compatibility", Key = "DisableIniMonitoring", Label = "Disable INI Monitoring",
                Description = "Stop DLSSTweaks from watching for INI changes during gameplay. Try if game fails to launch.",
                Type = TweaksSettingType.Bool, DefaultValue = "false"
            },
        ];
    }

    public static void LoadFromIni(string iniPath, List<TweaksSetting> settings)
    {
        if (!File.Exists(iniPath))
            return;

        var lines = File.ReadAllLines(iniPath);
        var currentSection = "";

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                currentSection = trimmed[1..^1];
                continue;
            }

            if (trimmed.StartsWith(';') || !trimmed.Contains('='))
                continue;

            var eqIdx = trimmed.IndexOf('=');
            var key = trimmed[..eqIdx].Trim();
            var value = trimmed[(eqIdx + 1)..].Trim();

            var setting = settings.FirstOrDefault(s =>
                s.Section.Equals(currentSection, StringComparison.OrdinalIgnoreCase) &&
                s.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

            if (setting is not null)
                setting.Value = value;
        }

        foreach (var s in settings.Where(s => string.IsNullOrEmpty(s.Value)))
            s.Value = s.DefaultValue;
    }

    public static void SaveToIni(string iniPath, List<TweaksSetting> settings)
    {
        // Read any keys from the existing file that aren't in our template so
        // keys added by a newer DLSSTweaks version are not silently destroyed.
        var unknownKeys = new Dictionary<string, List<(string Key, string Value)>>(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(iniPath))
        {
            var currentSection = "";
            foreach (var line in File.ReadAllLines(iniPath))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
                {
                    currentSection = trimmed[1..^1];
                    continue;
                }
                if (trimmed.StartsWith(';') || !trimmed.Contains('='))
                    continue;

                var eqIdx = trimmed.IndexOf('=');
                var key = trimmed[..eqIdx].Trim();
                var value = trimmed[(eqIdx + 1)..].Trim();

                var isKnown = settings.Any(s =>
                    s.Section.Equals(currentSection, StringComparison.OrdinalIgnoreCase) &&
                    s.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

                if (!isKnown)
                {
                    if (!unknownKeys.ContainsKey(currentSection))
                        unknownKeys[currentSection] = [];
                    unknownKeys[currentSection].Add((key, value));
                }
            }
        }

        using var writer = new StreamWriter(iniPath);
        writer.WriteLine("; DLSSTweaks config - generated by Game Profile Manager");
        writer.WriteLine();

        var sections = settings.Select(s => s.Section).Distinct().ToList();
        foreach (var section in sections)
        {
            writer.WriteLine($"[{section}]");
            foreach (var setting in settings.Where(s => s.Section == section))
            {
                var value = GetRawValue(setting);
                writer.WriteLine($"{setting.Key} = {value}");
            }
            if (unknownKeys.TryGetValue(section, out var extra))
                foreach (var (k, v) in extra)
                    writer.WriteLine($"{k} = {v}");
            writer.WriteLine();
        }

        // Append entirely new sections introduced by a newer DLSSTweaks version.
        foreach (var (section, entries) in unknownKeys)
        {
            if (sections.Contains(section, StringComparer.OrdinalIgnoreCase))
                continue;
            writer.WriteLine($"[{section}]");
            foreach (var (k, v) in entries)
                writer.WriteLine($"{k} = {v}");
            writer.WriteLine();
        }
    }

    private static string GetRawValue(TweaksSetting setting)
    {
        var v = setting.Value;
        if (setting.Type == TweaksSettingType.Dropdown)
        {
            var spaceIdx = v.IndexOf(' ');
            if (spaceIdx > 0)
                return v[..spaceIdx];
        }
        return v;
    }
}
