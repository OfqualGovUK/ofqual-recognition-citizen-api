using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Microsoft.AspNetCore.Http;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services;

public class AttachmentService : IAttachmentService
{
    private readonly IUnitOfWork _context;
    private readonly IUserInformationService _userInformationService;

    public AttachmentService(IUnitOfWork context, IUserInformationService userInformationService)
    {
        _context = context;
        _userInformationService = userInformationService;
    }

    public async Task<Attachment?> SaveAttachmentAndLink(Guid applicationId, Guid linkId, LinkType linkType, IFormFile file)
    {
        string upn = _userInformationService.GetCurrentUserUpn();

        // Save attachment metadata
        Attachment? savedAttachment = await _context.AttachmentRepository.CreateAttachment(file.FileName, file.ContentType, file.Length, upn);
        if (savedAttachment == null)
        {
            return null;
        }

        // Create the attachment link
        bool linkCreated = await _context.AttachmentRepository.CreateAttachmentLink(applicationId, savedAttachment.AttachmentId, linkId, linkType, upn);
        if (!linkCreated)
        {
            return null;
        }

        return savedAttachment;
    }
}
