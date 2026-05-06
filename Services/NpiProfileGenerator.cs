using System.IO;
using System.Text;
using System.Xml;
using GameProfileManager.Models;

namespace GameProfileManager.Services;

public record NpiPreset(string Name, string Description, (string SettingName, string HexValue)[] Values);

public static class NpiProfileGenerator
{
    public static List<NpiSettingDef> GetSettingsTemplate()
    {
        return
        [
            // --- Sync & Frame Pacing ---
            new()
            {
                Name = "Vertical Sync", Category = "Sync & Frame Pacing",
                Description = "Control how the GPU synchronizes frame output with your monitor's refresh rate.",
                SettingId = 0x00A879CF, DefaultHex = "0x60925292",
                Options =
                [
                    new("Use Application Setting", "0x60925292"),
                    new("Force Off", "0x08416747"),
                    new("Force On", "0x47814940"),
                    new("Adaptive (Half Refresh)", "0x32610244"),
                    new("Fast Sync", "0x18888888"),
                ]
            },
            new()
            {
                Name = "Adaptive VSync (Tear Control)", Category = "Sync & Frame Pacing",
                Description = "When enabled, VSync turns off when FPS drops below refresh rate to reduce stutter.",
                SettingId = 0x005A375C, DefaultHex = "0x96861077",
                Options =
                [
                    new("Disabled", "0x96861077"),
                    new("Enabled", "0x99941284"),
                ]
            },
            new()
            {
                Name = "Triple Buffering", Category = "Sync & Frame Pacing",
                Description = "Adds a third frame buffer to reduce VSync-induced stutter. Only affects OpenGL games.",
                SettingId = 0x20FDD1F9, DefaultHex = "0x00000000",
                Options =
                [
                    new("Disabled", "0x00000000"),
                    new("Enabled", "0x00000001"),
                ]
            },
            new()
            {
                Name = "Frame Rate Limiter (FPS)", Category = "Sync & Frame Pacing",
                Description = "Cap the maximum frame rate. Set to 0 for unlimited.",
                SettingId = 0x10834FEE, DefaultHex = "0x00000000",
                Options =
                [
                    new("Off", "0x00000000"),
                    new("30 FPS", "0x0000001E"),
                    new("60 FPS", "0x0000003C"),
                    new("90 FPS", "0x0000005A"),
                    new("120 FPS", "0x00000078"),
                    new("144 FPS", "0x00000090"),
                    new("165 FPS", "0x000000A5"),
                    new("240 FPS", "0x000000F0"),
                ]
            },
            new()
            {
                Name = "Max Pre-Rendered Frames", Category = "Sync & Frame Pacing",
                Description = "Number of frames the CPU can prepare ahead of the GPU. Lower = less input lag, higher = smoother. Set to 1 for 'Ultra Low Latency'.",
                SettingId = 0x007BA09E, DefaultHex = "0x00000000",
                Options =
                [
                    new("Application Controlled", "0x00000000"),
                    new("1 (Ultra Low Latency)", "0x00000001"),
                    new("2", "0x00000002"),
                    new("3", "0x00000003"),
                    new("4", "0x00000004"),
                ]
            },

            // --- Texture Filtering ---
            new()
            {
                Name = "Texture Filtering - Quality", Category = "Texture Filtering",
                Description = "Global texture filtering quality. 'High Quality' is sharpest, 'High Performance' is fastest.",
                SettingId = 0x00CE2691, DefaultHex = "0x00000000",
                Options =
                [
                    new("High Quality", "0xFFFFFFF6"),
                    new("Quality (Default)", "0x00000000"),
                    new("Performance", "0x0000000A"),
                    new("High Performance", "0x00000014"),
                ]
            },
            new()
            {
                Name = "Anisotropic Filtering Mode", Category = "Texture Filtering",
                Description = "(legacy — for drivers < 526.48) How anisotropic filtering is applied. Use 'AF — Combined' for drivers ≥ 526.48.",
                SettingId = 0x10D2BB16, DefaultHex = "0x00000000",
                Options =
                [
                    new("Application Controlled", "0x00000000"),
                    new("User (Override)", "0x00000001"),
                ]
            },
            new()
            {
                Name = "Anisotropic Filtering Level", Category = "Texture Filtering",
                Description = "The AF level to force when mode is set to 'User'. Higher = sharper textures at angles.",
                SettingId = 0x101E61A9, DefaultHex = "0x00000001",
                Options =
                [
                    new("Off", "0x00000001"),
                    new("2x", "0x00000002"),
                    new("4x", "0x00000004"),
                    new("8x", "0x00000008"),
                    new("16x", "0x00000010"),
                ]
            },
            new()
            {
                Name = "Negative LOD Bias", Category = "Texture Filtering",
                Description = "Allow or clamp negative LOD bias. Allowing makes textures sharper but can cause shimmer.",
                SettingId = 0x0019BB68, DefaultHex = "0x00000000",
                Options =
                [
                    new("Allow", "0x00000000"),
                    new("Clamp", "0x00000001"),
                ]
            },
            new()
            {
                Name = "Trilinear Optimization", Category = "Texture Filtering",
                Description = "Optimizes trilinear filtering for performance with minimal visual impact.",
                SettingId = 0x002ECAF2, DefaultHex = "0x00000000",
                Options =
                [
                    new("Off", "0x00000000"),
                    new("On", "0x00000001"),
                ]
            },

            // --- Antialiasing ---
            new()
            {
                Name = "Antialiasing - Mode", Category = "Antialiasing",
                Description = "(legacy — for drivers < 526.48) Override = replace game's AA; Enhance = add to game's AA; App = let game decide. Use 'AA — Combined' for drivers ≥ 526.48.",
                SettingId = 0x107EFC5B, DefaultHex = "0x00000000",
                Options =
                [
                    new("Application Controlled", "0x00000000"),
                    new("Override", "0x00000001"),
                    new("Enhance", "0x00000002"),
                ]
            },
            new()
            {
                Name = "FXAA", Category = "Antialiasing",
                Description = "Fast approximate anti-aliasing. Low cost, slight blur. Applied on top of other AA.",
                SettingId = 0x1074C972, DefaultHex = "0x00000000",
                Options =
                [
                    new("Off", "0x00000000"),
                    new("On", "0x00000001"),
                ]
            },
            new()
            {
                Name = "MFAA (Multi-Frame AA)", Category = "Antialiasing",
                Description = "Alternates AA sample patterns between frames. Effectively doubles MSAA quality for free. Requires MSAA.",
                SettingId = 0x0098C1AC, DefaultHex = "0x00000000",
                Options =
                [
                    new("Off", "0x00000000"),
                    new("On", "0x00000001"),
                ]
            },

            // --- Performance ---
            new()
            {
                Name = "Power Management Mode", Category = "Performance",
                Description = "Controls GPU clock speed behavior. 'Prefer Max Performance' keeps clocks high.",
                SettingId = 0x1057EB71, DefaultHex = "0x00000000",
                Options =
                [
                    new("Adaptive (Default)", "0x00000000"),
                    new("Prefer Maximum Performance", "0x00000001"),
                    new("Driver Controlled", "0x00000002"),
                    new("Prefer Consistent Performance", "0x00000003"),
                    new("Optimal Power", "0x00000005"),
                ]
            },
            new()
            {
                Name = "Threaded Optimization", Category = "Performance",
                Description = "Allows the driver to use multiple CPU threads. Usually best left on Auto.",
                SettingId = 0x20C1221E, DefaultHex = "0x00000000",
                Options =
                [
                    new("Auto", "0x00000000"),
                    new("On", "0x00000001"),
                    new("Off", "0x00000002"),
                ]
            },
            new()
            {
                Name = "Shader Cache", Category = "Performance",
                Description = "Cache compiled shaders on disk to reduce stutter on subsequent launches.",
                SettingId = 0x00198FFF, DefaultHex = "0x00000001",
                Options =
                [
                    new("Off", "0x00000000"),
                    new("On (Default)", "0x00000001"),
                ]
            },

            // --- DLSS Overrides ---
            // ⚠ NEEDS NPI v3.0.1.12 VERIFICATION: set Preset K via NPI GUI, export, confirm value = 0x0000000B
            new()
            {
                Name = "DLSS Super Resolution Override", Category = "DLSS Overrides",
                Description = "Forces a DLSS preset per-game at the driver level. Sets the preset at the driver level. Also available in DLSSTweaks per-game .ini if you need a more robust override on titles where the driver setting is ignored.",
                SettingId = 0x10C7D684, DefaultHex = "0x00000000",
                Options =
                [
                    new("Default", "0x00000000"),
                    new("Preset A", "0x00000001"),
                    new("Preset B", "0x00000002"),
                    new("Preset C", "0x00000003"),
                    new("Preset D", "0x00000004"),
                    new("Preset E", "0x00000005"),
                    new("Preset F", "0x00000006"),
                    new("Preset J (transformer)", "0x0000000A"),
                    new("Preset K (transformer)", "0x0000000B"),
                    new("Preset L (2nd-gen transformer)", "0x0000000C"),
                    new("Preset M (2nd-gen transformer)", "0x0000000D"),
                ]
            },
            new()
            {
                Name = "DLSS Frame Generation Override", Category = "DLSS Overrides",
                Description = "Forces a DLSS Frame Generation preset at the driver level. Sets the preset at the driver level. Also available in DLSSTweaks per-game .ini if you need a more robust override on titles where the driver setting is ignored.",
                SettingId = 0x10C7D57E, DefaultHex = "0x00000000",
                Options =
                [
                    new("Default", "0x00000000"),
                    new("Preset A", "0x00000001"),
                    new("Preset B", "0x00000002"),
                    new("Preset C", "0x00000003"),
                    new("Preset D", "0x00000004"),
                    new("Preset E", "0x00000005"),
                    new("Preset F", "0x00000006"),
                    new("Preset J (transformer)", "0x0000000A"),
                    new("Preset K (transformer)", "0x0000000B"),
                    new("Preset L (2nd-gen transformer)", "0x0000000C"),
                    new("Preset M (2nd-gen transformer)", "0x0000000D"),
                ]
            },
            new()
            {
                Name = "DLSS Ray Reconstruction Override", Category = "DLSS Overrides",
                Description = "Forces a DLSS Ray Reconstruction preset at the driver level. Sets the preset at the driver level. Also available in DLSSTweaks per-game .ini if you need a more robust override on titles where the driver setting is ignored.",
                SettingId = 0x10C7D86C, DefaultHex = "0x00000000",
                Options =
                [
                    new("Default", "0x00000000"),
                    new("Preset A", "0x00000001"),
                    new("Preset B", "0x00000002"),
                    new("Preset C", "0x00000003"),
                    new("Preset D", "0x00000004"),
                    new("Preset E", "0x00000005"),
                    new("Preset F", "0x00000006"),
                    new("Preset J (transformer)", "0x0000000A"),
                    new("Preset K (transformer)", "0x0000000B"),
                    new("Preset L (2nd-gen transformer)", "0x0000000C"),
                    new("Preset M (2nd-gen transformer)", "0x0000000D"),
                ]
            },
            new()
            {
                Name = "DLSS-FG Private Flags", Category = "DLSS Overrides",
                Description = "Frame Generation behavior flags. 'None' is the safe default.",
                SettingId = 0x10E41DF6, DefaultHex = "0x00000000",
                Options =
                [
                    new("None (Default)", "0x00000000"),
                    new("Disable Scene Change Detection", "0x00000001"),
                    new("Use Full-Screen Threshold Override", "0x00000002"),
                    new("Disable SCD + FS Threshold Override", "0x00000003"),
                ]
            },

            // --- Modern Upscaling & HDR ---
            new()
            {
                Name = "Enable NIS 2.0", Category = "Modern Upscaling & HDR",
                Description = "Enable NVIDIA Image Scaling 2.0 — a spatial upscaler + sharpening filter. Works in games without DLSS support.",
                SettingId = 0x00ABAC21, DefaultHex = "0x00000000",
                Options =
                [
                    new("Off (Default)", "0x00000000"),
                    new("On", "0x00000001"),
                ]
            },
            new()
            {
                Name = "NIS 2.0 Sharpening", Category = "Modern Upscaling & HDR",
                Description = "Sharpening strength for NIS 2.0 (0–100). Only active when NIS 2.0 is enabled.",
                SettingId = 0x00ABAB21, DefaultHex = "0x00000000",
                Options =
                [
                    new("0 (Off)", "0x00000000"),
                    new("25", "0x00000019"),
                    new("50", "0x00000032"),
                    new("64 (Default NIS)", "0x00000040"),
                    new("75", "0x0000004B"),
                    new("100 (Max)", "0x00000064"),
                ]
            },
            new()
            {
                Name = "RTX HDR Driver Flags", Category = "Modern Upscaling & HDR",
                Description = "Enable RTX HDR for SDR games via the driver. Requires RTX GPU and HDR-capable display.",
                SettingId = 0x00432F84, DefaultHex = "0x00000000",
                Options =
                [
                    new("Off", "0x00000000"),
                    new("Enable RTX HDR", "0x00000002"),
                    new("Enable RTX HDR + Disable Debanding", "0x00000006"),
                    new("Debug Indicator Only", "0x00000001"),
                ]
            },
            new()
            {
                Name = "Smooth Motion — Allowed APIs", Category = "Modern Upscaling & HDR",
                Description = "Restrict which graphics APIs NVIDIA Smooth Motion (driver-level frame interpolation) can activate on. Requires driver ≥ 571.86.",
                SettingId = 0xB0CC0875, DefaultHex = "0x00000000",
                Options =
                [
                    new("Default (None/All)", "0x00000000"),
                    new("DX12 Only", "0x00000001"),
                    new("DX11 Only", "0x00000002"),
                    new("Vulkan Only", "0x00000004"),
                    new("All APIs (DX12 + DX11 + Vulkan)", "0x00000007"),
                ]
            },
            new()
            {
                Name = "G-SYNC Compatibility", Category = "Modern Upscaling & HDR",
                Description = "Force enable G-SYNC Compatible mode on monitors that aren't officially validated.",
                SettingId = 0x109DB0D3, DefaultHex = "0x00000000",
                Options =
                [
                    new("Off (Default)", "0x00000000"),
                    new("On", "0x00000001"),
                ]
            },

            // --- Antialiasing (legacy) ---
            // These split-mode settings remain for drivers < 526.48. Use AA — Combined for newer drivers.
            new()
            {
                Name = "AA — Combined (Mode + Setting)", Category = "Antialiasing",
                Description = "Single combined antialiasing control for drivers ≥ 526.48. Replaces the legacy Antialiasing - Mode + separate sample count workflow. Leave legacy entries below for driver back-compat.",
                SettingId = 0x10D7162F, DefaultHex = "0x00000000",
                Options =
                [
                    new("Application-controlled / Off", "0x00000000"),
                    new("Override | 2x MSAA", "0x1000000E"),
                    new("Override | 2x MSAA + 2x TrSSAA", "0x1140000E"),
                    new("Override | 2x MSAA + 2x SGSSAA", "0x1180000E"),
                    new("Override | 4x MSAA", "0x10000010"),
                    new("Override | 4x MSAA + 4x TrSSAA", "0x12400010"),
                    new("Override | 4x MSAA + 4x SGSSAA", "0x12800010"),
                    new("Override | 8x MSAA", "0x10000025"),
                    new("Override | 8x MSAA + 8x TrSSAA", "0x13400025"),
                    new("Override | 8x MSAA + 8x SGSSAA", "0x13800025"),
                    new("Override | 4x OGSSAA", "0x10000005"),
                    new("Override | 9x OGSSAA", "0x1000000A"),
                    new("Override | 16x OGSSAA", "0x1000000C"),
                    new("Override | 4x OGSSAA + 2x MSAA", "0x10000019"),
                    new("Override | 4x OGSSAA + 4x MSAA", "0x1000001A"),
                    new("Override | 4x OGSSAA + 8x MSAA", "0x10000029"),
                    new("Override | 8xS [1x2 SS + 4x MS]", "0x10000018"),
                    new("Enhance | 2x MSAA", "0x2000000E"),
                    new("Enhance | 4x MSAA", "0x20000010"),
                    new("Enhance | 8x MSAA", "0x20000025"),
                    new("Enhance | 4x OGSSAA", "0x20000005"),
                    new("Enhance | 9x OGSSAA", "0x2000000A"),
                    new("Enhance | 16x OGSSAA", "0x2000000C"),
                    new("Enhance | 4x OGSSAA + 2x MSAA", "0x20000019"),
                    new("Enhance | 4x OGSSAA + 4x MSAA", "0x2000001A"),
                    new("Enhance | 4x OGSSAA + 8x MSAA", "0x20000029"),
                ]
            },
            new()
            {
                Name = "AF — Combined (Mode + Setting)", Category = "Texture Filtering",
                Description = "Single combined anisotropic filtering control for drivers ≥ 526.48. Replaces legacy Anisotropic Filtering Mode + Level.",
                SettingId = 0x10E30043, DefaultHex = "0x00000000",
                Options =
                [
                    new("Application-controlled", "0x00000000"),
                    new("Off [Linear]", "0x10000001"),
                    new("2x", "0x10000002"),
                    new("4x", "0x10000004"),
                    new("8x", "0x10000008"),
                    new("12x", "0x1000000C"),
                    new("16x", "0x10000010"),
                ]
            },
            new()
            {
                Name = "Background App Max Frame Rate", Category = "Performance",
                Description = "Cap FPS when the game window loses focus. Useful for reducing GPU load while alt-tabbed.",
                SettingId = 0x10835006, DefaultHex = "0x00000000",
                Options =
                [
                    new("Off (Unlimited)", "0x00000000"),
                    new("30 FPS", "0x0000001E"),
                    new("60 FPS", "0x0000003C"),
                    new("120 FPS", "0x00000078"),
                ]
            },
            new()
            {
                Name = "Platform Boost", Category = "Performance",
                Description = "Enable GPU Platform Boost for laptops. Allows the driver to dynamically increase GPU clocks. Laptop GPUs only.",
                SettingId = 0x10834FFE, DefaultHex = "0x00000000",
                Options =
                [
                    new("Off (Default)", "0x00000000"),
                    new("On", "0x00000001"),
                ]
            },
            new()
            {
                Name = "Frame Rate Monitor", Category = "Performance",
                Description = "Enable the driver-level frame rate monitor overlay. Diagnostic use.",
                SettingId = 0x10834F01, DefaultHex = "0x00000000",
                Options =
                [
                    new("Off (Default)", "0x00000000"),
                    new("On", "0x00000001"),
                ]
            },
        ];
    }

