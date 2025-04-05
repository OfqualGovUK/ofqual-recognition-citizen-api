using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

public interface IApplicationRepository
{
    Task<Application> CreateApplication();
    Task<bool> InsertApplicationAnswer(Guid applicationId, Guid questionId, string answer);
}