using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;

public interface ITaskStatusService
{
    public Task<bool> UpdateTaskAndStageStatus(Guid applicationId, Guid taskId, StatusType status);
    public Task<IEnumerable<TaskItemStatusSectionDto>?> GetTaskStatusesForApplication(Guid applicationId);
    public Task<bool> DetermineAndCreateTaskStatuses(Guid applicationId);
}