    public static void ApplyPreset(List<NpiSettingDef> settings, GpuTier tier, string presetName)
    {
        var preset = GetPresets(tier).FirstOrDefault(p => p.Name == presetName);
        if (preset is null) return;

        foreach (var setting in settings)
            setting.SelectedHex = setting.DefaultHex;

        foreach (var (settingName, hexValue) in preset.Values)
        {
            var setting = settings.FirstOrDefault(s => s.Name == settingName);
            if (setting is not null)
                setting.SelectedHex = hexValue;
        }
    }

    public static List<NpiPreset> GetPresets(GpuTier tier)
    {
        var presets = new List<NpiPreset>
        {
            new("Performance", "Max FPS, minimal input lag. Best for competitive/esports.",
            [
                ("Vertical Sync", "0x08416747"),               // Force Off
                ("Max Pre-Rendered Frames", "0x00000001"),     // 1 (Ultra Low Latency)
                ("Power Management Mode", "0x00000001"),       // Prefer Max Performance
                ("Texture Filtering - Quality", "0x00000014"), // High Performance
                ("Threaded Optimization", "0x00000001"),       // On
                ("Shader Cache", "0x00000001"),                // On
            ]),
            new("Balanced", "Good visuals with solid FPS. Best all-around starting point.",
            [
                ("Vertical Sync", "0x60925292"),               // App Controlled
                ("Adaptive VSync (Tear Control)", "0x99941284"), // Enabled
                ("Power Management Mode", "0x00000001"),       // Prefer Max Performance
                ("Texture Filtering - Quality", "0x00000000"), // Quality (Default)
                ("Anisotropic Filtering Mode", "0x00000001"),  // User Override
                ("Anisotropic Filtering Level", "0x00000008"), // 8x
                ("Threaded Optimization", "0x00000001"),       // On
                ("Shader Cache", "0x00000001"),                // On
            ]),
            new("Quality", "Best visuals, higher GPU load. Great for single-player / story games.",
            [
                ("Vertical Sync", "0x47814940"),               // Force On
                ("Triple Buffering", "0x00000001"),            // Enabled
                ("Power Management Mode", "0x00000001"),       // Prefer Max Performance
                ("Texture Filtering - Quality", "0xFFFFFFF6"), // High Quality
                ("Anisotropic Filtering Mode", "0x00000001"),  // User Override
                ("Anisotropic Filtering Level", "0x00000010"), // 16x
                ("Negative LOD Bias", "0x00000001"),           // Clamp
                ("Threaded Optimization", "0x00000001"),       // On
                ("Shader Cache", "0x00000001"),                // On
            ]),
        };

        if (tier >= GpuTier.High)
        {
            presets.Add(new("Ultra Quality", "Everything maxed. Only for high-end GPUs with headroom to spare.",
            [
                ("Vertical Sync", "0x47814940"),               // Force On
                ("Triple Buffering", "0x00000001"),            // Enabled
                ("Power Management Mode", "0x00000001"),       // Prefer Max Performance
                ("Texture Filtering - Quality", "0xFFFFFFF6"), // High Quality
                ("Anisotropic Filtering Mode", "0x00000001"),  // User Override
                ("Anisotropic Filtering Level", "0x00000010"), // 16x
                ("Negative LOD Bias", "0x00000001"),           // Clamp
                ("Trilinear Optimization", "0x00000000"),      // Off (pure quality)
                ("FXAA", "0x00000001"),                        // On
                ("MFAA (Multi-Frame AA)", "0x00000001"),       // On
                ("Threaded Optimization", "0x00000001"),       // On
                ("Shader Cache", "0x00000001"),                // On
            ]));
        }

        return presets;
    }

