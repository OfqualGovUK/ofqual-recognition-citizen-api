using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web;
using Moq;
using Newtonsoft.Json.Linq;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Ofqual.Recognition.Citizen.Tests.Unit.Services;

public class UserInformationServiceTests
{
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IApplicationRepository> _mockApplicationRepository;
    private readonly UserInformationService _userInformationService;

    public UserInformationServiceTests()
    {
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        _mockApplicationRepository = new Mock<IApplicationRepository>();

        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUnitOfWork.Setup(u => u.ApplicationRepository).Returns(_mockApplicationRepository.Object);

        _userInformationService = new UserInformationService(_mockHttpContextAccessor.Object, _mockUnitOfWork.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public Task GetCurrentUserDisplayName__WhenClaimAvailable__ReturnDisplayName()
    {
        // Arrange

        // Set up claimsprincipal for user
        string username = "Display Name";

        List<Claim> claims = new List<Claim>()
        {
            new Claim(ClaimTypes.Name, username)
        };
        ClaimsIdentity identity = new ClaimsIdentity(claims, "TestAuthType");
        ClaimsPrincipal user = new ClaimsPrincipal(identity);

        // Set up HTTP context and plug in the user

        HttpContext context = new DefaultHttpContext();
        context.User = user;


        _mockHttpContextAccessor
            .SetupGet(accessor => accessor.HttpContext).Returns(context);

        // Act

        var value = _userInformationService.GetCurrentUserDisplayName();

        // Assert

        Assert.Equal(username, value);
        return Task.CompletedTask;
    }

    [Fact]
    [Trait("Category", "Unit")]
    public Task GetCurrentUserDisplayName__WhenClaimUnavailable__ReturnUnavailable()
    {
        // Arrange

        // Set up claimsprincipal for user

        List<Claim> claims = new List<Claim>();
        ClaimsIdentity identity = new ClaimsIdentity(claims, "TestAuthType");
        ClaimsPrincipal user = new ClaimsPrincipal(identity);

        // Set up HTTP context and plug in the user

        HttpContext context = new DefaultHttpContext();
        context.User = user;


        _mockHttpContextAccessor
            .SetupGet(accessor => accessor.HttpContext).Returns(context);

        // Act

        var value = _userInformationService.GetCurrentUserDisplayName();

        // Assert

        Assert.Equal("Unavailable", value);
        return Task.CompletedTask;
    }

    [Fact]
    [Trait("Category", "Unit")]
    public Task GetCurrentUserObjectId__WhenClaimAvailable__ReturnObjectId()
    {
        // Arrange

        // Set up claimsprincipal for user
        string objectid = Guid.NewGuid().ToString();

        List<Claim> claims = new List<Claim>()
        {
            new Claim(ClaimConstants.Oid, objectid)
        };
        ClaimsIdentity identity = new ClaimsIdentity(claims, "TestAuthType");
        ClaimsPrincipal user = new ClaimsPrincipal(identity);

        // Set up HTTP context and plug in the user

        HttpContext context = new DefaultHttpContext();
        context.User = user;


        _mockHttpContextAccessor
            .SetupGet(accessor => accessor.HttpContext).Returns(context);

        // Act

        var value = _userInformationService.GetCurrentUserObjectId();

        // Assert

        Assert.Equal(objectid, value);
        return Task.CompletedTask;
    }

    [Fact]
    [Trait("Category", "Unit")]
    public Task GetCurrentUserObjectId__WhenClaimUnavailable__ThrowInvalidOperationException()
    {
        // Arrange

        // Set up claimsprincipal for user

        List<Claim> claims = new List<Claim>();
        ClaimsIdentity identity = new ClaimsIdentity(claims, "TestAuthType");
        ClaimsPrincipal user = new ClaimsPrincipal(identity);

        // Set up HTTP context and plug in the user

        HttpContext context = new DefaultHttpContext();
        context.User = user;


        _mockHttpContextAccessor
            .SetupGet(accessor => accessor.HttpContext).Returns(context);

        // Act + Assert

        try
        {
            var value = _userInformationService.GetCurrentUserObjectId();
        } catch (InvalidOperationException ex) {
            Assert.Equal(
                "Exception raised when trying to obtain Object ID, in UserInformationService::GetCurrentUserObjectId. Exception message: No ObjectId claim found in access token",
                ex.Message
            );
            return Task.CompletedTask;
        }

        Assert.Fail();
        return Task.CompletedTask;
    }

    [Fact]
    [Trait("Category", "Unit")]
    public Task GetCurrentUserUpn__WhenClaimAvailable__ReturnUpn()
    {
        // Arrange

        // Set up claimsprincipal for user
        string email = "test@test.com";

        List<Claim> claims = new List<Claim>()
        {
            new Claim("emails", email)
        };
        ClaimsIdentity identity = new ClaimsIdentity(claims, "TestAuthType");
        ClaimsPrincipal user = new ClaimsPrincipal(identity);

        // Set up HTTP context and plug in the user

        HttpContext context = new DefaultHttpContext();
        context.User = user;


        _mockHttpContextAccessor
            .SetupGet(accessor => accessor.HttpContext).Returns(context);

        // Act

        var value = _userInformationService.GetCurrentUserUpn();

        // Assert

        Assert.Equal(email, value);
        return Task.CompletedTask;
    }

    [Fact]
    [Trait("Category", "Unit")]
    public Task GetCurrentUserUpn__WhenClaimUnavailable__ThrowInvalidOperationException()
    {
        // Arrange

        // Set up claimsprincipal for user

        List<Claim> claims = new List<Claim>();
        ClaimsIdentity identity = new ClaimsIdentity(claims, "TestAuthType");
        ClaimsPrincipal user = new ClaimsPrincipal(identity);

        // Set up HTTP context and plug in the user

        HttpContext context = new DefaultHttpContext();
        context.User = user;


        _mockHttpContextAccessor
            .SetupGet(accessor => accessor.HttpContext).Returns(context);

        // Act + Assert

        try
        {
            var value = _userInformationService.GetCurrentUserUpn();
        }
        catch (InvalidOperationException ex)
        {
            Assert.Equal(
                "Exception raised when trying to obtain upn, in UserInformationService::GetCurrentUserUpn. Exception message: No Email claim found in access token",
                ex.Message
            );
            return Task.CompletedTask;
        }

        Assert.Fail();
        return Task.CompletedTask;
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CheckUserCanModifyApplication__WhenLatestApplicationMatches__ReturnTrue()
    {
        // Arrange

        // Set up claimsprincipal for user
        string objectid = Guid.NewGuid().ToString();

        List<Claim> claims = new List<Claim>()
        {
            new Claim(ClaimConstants.Oid, objectid)
        };
        ClaimsIdentity identity = new ClaimsIdentity(claims, "TestAuthType");
        ClaimsPrincipal user = new ClaimsPrincipal(identity);

        // Set up HTTP context and plug in the user

        HttpContext context = new DefaultHttpContext();
        context.User = user;


        _mockHttpContextAccessor
            .SetupGet(accessor => accessor.HttpContext).Returns(context);

        // Set up mock for the ApplicationRepository

        Guid applicationId = Guid.NewGuid();

        Application? application = new Application() { CreatedByUpn = "test", ApplicationId = applicationId };

        _mockApplicationRepository
            .Setup(_ => _.GetLatestApplication(objectid))
            .Returns(Task.FromResult(application));

        // Act

        var value = await _userInformationService.CheckUserCanModifyApplication(applicationId.ToString());

        // Assert

        Assert.True(value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CheckUserCanModifyApplication__WhenLatestApplicationDoesntMatch__ReturnFalse()
    {
        // Arrange

        // Set up claimsprincipal for user
        string objectid = Guid.NewGuid().ToString();

        List<Claim> claims = new List<Claim>()
        {
            new Claim(ClaimConstants.Oid, objectid)
        };
        ClaimsIdentity identity = new ClaimsIdentity(claims, "TestAuthType");
        ClaimsPrincipal user = new ClaimsPrincipal(identity);

        // Set up HTTP context and plug in the user

        HttpContext context = new DefaultHttpContext();
        context.User = user;


        _mockHttpContextAccessor
            .SetupGet(accessor => accessor.HttpContext).Returns(context);

        // Set up mock for the ApplicationRepository

        Guid applicationId = Guid.NewGuid();

        Application? application = new Application() { CreatedByUpn = "test", ApplicationId = applicationId };

        _mockApplicationRepository
            .Setup(_ => _.GetLatestApplication(objectid))
            .Returns(Task.FromResult(application));

        // Act

        var value = await _userInformationService.CheckUserCanModifyApplication(Guid.NewGuid().ToString());

        // Assert

        Assert.False(value);
    }
}

