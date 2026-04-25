namespace GameProfileManager.Models;

public enum DllType
{
    Dlss,
    DlssG,
    DlssD,
    Fsr31Dx12,
    Fsr31Vk,
    Xess,
    XessDx11,
    Xell,
    XessFg
}

public record DllTypeInfo(string Filename, string ManifestKey, string Description);

public static class DllTypeMap
{
    private static readonly Dictionary<DllType, DllTypeInfo> Info = new()
    {
        [DllType.Dlss] = new("nvngx_dlss.dll", "dlss",
            "NVIDIA DLSS — AI upscaling, renders at lower res then upscales for better FPS"),
        [DllType.DlssG] = new("nvngx_dlssg.dll", "dlss_g",
            "DLSS Frame Gen — AI-generated intermediate frames for higher perceived FPS"),
        [DllType.DlssD] = new("nvngx_dlssd.dll", "dlss_d",
            "DLSS Ray Reconstruction — AI denoiser that replaces traditional ray tracing denoiser"),
        [DllType.Fsr31Dx12] = new("amd_fidelityfx_dx12.dll", "fsr_31_dx12",
            "AMD FSR 3.1 (DX12) — open-source upscaling + frame gen, works on any GPU"),
        [DllType.Fsr31Vk] = new("amd_fidelityfx_vk.dll", "fsr_31_vk",
            "AMD FSR 3.1 (Vulkan) — same as FSR DX12 but for Vulkan-based games"),
        [DllType.Xess] = new("libxess.dll", "xess",
            "Intel XeSS — AI upscaling from Intel, works on any GPU with DP4a support"),
        [DllType.XessDx11] = new("libxess_dx11.dll", "xess_dx11",
            "Intel XeSS (DX11) — XeSS variant for older DX11 games"),
        [DllType.Xell] = new("libxell.dll", "xell",
            "Intel XeLL — low-latency module for XeSS frame generation"),
        [DllType.XessFg] = new("libxess_fg.dll", "xess_fg",
            "Intel XeSS Frame Gen — Intel's AI frame generation, similar to DLSS-G"),
    };

    public static IReadOnlyDictionary<DllType, string> Filenames { get; } =
        Info.ToDictionary(kv => kv.Key, kv => kv.Value.Filename);

    public static IReadOnlyDictionary<DllType, string> ManifestKeys { get; } =
        Info.ToDictionary(kv => kv.Key, kv => kv.Value.ManifestKey);

    public static IReadOnlyDictionary<DllType, string> Descriptions { get; } =
        Info.ToDictionary(kv => kv.Key, kv => kv.Value.Description);
}
