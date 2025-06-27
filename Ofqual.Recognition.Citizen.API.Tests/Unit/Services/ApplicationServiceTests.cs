using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Xunit;
using Moq;

namespace Ofqual.Recognition.Citizen.Tests.Unit.Services;

public class ApplicationServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IApplicationRepository> _mockApplicationRepository = new();
    private readonly Mock<IUserRepository> _mockUserRepository = new();
    private readonly Mock<IFeatureFlagService> _mockFeatureFlagService = new();
    private readonly Mock<IUserInformationService> _mockUserInformationService = new();

    private readonly ApplicationService _service;

    public ApplicationServiceTests()
    {
        _mockUnitOfWork.SetupGet(x => x.ApplicationRepository).Returns(_mockApplicationRepository.Object);
        _mockUnitOfWork.SetupGet(x => x.UserRepository).Returns(_mockUserRepository.Object);

        _service = new ApplicationService(_mockUnitOfWork.Object, _mockFeatureFlagService.Object, _mockUserInformationService.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetLatestApplicationForCurrentUser_ShouldReturnDto_WhenApplicationExists()
    {
        // Arrange
        string oid = Guid.NewGuid().ToString();
        _mockFeatureFlagService.Setup(x => x.IsFeatureEnabled("CheckUser")).Returns(true);
        _mockUserInformationService.Setup(x => x.GetCurrentUserObjectId()).Returns(oid);

        var expectedApp = new Application
        {
            ApplicationId = Guid.NewGuid(),
            OwnerUserId = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "ofqual@ofqual.gov.uk",
            ModifiedByUpn = "ofqual@ofqual.gov.uk"
        };

        _mockApplicationRepository.Setup(x => x.GetLatestApplication(oid)).ReturnsAsync(expectedApp);

        // Act
        var result = await _service.GetLatestApplicationForCurrentUser();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedApp.ApplicationId, result!.ApplicationId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetLatestApplicationForCurrentUser_ShouldReturnNull_WhenFeatureDisabled()
    {
        // Arrange
        _mockFeatureFlagService.Setup(x => x.IsFeatureEnabled("CheckUser")).Returns(false);

        // Act
        var result = await _service.GetLatestApplicationForCurrentUser();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateApplicationForCurrentUser_ShouldReturnApplication_WhenSuccessful()
    {
        // Arrange
        string oid = Guid.NewGuid().ToString();
        string displayName = "Test User";
        string upn = "test@ofqual.gov.uk";

        _mockUserInformationService.Setup(x => x.GetCurrentUserObjectId()).Returns(oid);
        _mockUserInformationService.Setup(x => x.GetCurrentUserDisplayName()).Returns(displayName);
        _mockUserInformationService.Setup(x => x.GetCurrentUserUpn()).Returns(upn);

        var user = new User
        {
            UserId = Guid.NewGuid(),
            B2CId = Guid.Parse(oid),
            EmailAddress = upn,
            DisplayName = displayName,
            CreatedByUpn = upn,
            ModifiedByUpn = upn,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        var expectedApp = new Application
        {
            ApplicationId = Guid.NewGuid(),
            OwnerUserId = user.UserId,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = upn,
            ModifiedByUpn = upn
        };

        _mockUserRepository.Setup(x => x.CreateUser(oid, displayName, upn)).ReturnsAsync(user);
        _mockApplicationRepository.Setup(x => x.CreateApplication(user.UserId, upn)).ReturnsAsync(expectedApp);

        // Act
        var result = await _service.CreateApplicationForCurrentUser();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedApp.ApplicationId, result!.ApplicationId);
        Assert.Equal(user.UserId, result.OwnerUserId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CheckUserCanModifyApplication_ShouldReturnTrue_WhenApplicationMatches()
    {
        // Arrange
        string oid = Guid.NewGuid().ToString();
        Guid appId = Guid.NewGuid();

        _mockUserInformationService.Setup(x => x.GetCurrentUserObjectId()).Returns(oid);

        var app = new Application
        {
            ApplicationId = appId,
            OwnerUserId = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "ofqual@ofqual.gov.uk",
            ModifiedByUpn = "ofqual@ofqual.gov.uk"
        };

        _mockApplicationRepository.Setup(x => x.GetLatestApplication(oid)).ReturnsAsync(app);

        // Act
        bool result = await _service.CheckUserCanModifyApplication(appId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CheckUserCanModifyApplication_ShouldReturnFalse_WhenApplicationDoesNotMatch()
    {
        // Arrange
        string oid = Guid.NewGuid().ToString();
        Guid givenAppId = Guid.NewGuid();
        Guid actualAppId = Guid.NewGuid();

        _mockUserInformationService.Setup(x => x.GetCurrentUserObjectId()).Returns(oid);

        var app = new Application
        {
            ApplicationId = actualAppId,
            OwnerUserId = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "ofqual@ofqual.gov.uk",
            ModifiedByUpn = "ofqual@ofqual.gov.uk"
        };

        _mockApplicationRepository.Setup(x => x.GetLatestApplication(oid)).ReturnsAsync(app);

        // Act
        bool result = await _service.CheckUserCanModifyApplication(givenAppId);

        // Assert
        Assert.False(result);
    }
}