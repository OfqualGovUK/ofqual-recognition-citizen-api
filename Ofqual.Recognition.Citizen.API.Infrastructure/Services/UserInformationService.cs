using Microsoft.AspNetCore.Http;
using System.Security.Claims;

public class UserInformationService : IUserInformationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserInformationService(IHttpContextAccessor httpContextAccessor) { 
        _httpContextAccessor = httpContextAccessor;
    }
    public string? GetCurrentUserDisplayName()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.FindFirstValue("name") ?? "Unavailable";
    }

    public string? GetCurrentUserObjectId()
    {

        var user = _httpContextAccessor.HttpContext?.User;
        return user?.FindFirstValue(ClaimTypes.NameIdentifier); // Name Identifier is the Object ID
    }

    public string? GetCurrentUserUpn()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.FindFirstValue("emails");
    }
}

