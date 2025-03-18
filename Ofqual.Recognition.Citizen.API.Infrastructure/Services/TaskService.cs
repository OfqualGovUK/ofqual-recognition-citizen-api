using Ofqual.Recognition.Citizen.API.Core.Models.TaskStatuses;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services;

public class TaskService : ITaskService
{

    private readonly IUnitOfWork _context;

    public TaskService(IUnitOfWork context)
    {
        _context = context;
    }

    public async Task<List<TaskItemTaskStatusSectionDto>> GetSectionsWithTasksByApplicationId(Guid applicationId)
    {
        IEnumerable<TaskItemTaskStatusSection> taskStatuses = await _context.TaskRepository.GetTaskStatusesByApplicationId(applicationId);

        List<TaskItemTaskStatusSectionDto> sections = taskStatuses
            .GroupBy(ts => new
            {
                ts.SectionId,
                ts.SectionName,
                ts.SectionOrderNumber
            })
            .OrderBy(g => g.Key.SectionOrderNumber)
            .Select(g => new TaskItemTaskStatusSectionDto
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