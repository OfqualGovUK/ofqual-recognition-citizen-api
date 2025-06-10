using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Core.Mappers;

public static class AttachmentMapper
{
    /// <summary>
    /// Maps a <see cref="Attachment"/> data model to a <see cref="AttachmentDto"/>.
    /// </summary>
    public static AttachmentDto ToDto(Attachment attachment)
    {
        return new AttachmentDto
        {
            AttachmentId = attachment.AttachmentId,
            FileMIMEtype = attachment.FileMIMEtype,
            FileName = attachment.FileName,
            FileSize = attachment.FileSize
        };
    }

    /// <summary>
    /// Maps a collection of <see cref="Attachment"/> to a list of <see cref="AttachmentDto"/>.
    /// </summary>
    public static List<AttachmentDto> ToDto(IEnumerable<Attachment> attachments)
    {
        return attachments
            .OrderBy(a => a.FileName)
            .Select(attachment => new AttachmentDto
            {
                AttachmentId = attachment.AttachmentId,
                FileMIMEtype = attachment.FileMIMEtype,
                FileName = attachment.FileName,
                FileSize = attachment.FileSize
            }).ToList();
    }
}