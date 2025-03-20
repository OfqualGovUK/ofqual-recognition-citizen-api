
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

public interface ITaskRepository
{
    Task<IEnumerable<ITaskItem>> GetAllTask();
    Task<IEnumerable<TaskItemStatusSection>> GetTaskStatusesByApplicationId(Guid applicationId);
    Task<bool> CreateTaskStatuses(Guid applicationId, IEnumerable<ITaskItem> tasks);
    Task<bool> UpdateTaskStatus(Guid applicationId, Guid taskId, TaskStatusEnum status);
}