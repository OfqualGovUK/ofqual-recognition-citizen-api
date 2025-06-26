using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;

public interface ITaskStatusService
{
    public Task<bool> UpdateTaskAndStageStatus(Guid applicationId, Guid taskId, TaskStatusEnum status, Stage stageToUpdate);
    public Task<bool> DetermineAndCreateTaskStatuses(Guid applicationId, IEnumerable<PreEngagementAnswerDto>? answers);
}