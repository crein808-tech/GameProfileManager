namespace GameProfileManager.Models;

public enum GpuTier
{
    Budget,
    Mid,
    High,
    Ultra
}

public enum GpuVendor { Unknown, Nvidia, Amd, Intel }

public class GpuInfo
{
    public required string Name { get; set; }
    public long VramBytes { get; set; }
    public GpuTier Tier { get; set; }
    public GpuVendor Vendor { get; set; }

    public bool IsNvidia => Vendor == GpuVendor.Nvidia;

    public string VramDisplay => VramBytes >= 1024L * 1024 * 1024
        ? $"{VramBytes / (1024.0 * 1024 * 1024):F0} GB"
        : $"{VramBytes / (1024.0 * 1024):F0} MB";

    public string TierDisplay => Tier switch
    {
        GpuTier.Budget => "Budget",
        GpuTier.Mid => "Mid-Range",
        GpuTier.High => "High-End",
        GpuTier.Ultra => "Ultra / Flagship",
        _ => "Unknown"
    };

    public override string ToString() => $"{Name}  ({VramDisplay})  —  {TierDisplay}";
}
