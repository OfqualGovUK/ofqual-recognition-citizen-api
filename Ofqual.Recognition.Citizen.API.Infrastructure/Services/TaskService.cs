using Ofqual.Recognition.Citizen.API.Core.Mappers;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services;

public class TaskService : ITaskService
{

    private readonly IUnitOfWork _context;

    public TaskService(IUnitOfWork context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves sections for a given application, including their tasks and statuses.
    /// Sections are ordered first, followed by tasks within each section.
    /// </summary>
    /// <param name="applicationId">The application ID.</param>
    /// <returns>A list of sections, each containing its tasks in the correct order.</returns>
    public async Task<List<TaskItemStatusSectionDto>> GetSectionsWithTasksByApplicationId(Guid applicationId)
    {
        var taskStatuses = await _context.TaskRepository.GetTaskStatusesByApplicationId(applicationId);
        return TaskMapper.MapToSectionsWithTasks(taskStatuses);
    }
}