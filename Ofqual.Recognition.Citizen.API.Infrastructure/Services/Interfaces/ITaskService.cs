using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;

public interface ITaskService
{
    public Task<TaskItemDto?> GetTaskWithStatusByUrl(string taskNameUrl);
}