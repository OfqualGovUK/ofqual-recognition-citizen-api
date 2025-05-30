namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class AttachmentScannerResult
{
    public required string Id { get; set; }
    public required string Status { get; set; }
    public required string[] Matches { get; set; }
    public string? Filename { get; set; }
    public long? ContentLength { get; set; }
    public string? Md5 { get; set; }
    public string? Sha256 { get; set; }
    public DateTime? ScannedAt { get; set; }
}