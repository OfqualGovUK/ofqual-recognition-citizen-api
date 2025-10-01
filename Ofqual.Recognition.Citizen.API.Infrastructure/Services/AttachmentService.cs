using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Constants;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Microsoft.AspNetCore.Http;
using Ofqual.Recognition.Citizen.API.Core.Mappers;

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

        Attachment? savedAttachment = await _context.AttachmentRepository.CreateAttachment(file.FileName, file.ContentType, file.Length, upn);
        if (savedAttachment == null)
        {
            return null;
        }

        bool linkCreated = await _context.AttachmentRepository.CreateAttachmentLink(applicationId, savedAttachment.AttachmentId, linkId, linkType, upn);
        if (!linkCreated)
        {
            return null;
        }

        return savedAttachment;
    }

    public async Task<bool> WillExceedAttachmentSizeLimit(Guid applicationId, Guid linkId, LinkType linkType, IFormFile newFile)
    {
        var existingAttachments = await _context.AttachmentRepository.GetAllAttachmentsForLink(applicationId, linkId, linkType);

        long existingTotalSize = existingAttachments.Sum(a => a.FileSize);
        long newTotalSize = existingTotalSize + newFile.Length;

        return newTotalSize > AttachmentConstants.MaxTotalSizeBytes;
    }

    public async Task<List<AttachmentDto>?> GetAllAttachmentsForLink(Guid applicationId, Guid linkId, LinkType linkType)
    {
        var attachments = await _context.AttachmentRepository.GetAllAttachmentsForLink(applicationId, linkId, linkType);
        if (!attachments.Any())
        {
            return null;
        }

        var attachmentDtos = AttachmentMapper.ToDto(attachments);

        var tasks = attachmentDtos.Select(async dto =>
        {
            dto.IsInOtherCriteria = await _context.AttachmentRepository.IsAttachmentInOtherCriteria(dto.FileName, applicationId, linkId);
            return dto;
        });

        return (await Task.WhenAll(tasks)).ToList();
    }
}
