using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

public interface ITaskStatusRepository
{
    public Task<IEnumerable<TaskItemStatusSection>?> GetTaskStatusesByApplicationId(Guid applicationId);
    public Task<bool> CreateTaskStatuses(IEnumerable<TaskItemStatus> statuses);
    public Task<bool> UpdateTaskStatus(Guid applicationId, Guid taskId, StatusType status, string upn);
}