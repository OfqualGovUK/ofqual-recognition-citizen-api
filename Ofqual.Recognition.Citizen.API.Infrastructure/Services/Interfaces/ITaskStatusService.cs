using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;

public interface ITaskStatusService
{
    public Task<bool> DetermineAndCreateTaskStatuses(Guid applicationId, IEnumerable<PreEngagementAnswerDto> answers);
}