    public static void GenerateNip(string outputPath, string profileName, List<NpiSettingDef> settings)
    {
        using var ms = new MemoryStream();
        using var writer = new XmlTextWriter(ms, Encoding.Unicode) { Formatting = System.Xml.Formatting.Indented };

        writer.WriteStartDocument();
        writer.WriteStartElement("ArrayOfProfile");

        writer.WriteStartElement("Profile");

        writer.WriteElementString("ProfileName", profileName);

        writer.WriteStartElement("Executeables");
        writer.WriteEndElement();

        writer.WriteStartElement("Settings");
        foreach (var setting in settings.Where(s => s.SelectedHex != s.DefaultHex))
        {
            writer.WriteStartElement("ProfileSetting");
            writer.WriteElementString("SettingNameInfo", setting.Name);
            writer.WriteElementString("SettingID", setting.SettingId.ToString());
            writer.WriteElementString("SettingValue", HexToDecimal(setting.SelectedHex));
            writer.WriteElementString("ValueType", setting.IsString ? "String" : "Dword");
            writer.WriteEndElement();
        }
        writer.WriteEndElement(); // Settings

        writer.WriteEndElement(); // Profile
        writer.WriteEndElement(); // ArrayOfProfile
        writer.WriteEndDocument();

        writer.Flush();
        File.WriteAllBytes(outputPath, ms.ToArray());
    }

    private static string HexToDecimal(string hex)
    {
        var clean = hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? hex[2..] : hex;
        return uint.Parse(clean, System.Globalization.NumberStyles.HexNumber).ToString();
    }
}
