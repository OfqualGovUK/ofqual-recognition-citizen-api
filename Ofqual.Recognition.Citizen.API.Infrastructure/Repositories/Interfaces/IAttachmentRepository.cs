using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

public interface IAttachmentRepository
{
    public Task<Attachment?> GetAttachment(Guid attachmentId);
    public Task<Attachment?> CreateAttachment(Attachment attachment);
    public Task<bool> DeleteAttachment(Guid attachmentId);
    public Task<AttachmentLink?> GetAttachmentLink(Guid attachmentId, Guid linkId, Guid applicationId);
    public Task<bool> CreateAttachmentLink(Guid attachmentId, Guid linkId, LinkTypeEnum linkTypeId, Guid applicationId);
    public Task<bool> DeleteAttachmentLink(Guid attachmentLinkId);
}
