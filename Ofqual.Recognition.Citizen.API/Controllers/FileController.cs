using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Helpers;
using Ofqual.Recognition.Citizen.API.Core.Mappers;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web.Resource;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Ofqual.Recognition.Citizen.API.Attributes;

namespace Ofqual.Recognition.Citizen.API.Controllers;

[ApiController]
[Route("files")]
[Authorize]
[RequiredScope("Applications.ReadWrite")]
public class FileController : ControllerBase
{
    private readonly IUnitOfWork _context;
    private readonly IAzureBlobStorageService _blobStorage;
    private readonly IAntiVirusService _antiVirus;

    public FileController(IUnitOfWork context, IAzureBlobStorageService blobStorage, IAntiVirusService antiVirus)
    {
        _context = context;
        _blobStorage = blobStorage;
        _antiVirus = antiVirus;
    }

    /// <summary>
    /// Uploads a file and links it to a specific entity.
    /// </summary>
    /// <param name="linkType">The type of link the file is associated with.</param>
    /// <param name="linkId">The unique identifier of the entity the file is linked to.</param>
    /// <param name="applicationId">The unique identifier of the application to associate the file with.</param>
    /// <returns>The created attachment details.</returns>
    [HttpPost("linked/{linkType}/{linkId}/application/{applicationId}")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 25 * 1024 * 1024)]
    [CheckApplicationId(queryParam: "applicationId")]
    public async Task<ActionResult<AttachmentDto>> UploadFile(LinkType linkType, Guid linkId, Guid applicationId, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("A file must be provided and must not be empty.");
            }

            if (!FileValidationHelper.IsAllowedFile(file))
            {
                return BadRequest("Unsupported file type or content. Allowed: CSV, JPEG, PNG, Excel, Word, PDF and text formats.");
            }

            await using var scanStream = file.OpenReadStream();
            AttachmentScannerResult? scanResult = await _antiVirus.ScanFile(scanStream, file.FileName);
            if (scanResult?.Status != ScanStatus.Ok)
            {
                return BadRequest("The uploaded file failed a virus scan and cannot be accepted.");
            }

            Attachment? savedAttachment = await _context.AttachmentRepository.CreateAttachment(file.FileName, file.ContentType, file.Length);
            if (savedAttachment == null)
            {
                return BadRequest("Failed to save attachment metadata.");
            }

            bool linkCreated = await _context.AttachmentRepository.CreateAttachmentLink(applicationId, savedAttachment.AttachmentId, linkId, linkType);
            if (!linkCreated)
            {
                return BadRequest("Failed to create attachment link.");
            }

            await using var writeStream = file.OpenReadStream();
            bool isBlobStored = await _blobStorage.Write(applicationId, savedAttachment.BlobId, writeStream);
            if (!isBlobStored)
            {
                return BadRequest("Unable to store the file in blob storage.");
            }

            AttachmentDto savedAttachmentDto = AttachmentMapper.ToDto(savedAttachment);

            _context.Commit();
            return Ok(savedAttachmentDto);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred whilst uploading file for LinkType {LinkType}, LinkId {LinkId}, ApplicationId {ApplicationId}", linkType, linkId, applicationId);
            throw new Exception("An error occurred while uploading the file. Please try again later.");
        }
    }

    /// <summary>
    /// Retrieves all files linked to a specific entity, scoped by link type and link ID.
    /// </summary>
    /// <param name="linkType">The type of link the files are associated with.</param>
    /// <param name="linkId">The unique identifier of the entity the files are linked to.</param>
    /// <returns>A list of attachments linked to the specified entity.</returns>
    [HttpGet("linked/{linkType}/{linkId}/application/{applicationId}")]
    [CheckApplicationId(queryParam: "applicationId")]
    public async Task<ActionResult<List<Attachment>>> GetAllFiles(LinkType linkType, Guid linkId, Guid applicationId)
    {
        try
        {
            var attachments = await _context.AttachmentRepository.GetAllAttachmentsForLink(applicationId, linkId, linkType);
            if (!attachments.Any())
            {
                return NotFound("No attachments found for the specified entity.");
            }

            var attachmentDtos = AttachmentMapper.ToDto(attachments);

            return Ok(attachmentDtos);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred whilst retrieving attachments for LinkType {LinkType}, LinkId {LinkId}, ApplicationId {ApplicationId}", linkType, linkId, applicationId);
            throw new Exception("An error occurred while fetching the files. Please try again later.");
        }
    }

    /// <summary>
    /// Downloads a file by its attachment ID, scoped to a specific link type and link ID.
    /// </summary>
    /// <param name="linkType">The type of link the file is associated with.</param>
    /// <param name="linkId">The unique identifier of the entity the file is linked to.</param>
    /// <param name="attachmentId">The unique identifier of the attachment to download.</param>
    /// <param name="applicationId">The unique identifier of the application to associate the file with.</param>
    /// <returns>The file stream with appropriate MIME type and file name.</returns>
    [HttpGet("linked/{linkType}/{linkId}/attachment/{attachmentId}/application/{applicationId}")]
    [CheckApplicationId(queryParam: "applicationId")]
    public async Task<IActionResult> DownloadFile(LinkType linkType, Guid linkId, Guid attachmentId, Guid applicationId)
    {
        try
        {
            Attachment? attachment = await _context.AttachmentRepository.GetLinkedAttachment(applicationId, attachmentId, linkId, linkType);
            if (attachment == null)
            {
                return BadRequest("Attachment is not linked to the specified entity or does not exist.");
            }

            Stream? stream = await _blobStorage.Read(applicationId, attachment.BlobId);
            if (stream == null || stream.Length == 0 || string.IsNullOrWhiteSpace(attachment.FileMIMEtype) || string.IsNullOrWhiteSpace(attachment.FileName))
            {
                return BadRequest("Invalid or incomplete file data. Download cannot proceed.");
            }

            return File(stream, attachment.FileMIMEtype, attachment.FileName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred whilst downloading attachment with ID: {AttachmentId} (LinkType: {LinkType}, LinkId: {LinkId}, ApplicationId: {ApplicationId})", attachmentId, linkType, linkId, applicationId);
            throw new Exception("An error occurred while downloading the file. Please try again later.");
        }
    }

    /// <summary>
    /// Deletes a file by its attachment ID, scoped to a specific link type and link ID.
    /// </summary>
    /// <param name="linkType">The type of link the file is associated with.</param>
    /// <param name="linkId">The unique identifier of the entity the file is linked to.</param>
    /// <param name="attachmentId">The unique identifier of the attachment to delete.</param>
    /// <param name="applicationId">The unique identifier of the application to associate the file with.</param>
    /// <returns>No content if the deletion is successful.</returns>
    [HttpDelete("linked/{linkType}/{linkId}/attachment/{attachmentId}/application/{applicationId}")]
    [CheckApplicationId(queryParam: "applicationId")]
    public async Task<IActionResult> DeleteFile(LinkType linkType, Guid linkId, Guid attachmentId, Guid applicationId)
    {
        try
        {
            Attachment? attachment = await _context.AttachmentRepository.GetLinkedAttachment(applicationId, attachmentId, linkId, linkType);
            if (attachment == null)
            {
                return BadRequest("Attachment is not linked to the specified entity or does not exist.");
            }

            bool deleted = await _context.AttachmentRepository.DeleteAttachmentWithLink(applicationId, attachmentId, linkId, linkType);
            if (!deleted)
            {
                return BadRequest("Failed to delete attachment metadata.");
            }

            bool isBlobDeleted = await _blobStorage.Delete(applicationId, attachment.BlobId);
            if (!isBlobDeleted)
            {
                return BadRequest("The file could not be deleted from storage. Attachment metadata was not removed.");
            }

            _context.Commit();
            return NoContent();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred whilst deleting attachment with ID: {AttachmentId} (LinkType: {LinkType}, LinkId: {LinkId}, ApplicationId: {ApplicationId})", attachmentId, linkType, linkId, applicationId);
            throw new Exception("An error occurred while deleting the file. Please try again later.");
        }
    }
}