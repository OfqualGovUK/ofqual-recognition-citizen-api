using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

public interface IApplicationRepository
{
    public Task<Application?> CreateApplication();

    /// <summary>
    /// Retrieves all answers for a given application.
    /// </summary>
    /// <param name="applicationId"></param>
    /// <returns></returns>
    public Task<IEnumerable<TaskQuestionAnswer?>> GetAllApplicationAnswers(Guid applicationId);
}