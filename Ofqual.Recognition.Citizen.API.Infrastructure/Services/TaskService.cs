using Ofqual.Recognition.Citizen.API.Core.Models.TaskStatuses;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services;

public class TaskService : ITaskService
{

    private readonly IUnitOfWork _context;

    public TaskService(IUnitOfWork context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves sections for a given application, grouping tasks within their respective sections.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the application.</param>
    /// <returns>A list of sections, each containing its ordered tasks and statuses.</returns>
    /// <remarks>
    /// Tasks are grouped by section, ensuring a structured hierarchy where
    /// sections are ordered first, followed by tasks in their respective order.
    /// </remarks>
    public async Task<List<TaskItemStatusSectionDto>> GetSectionsWithTasksByApplicationId(Guid applicationId)
    {
        IEnumerable<TaskItemStatusSection> taskStatuses = await _context.TaskRepository.GetTaskStatusesByApplicationId(applicationId);

        List<TaskItemStatusSectionDto> sections = taskStatuses
            .GroupBy(ts => new
            {
                ts.SectionId,
                ts.SectionName,
                ts.SectionOrderNumber
            })
            .OrderBy(g => g.Key.SectionOrderNumber)
            .Select(g => new TaskItemStatusSectionDto
            {
                SectionId = g.Key.SectionId,
                SectionName = g.Key.SectionName,
                SectionOrderNumber = g.Key.SectionOrderNumber,
                Tasks = g
                    .OrderBy(ts => ts.TaskOrderNumber)
                    .Select(ts => new TaskItemStatusDto
                    {
                        TaskId = ts.TaskId,
                        TaskName = ts.TaskName,
                        TaskOrderNumber = ts.TaskOrderNumber,
                        Status = ts.Status
                    })
                    .ToList()
            }).ToList();

        return sections;
    }
}