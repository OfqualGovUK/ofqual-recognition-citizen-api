using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

public interface ITaskRepository
{
    public Task<IEnumerable<ITaskItem>> GetAllTask();
    public Task<TaskItem?> GetTaskByTaskNameUrl(string taskNameUrl);
}