using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Microsoft.AspNetCore.Http;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;

public interface IAttachmentService
{
    public Task<Attachment?> SaveAttachmentAndLink(Guid applicationId, Guid linkId, LinkType linkType, IFormFile file);
    public Task<bool> WillExceedAttachmentSizeLimit(Guid applicationId, Guid linkId, LinkType linkType, IFormFile newFile);
}