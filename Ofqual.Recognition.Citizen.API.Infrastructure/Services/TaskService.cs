using Ofqual.Recognition.Citizen.API.Core.Models.TaskStatuses;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services;

public class TaskService : ITaskService
{

    private readonly IUnitOfWork _context;

    public TaskService(IUnitOfWork context)
    {
        _context = context;
    }

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