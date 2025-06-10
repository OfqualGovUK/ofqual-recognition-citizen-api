namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class Attachment
{
    public Guid AttachmentId { get; set; }
    public required string FileName { get; set; }
    public Guid BlobId { get; set; }
    public string? DirectoryPath { get; set; }
    public required string FileMIMEtype { get; set; }
    public long FileSize { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public required string CreatedByUpn { get; set; }
    public required string ModifiedByUpn { get; set; }
}