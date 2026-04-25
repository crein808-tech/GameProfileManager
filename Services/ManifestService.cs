using System.Net.Http;
using System.Text.Json;
using GameProfileManager.Models;

namespace GameProfileManager.Services;

public static class ManifestService
{
    private const string ManifestUrl = "https://beeradmoore.github.io/dlss-swapper/manifest.json";

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
        DefaultRequestHeaders = { { "User-Agent", "GameProfileManager/1.0" } }
    };

    public static async Task<DllManifest> FetchManifestAsync()
    {
        var json = await Http.GetStringAsync(ManifestUrl);
        return JsonSerializer.Deserialize<DllManifest>(json)
            ?? throw new InvalidOperationException("Failed to deserialize manifest");
    }
}
