
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Ofqual.Recognition.Citizen.API.Core.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Models.TaskStatuses;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories;

public interface ITaskRepository
{
    Task<IEnumerable<ITaskItem>> GetAllTask();
    Task<IEnumerable<TaskItemStatusSection>> GetTaskStatusesByApplicationId(Guid applicationId);
    Task<bool> CreateTaskStatuses(Guid applicationId, IEnumerable<TaskItem> tasks);
    Task<bool> UpdateTaskStatus(Guid applicationId, Guid taskId, TaskStatusEnum status);
}