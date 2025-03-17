
using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services;

public class TaskService : ITaskService
{

    private readonly IUnitOfWork _context;

    public TaskService(IUnitOfWork context)
    {
        _context = context;
    }

    public async Task<List<TaskSectionDto>> GetSectionsWithTasksByApplicationId(Guid applicationId)
    {
        var taskStatuses = await _context.TaskRepository.GetTaskStatusesByApplicationId(applicationId);
        
        var sections = taskStatuses
            .GroupBy(ts => new
            {
                ts.SectionId,
                ts.SectionName,
                ts.SectionOrderNumber
            })
            .OrderBy(g => g.Key.SectionOrderNumber)
            .Select(g => new TaskSectionDto
            {
                SectionId = g.Key.SectionId,
                SectionName = g.Key.SectionName,
                OrderNumber = g.Key.SectionOrderNumber,
                Tasks = g
                    .OrderBy(ts => ts.TaskOrderNumber)
                    .Select(ts => new TaskStatusDto
                    {
                        TaskId = ts.TaskId,
                        TaskName = ts.TaskName,
                        OrderNumber = ts.TaskOrderNumber,
                        Status = ts.Status
                    })
                    .ToList()
            }).ToList();
        return sections;
    }
}