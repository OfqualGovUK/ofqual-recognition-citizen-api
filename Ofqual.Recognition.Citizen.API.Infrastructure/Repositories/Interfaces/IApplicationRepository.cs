using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

public interface IApplicationRepository
{
    public Task<Application?> CreateApplication(Guid userId, string upn);
    public Task<bool> UpdateApplicationSubmittedDate(Guid applicationId, string modifiedByUpn);
    public Task<Application?> GetLatestApplication(string oid);
    public Task<Application?> GetApplicationById(Guid applicationId);
    public Task<string?> GetContactNameById(Guid applicationId);
}