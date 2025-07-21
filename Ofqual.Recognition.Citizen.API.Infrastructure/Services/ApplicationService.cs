using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Mappers;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Serilog;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services;

public class ApplicationService : IApplicationService
{
    private readonly IUnitOfWork _context;
    private readonly IFeatureFlagService _featureFlagService;
    private readonly IUserInformationService _userInformationService;
    private readonly IGovUkNotifyService _govUkNotifyService;

    public ApplicationService(IUnitOfWork context, IFeatureFlagService featureFlagService, IUserInformationService userInformationService, IGovUkNotifyService govUkNotifyService)
    {
        _context = context;
        _featureFlagService = featureFlagService;
        _userInformationService = userInformationService;
        _govUkNotifyService = govUkNotifyService;
    }

    public async Task<ApplicationDetailsDto?> CheckAndSubmitApplication(Guid applicationId)
    {
        string upn = _userInformationService.GetCurrentUserUpn();

        Application? application = await _context.ApplicationRepository.GetApplicationById(applicationId);
        if (application == null)
        {
            return null;
        }

        ApplicationDetailsDto applicationDetailsDto = ApplicationMapper.ToDto(application);

        if (application.SubmittedDate.HasValue)
        {
            return applicationDetailsDto;
        }

        StageStatusView? preEngagementStage = await _context.StageRepository.GetStageStatus(applicationId, StageType.PreEngagement);
        StageStatusView? declarationStage = await _context.StageRepository.GetStageStatus(applicationId, StageType.Declaration);
        StageStatusView? mainApplicationStage = await _context.StageRepository.GetStageStatus(applicationId, StageType.MainApplication);

        bool allStagesCompleted =
            preEngagementStage?.StatusId == StatusType.Completed &&
            declarationStage?.StatusId == StatusType.Completed &&
            mainApplicationStage?.StatusId == StatusType.Completed;

        if (allStagesCompleted)
        {
            bool updated = await _context.ApplicationRepository.UpdateApplicationSubmittedDate(applicationId, upn);
            if (!updated)
            {
                return null;
            }

            applicationDetailsDto.Submitted = true;

            if (_featureFlagService.IsFeatureEnabled("EmailRecognition"))
            {
                try
                {
                    var contactNameList = await _context
                        .ApplicationRepository
                        .GetContactNameById(applicationDetailsDto.ApplicationId);

                    foreach (var contactName in contactNameList!.Split(';'))                    
                        if (!await _govUkNotifyService.SendEmailApplicationToRecognition(contactName))
                            Log.Warning("GovUkNotifyService::SendEmail was unable to send notification to \"{contactName}\"", contactName);
                }                
                catch (Exception ex)
                {
                    Log.Error(ex, "ApplicationService::CheckAndSubmitApplication: " +
                        "Failed to send email to recognition inbox for Application \"{ApplicationId}\"",
                            applicationDetailsDto.ApplicationId);
                }
            }
            
            await _govUkNotifyService.SendEmailApplicationSubmitted();
        }

        return applicationDetailsDto;
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

    public async Task<bool> CheckUserCanAccessApplication(Guid applicationId)
    {
        string oid = _userInformationService.GetCurrentUserObjectId();

        Application? application = await _context.ApplicationRepository.GetLatestApplication(oid);
        if (application != null)
        {
            return applicationId == application.ApplicationId;
        }

        return false;
    }

    public async Task<bool> CheckUserCanModifyApplication(Guid applicationId)
    {
        string oid = _userInformationService.GetCurrentUserObjectId();

        Application? application = await _context.ApplicationRepository.GetLatestApplication(oid);
        if (application != null)
        {
            return application.SubmittedDate == null;
        }
        return false;
    }
}