using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Xunit;
using Moq;

namespace Ofqual.Recognition.Citizen.Tests.Unit.Services;

public class ApplicationServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IApplicationRepository> _mockApplicationRepository = new();
    private readonly Mock<IUserRepository> _mockUserRepository = new();
    private readonly Mock<IStageRepository> _mockStageRepository = new();
    private readonly Mock<IFeatureFlagService> _mockFeatureFlagService = new();
    private readonly Mock<IUserInformationService> _mockUserInformationService = new();
    private readonly Mock<IGovUkNotifyService> _mockGovUkNotifyService = new();
    private readonly ApplicationService _service;

    public ApplicationServiceTests()
    {
        _mockUnitOfWork.SetupGet(x => x.ApplicationRepository).Returns(_mockApplicationRepository.Object);
        _mockUnitOfWork.SetupGet(x => x.UserRepository).Returns(_mockUserRepository.Object);
        _mockUnitOfWork.SetupGet(x => x.StageRepository).Returns(_mockStageRepository.Object);

        _service = new ApplicationService(_mockUnitOfWork.Object, _mockFeatureFlagService.Object, _mockUserInformationService.Object, _mockGovUkNotifyService.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CheckAndSubmitApplication_ReturnsNull_WhenApplicationNotFound()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        _mockApplicationRepository
            .Setup(r => r.GetApplicationById(applicationId))
            .ReturnsAsync((Application?)null);

        // Act
        var result = await _service.CheckAndSubmitApplication(applicationId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CheckAndSubmitApplication_ReturnsDto_WhenAlreadySubmitted()
    {
        // Arrange
        var applicationId = Guid.NewGuid();

        var application = new Application
        {
            ApplicationId = applicationId,
            OwnerUserId = Guid.NewGuid(),
            SubmittedDate = DateTime.UtcNow,
            CreatedByUpn = "user@ofqual.gov.uk",
            ModifiedByUpn = "user@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _mockApplicationRepository
            .Setup(r => r.GetApplicationById(applicationId))
            .ReturnsAsync(application);

        // Act
        var result = await _service.CheckAndSubmitApplication(applicationId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result!.Submitted);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CheckAndSubmitApplication_ReturnsDto_WithSubmittedFalse_WhenStagesNotCompleted()
    {
        // Arrange
        var applicationId = Guid.NewGuid();

        var application = new Application
        {
            ApplicationId = applicationId,
            OwnerUserId = Guid.NewGuid(),
            SubmittedDate = null,
            CreatedByUpn = "user@ofqual.gov.uk",
            ModifiedByUpn = "user@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _mockApplicationRepository
            .Setup(r => r.GetApplicationById(applicationId))
            .ReturnsAsync(application);

        _mockStageRepository
            .Setup(r => r.GetStageStatus(applicationId, StageType.Declaration))
            .ReturnsAsync(new StageStatusView
            {
                ApplicationId = applicationId,
                StageId = StageType.Declaration,
                StageName = "Declaration Stage",
                Status = "In Progress",
                StatusId = StatusType.InProgress,
                StageStartDate = DateTime.UtcNow
            });

        _mockStageRepository
            .Setup(r => r.GetStageStatus(applicationId, StageType.MainApplication))
            .ReturnsAsync(new StageStatusView
            {
                ApplicationId = applicationId,
                StageId = StageType.MainApplication,
                StageName = "Main Application",
                Status = "Not Started",
                StatusId = StatusType.NotStarted,
                StageStartDate = DateTime.UtcNow
            });

        // Act
        var result = await _service.CheckAndSubmitApplication(applicationId);

        // Assert
        Assert.NotNull(result);
        Assert.False(result!.Submitted);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CheckAndSubmitApplication_MarksSubmitted_WhenStagesCompleted()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var upn = "user@ofqual.gov.uk";

        var application = new Application
        {
            ApplicationId = applicationId,
            OwnerUserId = Guid.NewGuid(),
            SubmittedDate = null,
            CreatedByUpn = upn,
            ModifiedByUpn = upn,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _mockUserInformationService
            .Setup(s => s.GetCurrentUserUpn())
            .Returns(upn);

        _mockApplicationRepository
            .Setup(r => r.GetApplicationById(applicationId))
            .ReturnsAsync(application);

        _mockStageRepository
            .Setup(r => r.GetStageStatus(applicationId, StageType.PreEngagement))
            .ReturnsAsync(new StageStatusView
            {
                ApplicationId = applicationId,
                StageId = StageType.PreEngagement,
                StageName = "Pre-Engagement Stage",
                Status = "Completed",
                StatusId = StatusType.Completed,
                StageStartDate = DateTime.UtcNow,
                StageCompletionDate = DateTime.UtcNow
            });

        _mockStageRepository
            .Setup(r => r.GetStageStatus(applicationId, StageType.Declaration))
            .ReturnsAsync(new StageStatusView
            {
                ApplicationId = applicationId,
                StageId = StageType.Declaration,
                StageName = "Declaration Stage",
                Status = "Completed",
                StatusId = StatusType.Completed,
                StageStartDate = DateTime.UtcNow,
                StageCompletionDate = DateTime.UtcNow
            });

        _mockStageRepository
            .Setup(r => r.GetStageStatus(applicationId, StageType.MainApplication))
            .ReturnsAsync(new StageStatusView
            {
                ApplicationId = applicationId,
                StageId = StageType.MainApplication,
                StageName = "Main Application",
                Status = "Completed",
                StatusId = StatusType.Completed,
                StageStartDate = DateTime.UtcNow,
                StageCompletionDate = DateTime.UtcNow
            });

        _mockApplicationRepository
            .Setup(r => r.UpdateApplicationSubmittedDate(applicationId, upn))
            .ReturnsAsync(true);

        _mockGovUkNotifyService
            .Setup(s => s.SendEmailApplicationSubmitted())
            .ReturnsAsync(true);

        // Act
        var result = await _service.CheckAndSubmitApplication(applicationId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result!.Submitted);
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
    public async Task CheckUserCanAccessApplication_ShouldReturnTrue_WhenApplicationMatches()
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
        bool result = await _service.CheckUserCanAccessApplication(appId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CheckUserCanAccessApplication_ShouldReturnFalse_WhenApplicationDoesNotMatch()
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
        bool result = await _service.CheckUserCanAccessApplication(givenAppId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CheckUserCanModifyApplication_ShouldReturnTrue_WhenApplicationSubmittedDateIsNotPresent()
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
            ModifiedByUpn = "ofqual@ofqual.gov.uk",
        };

        _mockApplicationRepository.Setup(x => x.GetLatestApplication(oid)).ReturnsAsync(app);

        // Act
        bool result = await _service.CheckUserCanModifyApplication(givenAppId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CheckUserCanModifyApplication_ShouldReturnFalse_WhenApplicationContainsSubmittedDate()
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
            ModifiedByUpn = "ofqual@ofqual.gov.uk",
            SubmittedDate = DateTime.UtcNow,
        };

        _mockApplicationRepository.Setup(x => x.GetLatestApplication(oid)).ReturnsAsync(app);

        // Act
        bool result = await _service.CheckUserCanModifyApplication(givenAppId);

        // Assert
        Assert.False(result);
    }
}