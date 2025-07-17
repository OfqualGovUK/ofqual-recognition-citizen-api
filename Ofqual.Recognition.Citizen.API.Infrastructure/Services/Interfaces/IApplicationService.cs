using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;

public interface IApplicationService
{
    public Task<ApplicationDetailsDto?> CheckAndSubmitApplication(Guid applicationId);
    public Task<ApplicationDetailsDto?> GetLatestApplicationForCurrentUser();
    public Task<Application?> CreateApplicationForCurrentUser();
    public Task<bool> CheckUserCanAccessApplication(Guid applicationId);
    public Task<bool> CheckUserCanModifyApplication(Guid applicationId);
}