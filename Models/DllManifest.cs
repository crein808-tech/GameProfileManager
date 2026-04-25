using System.Text.Json.Serialization;

namespace GameProfileManager.Models;

public class DllManifest
{
    [JsonPropertyName("dlss")]
    public List<DllRecord> Dlss { get; set; } = [];

    [JsonPropertyName("dlss_g")]
    public List<DllRecord> DlssG { get; set; } = [];

    [JsonPropertyName("dlss_d")]
    public List<DllRecord> DlssD { get; set; } = [];

    [JsonPropertyName("fsr_31_dx12")]
    public List<DllRecord> Fsr31Dx12 { get; set; } = [];

    [JsonPropertyName("fsr_31_vk")]
    public List<DllRecord> Fsr31Vk { get; set; } = [];

    [JsonPropertyName("xess")]
    public List<DllRecord> Xess { get; set; } = [];

    [JsonPropertyName("xess_dx11")]
    public List<DllRecord> XessDx11 { get; set; } = [];

    [JsonPropertyName("xell")]
    public List<DllRecord> Xell { get; set; } = [];

    [JsonPropertyName("xess_fg")]
    public List<DllRecord> XessFg { get; set; } = [];

    public List<DllRecord> GetRecords(DllType type) => type switch
    {
        DllType.Dlss => Dlss,
        DllType.DlssG => DlssG,
        DllType.DlssD => DlssD,
        DllType.Fsr31Dx12 => Fsr31Dx12,
        DllType.Fsr31Vk => Fsr31Vk,
        DllType.Xess => Xess,
        DllType.XessDx11 => XessDx11,
        DllType.Xell => Xell,
        DllType.XessFg => XessFg,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public int TotalCount =>
        Dlss.Count + DlssG.Count + DlssD.Count +
        Fsr31Dx12.Count + Fsr31Vk.Count +
        Xess.Count + XessDx11.Count + Xell.Count + XessFg.Count;
}
