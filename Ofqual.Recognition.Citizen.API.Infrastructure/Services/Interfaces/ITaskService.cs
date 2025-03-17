
using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services;

public interface ITaskService
{
    Task<List<TaskSectionDto>> GetSectionsWithTasksByApplicationId(Guid applicationId);
}