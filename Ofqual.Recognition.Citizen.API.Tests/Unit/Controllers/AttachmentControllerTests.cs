using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Controllers;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Xunit;
using Moq;

namespace Ofqual.Recognition.Citizen.Tests.Unit.Controllers;

public class AttachmentControllerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IAttachmentRepository> _mockAttachmentRepository = new();
    private readonly Mock<IAzureBlobStorageService> _mockBlobStorage = new();
    private readonly Mock<IAntiVirusService> _mockAntiVirus = new();
    private readonly Mock<IAttachmentService> _mockAttachmentService = new();
    private readonly AttachmentController _controller;

    public AttachmentControllerTests()
    {
        _mockUnitOfWork.Setup(u => u.AttachmentRepository).Returns(_mockAttachmentRepository.Object);

        _controller = new AttachmentController(
            _mockUnitOfWork.Object,
            _mockBlobStorage.Object,
            _mockAntiVirus.Object,
            _mockAttachmentService.Object
        );
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UploadFile_ReturnsOk_WhenFileIsValid()
    {
        // Arrange
        var fileContent = "Fake file content";
        var fileName = "document.pdf";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
        var formFile = new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        var attachment = new Attachment
        {
            AttachmentId = Guid.NewGuid(),
            FileName = fileName,
            FileMIMEtype = formFile.ContentType!,
            FileSize = formFile.Length,
            BlobId = Guid.NewGuid(),
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _mockAntiVirus.Setup(s => s.ScanFile(It.IsAny<Stream>(), fileName)).ReturnsAsync(new AttachmentScannerResult { Status = ScanStatus.Ok });
        _mockAttachmentService.Setup(s => s.SaveAttachmentAndLink(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>(), formFile))
                            .ReturnsAsync(attachment);
        _mockBlobStorage.Setup(s => s.Write(It.IsAny<Guid>(), attachment.BlobId, It.IsAny<Stream>(), false))
                            .ReturnsAsync(true);

        // Act
        var result = await _controller.UploadFile(LinkType.Question, Guid.NewGuid(), Guid.NewGuid(), formFile);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AttachmentDto>(ok.Value);
        Assert.Equal(fileName, dto.FileName);
        _mockUnitOfWork.Verify(u => u.Commit(), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UploadFile_ReturnsBadRequest_WhenFileIsNull()
    {
        // Act
        var result = await _controller.UploadFile(LinkType.Question, Guid.NewGuid(), Guid.NewGuid(), null!);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("A file must be provided and must not be empty.", badRequest.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UploadFile_ReturnsBadRequest_WhenFileIsEmpty()
    {
        // Arrange
        var file = new Mock<IFormFile>();
        file.Setup(f => f.Length).Returns(0);

        // Act
        var result = await _controller.UploadFile(LinkType.Question, Guid.NewGuid(), Guid.NewGuid(), file.Object);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("A file must be provided and must not be empty.", badRequest.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UploadFile_ReturnsBadRequest_WhenFileExtensionIsNotAllowed()
    {
        // Arrange
        var file = new Mock<IFormFile>();
        file.Setup(f => f.Length).Returns(1024);
        file.Setup(f => f.FileName).Returns("malware.exe");

        // Act
        var result = await _controller.UploadFile(LinkType.Question, Guid.NewGuid(), Guid.NewGuid(), file.Object);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.StartsWith("Unsupported file type", badRequest.Value?.ToString());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UploadFile_ReturnsBadRequest_WhenVirusScanFails()
    {
        // Arrange
        var file = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("virus")), 0, 5, "file", "unsafe.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        _mockAntiVirus.Setup(s => s.ScanFile(It.IsAny<Stream>(), "unsafe.pdf"))
                      .ReturnsAsync(new AttachmentScannerResult { Status = ScanStatus.Found });

        // Act
        var result = await _controller.UploadFile(LinkType.Question, Guid.NewGuid(), Guid.NewGuid(), file);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("The uploaded file failed a virus scan and cannot be accepted.", badRequest.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UploadFile_ReturnsBadRequest_WhenAttachmentSaveFails()
    {
        // Arrange
        var file = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("valid")), 0, 5, "file", "valid.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        _mockAntiVirus.Setup(s => s.ScanFile(It.IsAny<Stream>(), "valid.pdf"))
                      .ReturnsAsync(new AttachmentScannerResult { Status = ScanStatus.Ok });
        _mockAttachmentService.Setup(s => s.SaveAttachmentAndLink(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>(), file))
                              .ReturnsAsync((Attachment?)null);

        // Act
        var result = await _controller.UploadFile(LinkType.Question, Guid.NewGuid(), Guid.NewGuid(), file);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Failed to save attachment.", badRequest.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UploadFile_ReturnsBadRequest_WhenBlobWriteFails()
    {
        // Arrange
        var file = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("valid")), 0, 5, "file", "valid.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        var attachment = new Attachment
        {
            AttachmentId = Guid.NewGuid(),
            FileName = "valid.pdf",
            FileMIMEtype = file.ContentType!,
            FileSize = file.Length,
            BlobId = Guid.NewGuid(),
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _mockAntiVirus.Setup(s => s.ScanFile(It.IsAny<Stream>(), "valid.pdf"))
                      .ReturnsAsync(new AttachmentScannerResult { Status = ScanStatus.Ok });
        _mockAttachmentService.Setup(s => s.SaveAttachmentAndLink(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>(), file))
                              .ReturnsAsync(attachment);
        _mockBlobStorage.Setup(s => s.Write(It.IsAny<Guid>(), attachment.BlobId, It.IsAny<Stream>(), false))
                            .ReturnsAsync(false);

        // Act
        var result = await _controller.UploadFile(LinkType.Question, Guid.NewGuid(), Guid.NewGuid(), file);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Unable to store the file in blob storage.", badRequest.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAllFiles_ReturnsOk_WhenAttachmentsExist()
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

        _mockAttachmentRepository.Setup(r => r.GetAllAttachmentsForLink(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
                           .ReturnsAsync(attachments);

        // Act
        var result = await _controller.GetAllFiles(LinkType.Question, Guid.NewGuid(), Guid.NewGuid());

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedList = Assert.IsAssignableFrom<IEnumerable<AttachmentDto>>(okResult.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAllFiles_ReturnsNotFound_WhenNoAttachmentsExist()
    {
        // Arrange
        _mockAttachmentRepository.Setup(r => r.GetAllAttachmentsForLink(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
                           .ReturnsAsync(new List<Attachment>());

        // Act
        var result = await _controller.GetAllFiles(LinkType.Question, Guid.NewGuid(), Guid.NewGuid());

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("No attachments found for the specified entity.", notFound.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DownloadFile_ReturnsFileStream_WhenAttachmentAndBlobExist()
    {
        // Arrange
        var attachment = new Attachment
        {
            AttachmentId = Guid.NewGuid(),
            FileName = "file.txt",
            FileMIMEtype = "text/plain",
            BlobId = Guid.NewGuid(),
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));

        _mockAttachmentRepository.Setup(r => r.GetLinkedAttachment(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
                           .ReturnsAsync(attachment);
        _mockBlobStorage.Setup(s => s.Read(It.IsAny<Guid>(), It.IsAny<Guid>()))
                        .ReturnsAsync(stream);

        // Act
        var result = await _controller.DownloadFile(LinkType.Question, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Assert
        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("text/plain", fileResult.ContentType);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DownloadFile_ReturnsBadRequest_WhenAttachmentNotFound()
    {
        // Arrange
        _mockAttachmentRepository.Setup(r => r.GetLinkedAttachment(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
                           .ReturnsAsync((Attachment?)null);

        // Act
        var result = await _controller.DownloadFile(LinkType.Question, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Attachment is not linked to the specified entity or does not exist.", badRequest.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DownloadFile_ReturnsBadRequest_WhenStreamIsNull()
    {
        // Arrange
        var attachment = new Attachment
        {
            AttachmentId = Guid.NewGuid(),
            FileName = "file.txt",
            FileMIMEtype = "text/plain",
            BlobId = Guid.NewGuid(),
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _mockAttachmentRepository.Setup(r => r.GetLinkedAttachment(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
                           .ReturnsAsync(attachment);
        _mockBlobStorage.Setup(s => s.Read(It.IsAny<Guid>(), It.IsAny<Guid>()))
                        .ReturnsAsync((Stream?)null);

        // Act
        var result = await _controller.DownloadFile(LinkType.Question, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid or incomplete file data. Download cannot proceed.", badRequest.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DownloadFile_ReturnsBadRequest_WhenStreamIsEmpty()
    {
        // Arrange
        var attachment = new Attachment
        {
            AttachmentId = Guid.NewGuid(),
            FileName = "file.txt",
            FileMIMEtype = "text/plain",
            BlobId = Guid.NewGuid(),
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        var emptyStream = new MemoryStream();

        _mockAttachmentRepository.Setup(r => r.GetLinkedAttachment(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
                           .ReturnsAsync(attachment);
        _mockBlobStorage.Setup(s => s.Read(It.IsAny<Guid>(), It.IsAny<Guid>()))
                        .ReturnsAsync(emptyStream);

        // Act
        var result = await _controller.DownloadFile(LinkType.Question, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid or incomplete file data. Download cannot proceed.", badRequest.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DownloadFile_ReturnsBadRequest_WhenMIMETypeOrFileNameMissing()
    {
        // Arrange
        var attachment = new Attachment
        {
            AttachmentId = Guid.NewGuid(),
            FileName = "",
            FileMIMEtype = "",
            BlobId = Guid.NewGuid(),
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        var stream = new MemoryStream(Encoding.UTF8.GetBytes("some content"));

        _mockAttachmentRepository.Setup(r => r.GetLinkedAttachment(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
                           .ReturnsAsync(attachment);
        _mockBlobStorage.Setup(s => s.Read(It.IsAny<Guid>(), It.IsAny<Guid>()))
                        .ReturnsAsync(stream);

        // Act
        var result = await _controller.DownloadFile(LinkType.Question, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid or incomplete file data. Download cannot proceed.", badRequest.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteFile_ReturnsNoContent_WhenDeletionSucceeds()
    {
        // Arrange
        var attachment = new Attachment
        {
            AttachmentId = Guid.NewGuid(),
            BlobId = Guid.NewGuid(),
            FileName = "file.pdf",
            FileMIMEtype = "application/pdf",
            FileSize = 123456,
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _mockAttachmentRepository.Setup(r => r.GetLinkedAttachment(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
                           .ReturnsAsync(attachment);
        _mockAttachmentRepository.Setup(r => r.DeleteAttachmentWithLink(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
                           .ReturnsAsync(true);
        _mockBlobStorage.Setup(s => s.Delete(It.IsAny<Guid>(), It.IsAny<Guid>()))
                        .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteFile(LinkType.Question, Guid.NewGuid(), attachment.AttachmentId, Guid.NewGuid());

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockUnitOfWork.Verify(u => u.Commit(), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteFile_ReturnsBadRequest_WhenAttachmentNotFound()
    {
        // Arrange
        _mockAttachmentRepository.Setup(r => r.GetLinkedAttachment(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
                           .ReturnsAsync((Attachment?)null);

        // Act
        var result = await _controller.DeleteFile(LinkType.Question, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Attachment is not linked to the specified entity or does not exist.", badRequest.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteFile_ReturnsBadRequest_WhenMetadataDeletionFails()
    {
        // Arrange
        var attachment = new Attachment
        {
            AttachmentId = Guid.NewGuid(),
            BlobId = Guid.NewGuid(),
            FileName = "file.pdf",
            FileMIMEtype = "application/pdf",
            FileSize = 123456,
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _mockAttachmentRepository.Setup(r => r.GetLinkedAttachment(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
                           .ReturnsAsync(attachment);
        _mockAttachmentRepository.Setup(r => r.DeleteAttachmentWithLink(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
                           .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteFile(LinkType.Question, Guid.NewGuid(), attachment.AttachmentId, Guid.NewGuid());

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Failed to delete attachment metadata.", badRequest.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteFile_ReturnsBadRequest_WhenBlobDeletionFails()
    {
        // Arrange
        var attachment = new Attachment
        {
            AttachmentId = Guid.NewGuid(),
            BlobId = Guid.NewGuid(),
            FileName = "file.pdf",
            FileMIMEtype = "application/pdf",
            FileSize = 123456,
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _mockAttachmentRepository.Setup(r => r.GetLinkedAttachment(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
                           .ReturnsAsync(attachment);
        _mockAttachmentRepository.Setup(r => r.DeleteAttachmentWithLink(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
                           .ReturnsAsync(true);
        _mockBlobStorage.Setup(r => r.Delete(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteFile(LinkType.Question, Guid.NewGuid(), attachment.AttachmentId, Guid.NewGuid());

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("The file could not be deleted from storage. Attachment metadata was not removed.", badRequest.Value);
    }
}