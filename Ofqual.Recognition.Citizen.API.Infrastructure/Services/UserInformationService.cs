using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.Identity.Web;

public class UserInformationService : IUserInformationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserInformationService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetCurrentUserDisplayName()
    {
        return _httpContextAccessor.HttpContext?.User?.GetDisplayName() ?? "Unavailable"; // legacy users may not have display names;
    }

    public string GetCurrentUserObjectId()
    {
        string? oid = _httpContextAccessor.HttpContext?.User.GetObjectId(); // Name Identifier is the Object ID
        if (oid == null)
        {
            throw new InvalidOperationException("Exception raised when trying to obtain Object ID, in UserInformationService::GetCurrentUserObjectId. Exception message: No ObjectId claim found in access token");
        }
        return oid;
    }

    public string GetCurrentUserUpn()
    {
        ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
        string? email = user?.FindFirstValue("emails");
        if (email == null)
        {
            throw new InvalidOperationException("Exception raised when trying to obtain upn, in UserInformationService::GetCurrentUserUpn. Exception message: No Email claim found in access token");
        }
        return email;
    }
}