
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories;

public interface ITaskRepository
{
    Task<IEnumerable<TaskItem>> GetAllTask();
    Task<IEnumerable<TaskWithSectionStatus>> GetTaskStatusesByApplicationId(Guid applicationId);
    Task<bool> CreateTaskStatuses(Guid applicationId, IEnumerable<TaskItem> tasks);
    Task<bool> UpdateTaskStatus(Guid applicationId, Guid taskId, TaskStatusEnum status);
}