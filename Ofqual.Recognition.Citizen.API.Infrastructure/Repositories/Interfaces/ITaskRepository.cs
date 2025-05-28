using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

public interface ITaskRepository
{
    public Task<IEnumerable<ITaskItem>> GetAllTask();
    public Task<TaskItem?> GetTaskByTaskNameUrl(string taskNameUrl);
    public Task<IEnumerable<TaskItemStatusSection>> GetTaskStatusesByApplicationId(Guid applicationId);
    public Task<bool> CreateTaskStatuses(Guid applicationId, IEnumerable<ITaskItem> tasks);
    public Task<bool> UpdateTaskStatus(Guid applicationId, Guid taskId, TaskStatusEnum status);
}