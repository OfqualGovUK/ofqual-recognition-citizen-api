using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

public interface IApplicationRepository
{
    public Task<Application> CreateApplication();
    public Task<bool> InsertApplicationAnswer(Guid applicationId, Guid questionId, string answer);
}