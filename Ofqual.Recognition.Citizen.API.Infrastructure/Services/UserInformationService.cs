using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using System.Security.Claims;

public class UserInformationService : IUserInformationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUnitOfWork _context;

    public UserInformationService(IHttpContextAccessor httpContextAccessor, IUnitOfWork context) { 
        _httpContextAccessor = httpContextAccessor;
        _context = context;
    }

    public string GetCurrentUserDisplayName()
    {
        return _httpContextAccessor.HttpContext?.User?.GetDisplayName() ?? "Unavailable"; // legacy users may not have display names;
    }

    public string GetCurrentUserObjectId()
    {
        string? oid = _httpContextAccessor.HttpContext?.User.GetObjectId(); // Name Identifier is the Object ID
        if (oid == null) {
            throw new InvalidOperationException("Exception raised when trying to obtain Object ID, in UserInformationService::GetCurrentUserObjectId. Exception message: No ObjectId claim found in access token");
        }
        return oid; 
    }

    public string GetCurrentUserUpn()
    {
        ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
        string? email = user?.FindFirstValue("emails");
        if (email == null) {
            throw new InvalidOperationException("Exception raised when trying to obtain upn, in UserInformationService::GetCurrentUserUpn. Exception message: No Email claim found in access token");
        }
        return email;
    }

    public async Task<bool> CheckUserCanModifyApplication(string applicationId)
    {
        return await CheckUserCanModifyApplication(Guid.Parse(applicationId));
    }

    public async Task<bool> CheckUserCanModifyApplication(Guid applicationId)
    {
        var oid = GetCurrentUserObjectId();
        Application? application = await _context.ApplicationRepository.GetLatestApplication(oid);
        if (application != null)
        {
            return applicationId == application.ApplicationId;
        }
        return false;
    }
}

