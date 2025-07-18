using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services;

public class TaskService : ITaskService
{
    private readonly IUnitOfWork _context;

    public TaskService(IUnitOfWork context)
    {
        _context = context;
    }

    public async Task<TaskItemDto?> GetTaskWithStatusByUrl(string taskNameUrl)
    {
        TaskItem? task = await _context.TaskRepository.GetTaskByTaskNameUrl(taskNameUrl);
        if (task == null)
        {
            return null;
        }

        StageTaskView? stageTask = await _context.StageRepository.GetStageTaskByTaskId(task.TaskId);
        if (stageTask == null)
        {
            return null;
        }

        return new TaskItemDto
        {
            TaskId = task.TaskId,
            TaskName = task.TaskName,
            TaskNameUrl = task.TaskNameUrl,
            TaskOrderNumber = task.TaskOrderNumber,
            SectionId = task.SectionId,
            Stage = stageTask.StageId,
            ReviewFlag = task.ReviewFlag
        };
    }
}
