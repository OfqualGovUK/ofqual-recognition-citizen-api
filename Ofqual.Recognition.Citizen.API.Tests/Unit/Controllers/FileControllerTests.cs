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

public class FileControllerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IAttachmentRepository> _mockAttachmentRepo;
    private readonly Mock<IAzureBlobStorageService> _mockBlobStorage;
    private readonly Mock<IAntiVirusService> _mockAntiVirus;
    private readonly FileController _controller;

    public FileControllerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockAttachmentRepo = new Mock<IAttachmentRepository>();
        _mockBlobStorage = new Mock<IAzureBlobStorageService>();
        _mockAntiVirus = new Mock<IAntiVirusService>();

        _mockUnitOfWork.Setup(u => u.AttachmentRepository).Returns(_mockAttachmentRepo.Object);
        _controller = new FileController(_mockUnitOfWork.Object, _mockBlobStorage.Object, _mockAntiVirus.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UploadFile_ReturnsOk_WhenFileIsValid()
    {
        // Arrange
        var fileContent = "Fake file content";
        var fileName = "document.pdf";
        var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
        var formFile = new FormFile(fileStream, 0, fileStream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        var attachment = new Attachment
        {
            AttachmentId = Guid.NewGuid(),
            FileName = fileName,
            FileMIMEtype = "application/pdf",
            FileSize = fileStream.Length,
            BlobId = Guid.NewGuid(),
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _mockAntiVirus.Setup(s => s.ScanFile(It.IsAny<Stream>(), fileName)).ReturnsAsync(new AttachmentScannerResult { Status = ScanStatus.Ok });
        _mockAttachmentRepo.Setup(r => r.CreateAttachment(fileName, formFile.ContentType, formFile.Length)).ReturnsAsync(attachment);
        _mockAttachmentRepo.Setup(r => r.CreateAttachmentLink(It.IsAny<Guid>(), attachment.AttachmentId, It.IsAny<Guid>(), It.IsAny<LinkType>())).ReturnsAsync(true);
        _mockBlobStorage.Setup(s => s.Write(It.IsAny<Guid>(), attachment.BlobId, It.IsAny<Stream>(), false)).ReturnsAsync(true);

        // Act
        var result = await _controller.UploadFile(LinkType.Question, Guid.NewGuid(), Guid.NewGuid(), formFile);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AttachmentDto>(okResult.Value);
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
        Assert.StartsWith("Unsupported file type", badRequest.Value!.ToString());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UploadFile_ReturnsBadRequest_WhenVirusScanFails()
    {
        // Arrange
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("virus"));
        var formFile = new FormFile(stream, 0, stream.Length, "file", "unsafe.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf",
            ContentDisposition = "form-data; name=\"file\"; filename=\"unsafe.pdf\""
        };

        _mockAntiVirus.Setup(s => s.ScanFile(It.IsAny<Stream>(), It.IsAny<string>())).ReturnsAsync(new AttachmentScannerResult { Status = ScanStatus.Found });

        // Act
        var result = await _controller.UploadFile(LinkType.Question, Guid.NewGuid(), Guid.NewGuid(), formFile);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("The uploaded file failed a virus scan and cannot be accepted.", badRequest.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UploadFile_ReturnsBadRequest_WhenCreateAttachmentFails()
    {
        // Arrange
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("file"));
        var formFile = new FormFile(stream, 0, stream.Length, "file", "doc.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf",
            ContentDisposition = "form-data; name=\"file\"; filename=\"doc.pdf\""
        };

        _mockAntiVirus.Setup(s => s.ScanFile(It.IsAny<Stream>(), It.IsAny<string>()))
                      .ReturnsAsync(new AttachmentScannerResult { Status = ScanStatus.Ok });
        _mockAttachmentRepo.Setup(r => r.CreateAttachment(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()))
                           .ReturnsAsync((Attachment?)null);

        // Act
        var result = await _controller.UploadFile(LinkType.Question, Guid.NewGuid(), Guid.NewGuid(), formFile);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Failed to save attachment metadata.", badRequest.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UploadFile_ReturnsBadRequest_WhenCreateAttachmentLinkFails()
    {
        // Arrange
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("valid"));
        var formFile = new FormFile(stream, 0, stream.Length, "file", "report.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf",
            ContentDisposition = "form-data; name=\"file\"; filename=\"report.pdf\""
        };

        var attachment = new Attachment
        {
            AttachmentId = Guid.NewGuid(),
            FileName = "report.pdf",
            FileMIMEtype = "application/pdf",
            FileSize = stream.Length,
            BlobId = Guid.NewGuid(),
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _mockAntiVirus.Setup(s => s.ScanFile(It.IsAny<Stream>(), It.IsAny<string>()))
                      .ReturnsAsync(new AttachmentScannerResult { Status = ScanStatus.Ok });
        _mockAttachmentRepo.Setup(r => r.CreateAttachment(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()))
                           .ReturnsAsync(attachment);
        _mockAttachmentRepo.Setup(r => r.CreateAttachmentLink(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
                           .ReturnsAsync(false);

        // Act
        var result = await _controller.UploadFile(LinkType.Question, Guid.NewGuid(), Guid.NewGuid(), formFile);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Failed to create attachment link.", badRequest.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UploadFile_ReturnsBadRequest_WhenBlobStorageWriteFails()
    {
        // Arrange
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("valid"));
        var formFile = new FormFile(stream, 0, stream.Length, "file", "report.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf",
            ContentDisposition = "form-data; name=\"file\"; filename=\"report.pdf\""
        };

        var attachment = new Attachment
        {
            AttachmentId = Guid.NewGuid(),
            FileName = "report.pdf",
            FileMIMEtype = "application/pdf",
            FileSize = stream.Length,
            BlobId = Guid.NewGuid(),
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _mockAntiVirus.Setup(s => s.ScanFile(It.IsAny<Stream>(), It.IsAny<string>()))
                           .ReturnsAsync(new AttachmentScannerResult { Status = ScanStatus.Ok });
        _mockAttachmentRepo.Setup(r => r.CreateAttachment(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()))
                           .ReturnsAsync(attachment);
        _mockAttachmentRepo.Setup(r => r.CreateAttachmentLink(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
                           .ReturnsAsync(true);
        _mockBlobStorage.Setup(s => s.Write(It.IsAny<Guid>(), attachment.BlobId, It.IsAny<Stream>(), false)).ReturnsAsync(false);

        // Act
        var result = await _controller.UploadFile(LinkType.Question, Guid.NewGuid(), Guid.NewGuid(), formFile);

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

        _mockAttachmentRepo.Setup(r => r.GetAllAttachmentsForLink(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
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
        _mockAttachmentRepo.Setup(r => r.GetAllAttachmentsForLink(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
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

        _mockAttachmentRepo.Setup(r => r.GetLinkedAttachment(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
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
        _mockAttachmentRepo.Setup(r => r.GetLinkedAttachment(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
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

        _mockAttachmentRepo.Setup(r => r.GetLinkedAttachment(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
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

        _mockAttachmentRepo.Setup(r => r.GetLinkedAttachment(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
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

        _mockAttachmentRepo.Setup(r => r.GetLinkedAttachment(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
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

        _mockAttachmentRepo.Setup(r => r.GetLinkedAttachment(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
                           .ReturnsAsync(attachment);
        _mockAttachmentRepo.Setup(r => r.DeleteAttachmentWithLink(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
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
        _mockAttachmentRepo.Setup(r => r.GetLinkedAttachment(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
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

        _mockAttachmentRepo.Setup(r => r.GetLinkedAttachment(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
                           .ReturnsAsync(attachment);
        _mockAttachmentRepo.Setup(r => r.DeleteAttachmentWithLink(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
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

        _mockAttachmentRepo.Setup(r => r.GetLinkedAttachment(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
                           .ReturnsAsync(attachment);
        _mockAttachmentRepo.Setup(r => r.DeleteAttachmentWithLink(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
                           .ReturnsAsync(true);
        _mockBlobStorage.Setup(r => r.Delete(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteFile(LinkType.Question, Guid.NewGuid(), attachment.AttachmentId, Guid.NewGuid());

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("The file could not be deleted from storage. Attachment metadata was not removed.", badRequest.Value);
    }
}