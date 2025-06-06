using Ofqual.Recognition.Citizen.API.Core.Enums;
using Newtonsoft.Json;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class AttachmentScannerResult
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("status")]
    public ScanStatus? Status { get; set; }

    [JsonProperty("matches")]
    public string[] Matches { get; set; } = Array.Empty<string>();

    [JsonProperty("url")]
    public string? Url { get; set; }

    [JsonProperty("filename")]
    public string? Filename { get; set; }

    [JsonProperty("content_length")]
    public long? ContentLength { get; set; }

    [JsonProperty("md5")]
    public string? Md5 { get; set; }

    [JsonProperty("sha256")]
    public string? Sha256 { get; set; }

    [JsonProperty("callback")]
    public string? Callback { get; set; }

    [JsonProperty("created_at")]
    public DateTime? CreatedAt { get; set; }
    
    [JsonProperty("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}