using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Mappers;
using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services;

public class ApplicationService : IApplicationService
{
    private readonly IUnitOfWork _context;
    private readonly IFeatureFlagService _featureFlagService;
    private readonly IUserInformationService _userInformationService;

    public ApplicationService(IUnitOfWork context, IFeatureFlagService featureFlagService, IUserInformationService userInformationService)
    {
        _context = context;
        _featureFlagService = featureFlagService;
        _userInformationService = userInformationService;
    }

    public async Task<ApplicationDetailsDto?> GetLatestApplicationForCurrentUser()
    {
        if (!_featureFlagService.IsFeatureEnabled("CheckUser"))
        {
            return null;
        }

        string oid = _userInformationService.GetCurrentUserObjectId();

        Application? application = await _context.ApplicationRepository.GetLatestApplication(oid);
        if (application == null)
        {
            return null;
        }

        return ApplicationMapper.ToDto(application);
    }

    public async Task<Application?> CreateApplicationForCurrentUser()
    {
        string oid = _userInformationService.GetCurrentUserObjectId();
        string displayName = _userInformationService.GetCurrentUserDisplayName();
        string upn = _userInformationService.GetCurrentUserUpn();

        User? user = await _context.UserRepository.CreateUser(oid, displayName, upn);
        if (user == null)
        {
            return null;
        }

        Application? application = await _context.ApplicationRepository.CreateApplication(user.UserId, upn);
        if (application == null)
        {
            return null;
        }

        return application;
    }

    public async Task<bool> CheckUserCanModifyApplication(Guid applicationId)
    {
        string oid = _userInformationService.GetCurrentUserObjectId();

        Application? application = await _context.ApplicationRepository.GetLatestApplication(oid);
        if (application != null)
        {
            return applicationId == application.ApplicationId;
        }
        
        return false;
    }
}