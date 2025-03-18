using Ofqual.Recognition.Citizen.API.Core.Models.TaskStatuses;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services;

public interface ITaskService
{
    Task<List<TaskItemTaskStatusSectionDto>> GetSectionsWithTasksByApplicationId(Guid applicationId);
}