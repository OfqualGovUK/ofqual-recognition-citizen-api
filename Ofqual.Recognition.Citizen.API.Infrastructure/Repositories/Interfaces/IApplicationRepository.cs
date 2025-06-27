using Ofqual.Recognition.Citizen.API.Core.Enums;
using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

public interface IApplicationRepository
{
    public Task<Application?> CreateApplication(string oid, string displayName, string upn);
    public Task<Application?> GetLatestApplication(string oid);

    public Task<ApplicationStatus?> CheckAndCompleteApplication(Guid applicationId, string upn);

    public Task<bool?> IsApplicationSubmitted(Guid applicationId);

}