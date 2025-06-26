using Ofqual.Recognition.Citizen.Tests.Integration.Builders;
using Ofqual.Recognition.Citizen.Tests.Integration.Fixtures;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Xunit;

namespace Ofqual.Recognition.Citizen.Tests.Integration.Repositories;

public class ApplicationRepositoryTests : IClassFixture<SqlTestFixture>
{
    private readonly SqlTestFixture _fixture;

    public ApplicationRepositoryTests(SqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateApplication_Should_Insert_And_Return_Application()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var user = await UserTestDataBuilder.CreateTestUser(unitOfWork, new User
        {
            UserId = Guid.NewGuid(),
            B2CId = Guid.NewGuid(),
            EmailAddress = "test@ofqual.gov.uk",
            DisplayName = "Test User",
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        // Act
        var result = await unitOfWork.ApplicationRepository.CreateApplication(user.UserId, user.CreatedByUpn);
        unitOfWork.Commit();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.UserId, result!.OwnerUserId);
        Assert.Equal(user.CreatedByUpn, result.CreatedByUpn);
        Assert.Equal(user.CreatedByUpn, result.ModifiedByUpn);
        Assert.True(result.CreatedDate <= DateTime.UtcNow);
        Assert.True(result.ModifiedDate <= DateTime.UtcNow);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetLatestApplication_Should_Return_Application_For_User()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var user = await UserTestDataBuilder.CreateTestUser(unitOfWork, new User
        {
            UserId = Guid.NewGuid(),
            B2CId = Guid.NewGuid(),
            EmailAddress = "test@ofqual.gov.uk",
            DisplayName = "Test User",
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var app = await ApplicationTestDataBuilder.CreateTestApplication(unitOfWork, new Application
        {
            ApplicationId = Guid.NewGuid(),
            OwnerUserId = user.UserId,
            CreatedByUpn = user.CreatedByUpn,
            ModifiedByUpn = user.ModifiedByUpn,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        unitOfWork.Commit();

        // Act
        var result = await unitOfWork.ApplicationRepository.GetLatestApplication(user.B2CId.ToString());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(app.ApplicationId, result!.ApplicationId);
        Assert.Equal(app.OwnerUserId, result.OwnerUserId);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetApplicationById_Should_Return_Application()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var user = await UserTestDataBuilder.CreateTestUser(unitOfWork, new User
        {
            UserId = Guid.NewGuid(),
            B2CId = Guid.NewGuid(),
            EmailAddress = "test@ofqual.gov.uk",
            DisplayName = "Test User",
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var app = await ApplicationTestDataBuilder.CreateTestApplication(unitOfWork, new Application
        {
            ApplicationId = Guid.NewGuid(),
            OwnerUserId = user.UserId,
            SubmittedDate = DateTime.UtcNow,
            ApplicationReleaseDate = DateTime.UtcNow.AddDays(5),
            OrganisationId = Guid.NewGuid(),
            CreatedByUpn = user.CreatedByUpn,
            ModifiedByUpn = user.ModifiedByUpn,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        unitOfWork.Commit();

        // Act
        var result = await unitOfWork.ApplicationRepository.GetApplicationById(app.ApplicationId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(app.ApplicationId, result!.ApplicationId);
        Assert.Equal(app.OwnerUserId, result.OwnerUserId);
        Assert.Equal(app.SubmittedDate, result.SubmittedDate);
        Assert.Equal(app.ApplicationReleaseDate, result.ApplicationReleaseDate);
        Assert.Equal(app.OrganisationId, result.OrganisationId);
        Assert.Equal(app.CreatedByUpn, result.CreatedByUpn);
        Assert.Equal(app.ModifiedByUpn, result.ModifiedByUpn);

        // Clean up test container
        await _fixture.DisposeAsync();
    }
}