using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

public interface IApplicationRepository
{
    public Task<Application?> CreateApplication(Guid userId, string upn);
    public Task<Application?> GetLatestApplication(string oid);
}