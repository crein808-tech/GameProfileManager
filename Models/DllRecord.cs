using System.Text.Json.Serialization;

namespace GameProfileManager.Models;

public class DllRecord
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("version_number")]
    public ulong VersionNumber { get; set; }

    [JsonPropertyName("internal_name")]
    public string InternalName { get; set; } = string.Empty;

    [JsonPropertyName("internal_name_extra")]
    public string InternalNameExtra { get; set; } = string.Empty;

    [JsonPropertyName("additional_label")]
    public string AdditionalLabel { get; set; } = string.Empty;

    [JsonPropertyName("md5_hash")]
    public string Md5Hash { get; set; } = string.Empty;

    [JsonPropertyName("zip_md5_hash")]
    public string ZipMd5Hash { get; set; } = string.Empty;

    [JsonPropertyName("download_url")]
    public string DownloadUrl { get; set; } = string.Empty;

    [JsonPropertyName("file_description")]
    public string FileDescription { get; set; } = string.Empty;

    [JsonPropertyName("signed_datetime")]
    public string SignedDatetime { get; set; } = string.Empty;

    [JsonPropertyName("is_signature_valid")]
    public bool IsSignatureValid { get; set; }

    [JsonPropertyName("is_dev_file")]
    public bool IsDevFile { get; set; }

    [JsonPropertyName("file_size")]
    public long FileSize { get; set; }

    [JsonPropertyName("zip_file_size")]
    public long ZipFileSize { get; set; }

    [JsonPropertyName("dll_source")]
    public string DllSource { get; set; } = string.Empty;
}
