namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class AttachmentDto
{
    public Guid AttachmentId { get; set; }
    public required string FileName { get; set; }
    public required string FileMIMEtype { get; set; }
    public long FileSize { get; set; }
    public bool IsInOtherCriteria { get; set; }
}