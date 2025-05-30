using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

public interface IAttachmentRepository
{
    public Task<Attachment?> GetLinkedAttachment(Guid applicationId, Guid attachmentId, Guid linkId, LinkTypeEnum linkType);
    public Task<IEnumerable<Attachment>> GetAllAttachmentsForLink(Guid applicationId, Guid linkId, LinkTypeEnum linkType);
    public Task<Attachment?> CreateAttachment(string fileName, string contentType, long size);
    public Task<bool> CreateAttachmentLink(Guid applicationId, Guid attachmentId, Guid linkId, LinkTypeEnum linkTypeId);
    public Task<bool> DeleteAttachmentWithLink(Guid applicationId, Guid attachmentId, Guid linkId, LinkTypeEnum linkType);
}
