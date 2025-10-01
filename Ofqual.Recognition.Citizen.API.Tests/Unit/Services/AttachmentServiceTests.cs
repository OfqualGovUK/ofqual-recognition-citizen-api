using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Xunit;

namespace Ofqual.Recognition.Citizen.Tests.Unit.Services;

public class AttachmentServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IAttachmentRepository> _mockAttachmentRepository = new();
    private readonly Mock<IUserInformationService> _mockUserInformationService = new();
    private readonly AttachmentService _service;

    public AttachmentServiceTests()
    {
        _mockUnitOfWork.Setup(u => u.AttachmentRepository).Returns(_mockAttachmentRepository.Object);
        _mockUserInformationService.Setup(u => u.GetCurrentUserUpn()).Returns("test@ofqual.gov.uk");

        _service = new AttachmentService(_mockUnitOfWork.Object, _mockUserInformationService.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SaveAttachmentAndLink_ReturnsAttachment_WhenSuccessful()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var linkId = Guid.NewGuid();
        var linkType = LinkType.Question;

        var file = new FormFile(new MemoryStream(new byte[] { 1, 2, 3 }), 0, 3, "file", "test.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        var attachment = new Attachment
        {
            AttachmentId = Guid.NewGuid(),
            FileName = "test.pdf",
            FileMIMEtype = "application/pdf",
            FileSize = 3,
            BlobId = Guid.NewGuid(),
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _mockAttachmentRepository
            .Setup(r => r.CreateAttachment(file.FileName, file.ContentType, file.Length, "test@ofqual.gov.uk"))
            .ReturnsAsync(attachment);

        _mockAttachmentRepository
            .Setup(r => r.CreateAttachmentLink(applicationId, attachment.AttachmentId, linkId, linkType, "test@ofqual.gov.uk"))
            .ReturnsAsync(true);

        // Act
        var result = await _service.SaveAttachmentAndLink(applicationId, linkId, linkType, file);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(attachment.AttachmentId, result.AttachmentId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SaveAttachmentAndLink_ReturnsNull_WhenCreateAttachmentFails()
    {
        // Arrange
        var fileStream = new MemoryStream(new byte[] { 1 });
        var file = new FormFile(fileStream, 0, fileStream.Length, "file", "fail.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        _mockAttachmentRepository
            .Setup(r => r.CreateAttachment(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>()))
            .ReturnsAsync((Attachment?)null);

        // Act
        var result = await _service.SaveAttachmentAndLink(Guid.NewGuid(), Guid.NewGuid(), LinkType.Question, file);

        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    [Trait("Category", "Unit")]
    public async Task SaveAttachmentAndLink_ReturnsNull_WhenCreateAttachmentLinkFails()
    {
        // Arrange
        var fileStream = new MemoryStream(new byte[] { 1 });
        var file = new FormFile(fileStream, 0, fileStream.Length, "file", "linkfail.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        var applicationId = Guid.NewGuid();
        var linkId = Guid.NewGuid();
        var linkType = LinkType.Question;

        var attachment = new Attachment
        {
            AttachmentId = Guid.NewGuid(),
            FileName = "linkfail.pdf",
            FileMIMEtype = "application/pdf",
            FileSize = fileStream.Length,
            BlobId = Guid.NewGuid(),
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _mockAttachmentRepository
            .Setup(r => r.CreateAttachment(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>()))
            .ReturnsAsync(attachment);

        _mockAttachmentRepository
            .Setup(r => r.CreateAttachmentLink(applicationId, attachment.AttachmentId, linkId, linkType, "test@ofqual.gov.uk"))
            .ReturnsAsync(false);

        // Act
        var result = await _service.SaveAttachmentAndLink(applicationId, linkId, linkType, file);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAllAttachmentsForLink_ReturnsDtos_WhenAttachmentsExist()
    {
        // Arrange
        var attachments = new List<Attachment>
        {
            new Attachment
            {
                AttachmentId = Guid.NewGuid(),
                FileName = "test.pdf",
                FileMIMEtype = "application/pdf",
                FileSize = 1024,
                BlobId = Guid.NewGuid(),
                CreatedByUpn = "test@ofqual.gov.uk",
                ModifiedByUpn = "test@ofqual.gov.uk",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            }
        };

        _mockAttachmentRepository
            .Setup(r => r.GetAllAttachmentsForLink(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
            .ReturnsAsync(attachments);

        _mockAttachmentRepository
            .Setup(r => r.IsAttachmentInOtherCriteria(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        var applicationId = Guid.NewGuid();
        var linkId = Guid.NewGuid();

        // Act
        var result = await _service.GetAllAttachmentsForLink(applicationId, linkId, LinkType.Question);

        // Assert
        Assert.NotNull(result);
        var returnedList = Assert.IsAssignableFrom<IEnumerable<AttachmentDto>>(result);

        var dto = returnedList.First();
        Assert.Equal("test.pdf", dto.FileName);
        Assert.Equal("application/pdf", dto.FileMIMEtype);
        Assert.Equal(1024, dto.FileSize);
        Assert.False(dto.IsInOtherCriteria);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAllAttachmentsForLink_SetsIsInOtherCriteriaTrue_WhenRepoReturnsTrue()
    {
        // Arrange
        var attachments = new List<Attachment>
        {
            new Attachment
            {
                AttachmentId = Guid.NewGuid(),
                FileName = "duplicate.pdf",
                FileMIMEtype = "application/pdf",
                FileSize = 2048,
                BlobId = Guid.NewGuid(),
                CreatedByUpn = "test@ofqual.gov.uk",
                ModifiedByUpn = "test@ofqual.gov.uk",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            }
        };

        _mockAttachmentRepository
            .Setup(r => r.GetAllAttachmentsForLink(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
            .ReturnsAsync(attachments);

        _mockAttachmentRepository
            .Setup(r => r.IsAttachmentInOtherCriteria(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var applicationId = Guid.NewGuid();
        var linkId = Guid.NewGuid();

        // Act
        var result = await _service.GetAllAttachmentsForLink(applicationId, linkId, LinkType.Question);

        // Assert
        Assert.NotNull(result);
        var returnedList = Assert.IsAssignableFrom<IEnumerable<AttachmentDto>>(result);

        var dto = returnedList.First();
        Assert.Equal("duplicate.pdf", dto.FileName);
        Assert.Equal("application/pdf", dto.FileMIMEtype);
        Assert.Equal(2048, dto.FileSize);
        Assert.True(dto.IsInOtherCriteria);
    }
}