using System.Management;
using GameProfileManager.Models;

namespace GameProfileManager.Services;

public static class HardwareDetector
{
    public static GpuInfo? DetectGpu()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, AdapterRAM FROM Win32_VideoController");
            GpuInfo? best = null;

            foreach (var obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString() ?? "";
                var ram = Convert.ToInt64(obj["AdapterRAM"] ?? 0);

                // AdapterRAM is a uint32 in WMI, caps at ~4GB. For modern GPUs,
                // fall back to name-based VRAM estimation when value looks capped.
                if (ram <= 0 || (ram == uint.MaxValue))
                    ram = EstimateVramFromName(name);
                else if (ram <= 4L * 1024 * 1024 * 1024 && IsHighEndGpu(name))
                    ram = EstimateVramFromName(name);

                var gpu = new GpuInfo
                {
                    Name = name,
                    VramBytes = ram,
                    Tier = ClassifyTier(name, ram)
                };

                // Pick the GPU with the most VRAM (discrete over integrated)
                if (best is null || gpu.VramBytes > best.VramBytes)
                    best = gpu;
            }

            return best;
        }
        catch
        {
            return null;
        }
    }

    private static GpuTier ClassifyTier(string name, long vramBytes)
    {
        var n = name.ToUpperInvariant();

        // NVIDIA classifications
        if (n.Contains("RTX 5090") || n.Contains("RTX 4090"))
            return GpuTier.Ultra;
        if (n.Contains("RTX 5080") || n.Contains("RTX 4080"))
            return GpuTier.Ultra;
        if (n.Contains("RTX 5070 TI") || n.Contains("RTX 4070 TI"))
            return GpuTier.High;
        if (n.Contains("RTX 5070") || n.Contains("RTX 4070"))
            return GpuTier.High;
        if (n.Contains("RTX 3090") || n.Contains("RTX 3080"))
            return GpuTier.High;
        if (n.Contains("RTX 3070 TI") || n.Contains("RTX 3070"))
            return GpuTier.High;
        if (n.Contains("RTX 5060") || n.Contains("RTX 4060"))
            return GpuTier.Mid;
        if (n.Contains("RTX 3060") || n.Contains("RTX 3050"))
            return GpuTier.Mid;
        if (n.Contains("RTX 20"))
            return GpuTier.Mid;
        if (n.Contains("GTX 16"))
            return GpuTier.Budget;
        if (n.Contains("GTX"))
            return GpuTier.Budget;

        // AMD classifications
        if (n.Contains("RX 9070") || n.Contains("RX 7900"))
            return GpuTier.Ultra;
        if (n.Contains("RX 7800") || n.Contains("RX 7700"))
            return GpuTier.High;
        if (n.Contains("RX 7600") || n.Contains("RX 6700"))
            return GpuTier.Mid;
        if (n.Contains("RX 6600") || n.Contains("RX 6500"))
            return GpuTier.Budget;

        // Intel Arc
        if (n.Contains("ARC A7"))
            return GpuTier.Mid;
        if (n.Contains("ARC"))
            return GpuTier.Budget;

        // Fallback: classify by VRAM
        var vramGb = vramBytes / (1024.0 * 1024 * 1024);
        return vramGb switch
        {
            >= 16 => GpuTier.Ultra,
            >= 10 => GpuTier.High,
            >= 6 => GpuTier.Mid,
            _ => GpuTier.Budget
        };
    }

    private static bool IsHighEndGpu(string name)
    {
        var n = name.ToUpperInvariant();
        return n.Contains("RTX 40") || n.Contains("RTX 50") || n.Contains("RTX 30")
            || n.Contains("RX 79") || n.Contains("RX 78") || n.Contains("RX 90");
    }

    private static long EstimateVramFromName(string name)
    {
        var n = name.ToUpperInvariant();
        long gb = n switch
        {
            _ when n.Contains("4090") || n.Contains("5090") => 24,
            _ when n.Contains("4080") || n.Contains("5080") => 16,
            _ when n.Contains("4070 TI SUPER") => 16,
            _ when n.Contains("4070 TI") || n.Contains("5070 TI") => 12,
            _ when n.Contains("4070 SUPER") || n.Contains("5070") => 12,
            _ when n.Contains("4070") => 12,
            _ when n.Contains("4060 TI") => 8,
            _ when n.Contains("4060") || n.Contains("5060") => 8,
            _ when n.Contains("3090") => 24,
            _ when n.Contains("3080") => 10,
            _ when n.Contains("3070") => 8,
            _ when n.Contains("3060") => 12,
            _ when n.Contains("7900 XTX") => 24,
            _ when n.Contains("7900 XT") => 20,
            _ when n.Contains("7800 XT") => 16,
            _ when n.Contains("7700 XT") => 12,
            _ when n.Contains("7600") => 8,
            _ => 8
        };
        return gb * 1024L * 1024 * 1024;
    }
}
