using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;

public interface ITaskService
{
    Task<List<TaskItemStatusSectionDto>> GetSectionsWithTasksByApplicationId(Guid applicationId);
}