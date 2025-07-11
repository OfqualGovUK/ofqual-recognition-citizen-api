﻿using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web;
using System.Security.Claims;
using Xunit;
using Moq;

namespace Ofqual.Recognition.Citizen.Tests.Unit.Services;

public class UserInformationServiceTests
{
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor = new();
    private readonly UserInformationService _userInformationService;

    public UserInformationServiceTests()
    {
        _userInformationService = new UserInformationService(_mockHttpContextAccessor.Object);
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
        }
        catch (InvalidOperationException ex)
        {
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
}