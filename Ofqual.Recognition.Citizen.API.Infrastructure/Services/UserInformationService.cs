using Microsoft.AspNetCore.Http;
using System.Security.Claims;

public class UserInformationService : IUserInformationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserInformationService(IHttpContextAccessor httpContextAccessor) { 
        _httpContextAccessor = httpContextAccessor;
    }
    public string GetCurrentUserDisplayName()
    {
        ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
        return user?.FindFirstValue("name") ?? "Unavailable"; // legacy users may not have display names;
    }

    public string GetCurrentUserObjectId()
    {

        ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
        string? oid = user?.FindFirstValue(ClaimTypes.NameIdentifier); // Name Identifier is the Object ID
        if (oid == null) {
            throw new ArgumentNullException("Exception raised when trying to obtain Object ID, in UserInformationService::GetCurrentUserObjectId. Exception message: No ObjectId claim found in access token");
        }
        return oid; 
    }

    public string GetCurrentUserUpn()
    {
        ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
        string? email = user?.FindFirstValue("emails");
        if (email == null) {
            throw new ArgumentNullException("Exception raised when trying to obtain upn, in UserInformationService::GetCurrentUserUpn. Exception message: No Email claim found in access token");
        }
        return email;
    }
}

