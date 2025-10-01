using Ofqual.Recognition.Citizen.Tests.Integration.Builders;
using Ofqual.Recognition.Citizen.Tests.Integration.Fixtures;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Xunit;

namespace Ofqual.Recognition.Citizen.Tests.Integration.Repositories;

public class AttachmentRepositoryTests : IClassFixture<SqlTestFixture>
{
    private readonly SqlTestFixture _fixture;

    public AttachmentRepositoryTests(SqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Create_And_Retrieve_Attachment()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Act
        var attachment = await unitOfWork.AttachmentRepository.CreateAttachment("file.pdf", "application/pdf", 12345, "test@ofqual.gov.uk");

        unitOfWork.Commit();

        // Assert
        Assert.NotNull(attachment);
        Assert.Equal("file.pdf", attachment.FileName);
        Assert.Equal("application/pdf", attachment.FileMIMEtype);
        Assert.Equal(12345, attachment.FileSize);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Create_And_Link_Attachment()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var user = await UserTestDataBuilder.CreateTestUser(unitOfWork, new User
        {
            B2CId = Guid.NewGuid(),
            EmailAddress = "test@ofqual.gov.uk",
            DisplayName = "Ofqual Test Account",
            CreatedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedDate = DateTime.UtcNow,
            ModifiedByUpn = "test@ofqual.gov.uk"
        });

        var application = await ApplicationTestDataBuilder.CreateTestApplication(unitOfWork, new Application
        {
            ApplicationId = Guid.NewGuid(),
            OwnerUserId = user.UserId,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var linkId = Guid.NewGuid();

        var attachment = await unitOfWork.AttachmentRepository.CreateAttachment("image.png", "image/png", 45678, application.CreatedByUpn);
        unitOfWork.Commit();

        // Act
        var linked = await unitOfWork.AttachmentRepository.CreateAttachmentLink(application.ApplicationId, attachment!.AttachmentId, linkId, LinkType.Question, application.CreatedByUpn);
        unitOfWork.Commit();

        // Assert
        Assert.True(linked);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Get_Linked_Attachment()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var user = await UserTestDataBuilder.CreateTestUser(unitOfWork, new User
        {
            B2CId = Guid.NewGuid(),
            EmailAddress = "test@ofqual.gov.uk",
            DisplayName = "Ofqual Test Account",
            CreatedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedDate = DateTime.UtcNow,
            ModifiedByUpn = "test@ofqual.gov.uk"
        });

        var application = await ApplicationTestDataBuilder.CreateTestApplication(unitOfWork, new Application
        {
            ApplicationId = Guid.NewGuid(),
            OwnerUserId = user.UserId,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var linkId = Guid.NewGuid();

        var attachment = await unitOfWork.AttachmentRepository.CreateAttachment("doc.txt", "text/plain", 7890, application.CreatedByUpn);
        await unitOfWork.AttachmentRepository.CreateAttachmentLink(application.ApplicationId, attachment!.AttachmentId, linkId, LinkType.Question, application.CreatedByUpn);

        unitOfWork.Commit();

        // Act
        var fetched = await unitOfWork.AttachmentRepository.GetLinkedAttachment(application.ApplicationId, attachment.AttachmentId, linkId, LinkType.Question);

        // Assert
        Assert.NotNull(fetched);
        Assert.Equal(attachment.AttachmentId, fetched!.AttachmentId);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Get_All_Attachments_For_Link()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var user = await UserTestDataBuilder.CreateTestUser(unitOfWork, new User
        {
            B2CId = Guid.NewGuid(),
            EmailAddress = "test@ofqual.gov.uk",
            DisplayName = "Ofqual Test Account",
            CreatedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedDate = DateTime.UtcNow,
            ModifiedByUpn = "test@ofqual.gov.uk"
        });

        var application = await ApplicationTestDataBuilder.CreateTestApplication(unitOfWork, new Application
        {
            ApplicationId = Guid.NewGuid(),
            OwnerUserId = user.UserId,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var linkId = Guid.NewGuid();

        var att1 = await unitOfWork.AttachmentRepository.CreateAttachment("one.pdf", "application/pdf", 111, application.CreatedByUpn);
        var att2 = await unitOfWork.AttachmentRepository.CreateAttachment("two.docx", "application/vnd.openxmlformats", 222, application.CreatedByUpn);

        await unitOfWork.AttachmentRepository.CreateAttachmentLink(application.ApplicationId, att1!.AttachmentId, linkId, LinkType.Question, application.CreatedByUpn);
        await unitOfWork.AttachmentRepository.CreateAttachmentLink(application.ApplicationId, att2!.AttachmentId, linkId, LinkType.Question, application.CreatedByUpn);
        unitOfWork.Commit();

        // Act
        var results = (await unitOfWork.AttachmentRepository.GetAllAttachmentsForLink(application.ApplicationId, linkId, LinkType.Question)).ToList();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(results, x => x.AttachmentId == att1.AttachmentId);
        Assert.Contains(results, x => x.AttachmentId == att2.AttachmentId);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Delete_Attachment_With_Link()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var user = await UserTestDataBuilder.CreateTestUser(unitOfWork, new User
        {
            B2CId = Guid.NewGuid(),
            EmailAddress = "test@ofqual.gov.uk",
            DisplayName = "Ofqual Test Account",
            CreatedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedDate = DateTime.UtcNow,
            ModifiedByUpn = "test@ofqual.gov.uk"
        });

        var application = await ApplicationTestDataBuilder.CreateTestApplication(unitOfWork, new Application
        {
            ApplicationId = Guid.NewGuid(),
            OwnerUserId = user.UserId,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var linkId = Guid.NewGuid();

        var attachment = await unitOfWork.AttachmentRepository.CreateAttachment("delete-me.txt", "text/plain", 100, application.CreatedByUpn);
        await unitOfWork.AttachmentRepository.CreateAttachmentLink(application.ApplicationId, attachment!.AttachmentId, linkId, LinkType.Question, application.CreatedByUpn);
        unitOfWork.Commit();

        // Act
        var deleted = await unitOfWork.AttachmentRepository.DeleteAttachmentWithLink(application.ApplicationId, attachment.AttachmentId, linkId, LinkType.Question);
        unitOfWork.Commit();
        var result = await unitOfWork.AttachmentRepository.GetLinkedAttachment(application.ApplicationId, attachment.AttachmentId, linkId, LinkType.Question);

        // Assert
        Assert.True(deleted);
        Assert.Null(result);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Return_Null_When_Attachment_Link_Not_Found()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var applicationId = Guid.NewGuid();
        var attachmentId = Guid.NewGuid();
        var linkId = Guid.NewGuid();

        // Act
        var result = await unitOfWork.AttachmentRepository.GetLinkedAttachment(applicationId, attachmentId, linkId, LinkType.Question);

        // Assert
        Assert.Null(result);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Not_Link_Attachment_If_Attachment_Does_Not_Exist()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var user = await UserTestDataBuilder.CreateTestUser(unitOfWork, new User
        {
            B2CId = Guid.NewGuid(),
            EmailAddress = "test@ofqual.gov.uk",
            DisplayName = "Ofqual Test Account",
            CreatedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedDate = DateTime.UtcNow,
            ModifiedByUpn = "test@ofqual.gov.uk"
        });

        var application = await ApplicationTestDataBuilder.CreateTestApplication(unitOfWork, new Application
        {
            ApplicationId = Guid.NewGuid(),
            OwnerUserId = user.UserId,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var attachmentId = Guid.NewGuid();
        var linkId = Guid.NewGuid();

        // Act
        var success = await unitOfWork.AttachmentRepository.CreateAttachmentLink(application.ApplicationId, attachmentId, linkId, LinkType.Question, application.CreatedByUpn);

        // Assert
        Assert.False(success);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Detect_Duplicate_Attachment_By_FileName_Across_Links()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var user = await UserTestDataBuilder.CreateTestUser(unitOfWork, new User
        {
            B2CId = Guid.NewGuid(),
            EmailAddress = "test@ofqual.gov.uk",
            DisplayName = "Ofqual Test Account",
            CreatedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedDate = DateTime.UtcNow,
            ModifiedByUpn = "test@ofqual.gov.uk"
        });

        var application = await ApplicationTestDataBuilder.CreateTestApplication(unitOfWork, new Application
        {
            ApplicationId = Guid.NewGuid(),
            OwnerUserId = user.UserId,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var linkId1 = Guid.NewGuid();
        var linkId2 = Guid.NewGuid();


        var attachment = await unitOfWork.AttachmentRepository.CreateAttachment("duplicate-file.pdf", "application/pdf", 200, application.CreatedByUpn);
        await unitOfWork.AttachmentRepository.CreateAttachmentLink(application.ApplicationId, attachment!.AttachmentId, linkId1, LinkType.Question, application.CreatedByUpn);

        await unitOfWork.AttachmentRepository.CreateAttachmentLink(application.ApplicationId, attachment.AttachmentId, linkId2, LinkType.Question, application.CreatedByUpn);

        unitOfWork.Commit();

        // Act
        var isDuplicateForLink1 = await unitOfWork.AttachmentRepository.IsAttachmentInOtherCriteria(attachment.FileName, application.ApplicationId, linkId1);
        var isDuplicateForLink2 = await unitOfWork.AttachmentRepository.IsAttachmentInOtherCriteria(attachment.FileName, application.ApplicationId, linkId2);
        
        // Assert
        Assert.True(isDuplicateForLink1);
        Assert.True(isDuplicateForLink2);

        // Clean up
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Return_False_When_FileName_Is_Not_Duplicated()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var user = await UserTestDataBuilder.CreateTestUser(unitOfWork, new User
        {
            B2CId = Guid.NewGuid(),
            EmailAddress = "test@ofqual.gov.uk",
            DisplayName = "Ofqual Test Account",
            CreatedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedDate = DateTime.UtcNow,
            ModifiedByUpn = "test@ofqual.gov.uk"
        });

        var application = await ApplicationTestDataBuilder.CreateTestApplication(unitOfWork, new Application
        {
            ApplicationId = Guid.NewGuid(),
            OwnerUserId = user.UserId,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var linkId = Guid.NewGuid();
        var attachment = await unitOfWork.AttachmentRepository.CreateAttachment("unique-file.pdf", "application/pdf", 200, application.CreatedByUpn);

        await unitOfWork.AttachmentRepository.CreateAttachmentLink(application.ApplicationId, attachment!.AttachmentId, linkId, LinkType.Question, application.CreatedByUpn);
        unitOfWork.Commit();

        // Act
        var isDuplicate = await unitOfWork.AttachmentRepository.IsAttachmentInOtherCriteria(attachment.FileName, application.ApplicationId, linkId);

        // Assert
        Assert.False(isDuplicate);

        // Clean up
        await _fixture.DisposeAsync();
    }
}