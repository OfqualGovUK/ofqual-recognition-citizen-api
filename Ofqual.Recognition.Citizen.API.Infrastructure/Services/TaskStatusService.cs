using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Helpers;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services;

public class TaskStatusService : ITaskStatusService
{
    private readonly IUnitOfWork _context;

    public TaskStatusService(IUnitOfWork context)
    {
        _context = context;
    }

    public async Task<bool> DetermineAndCreateTaskStatuses(Guid applicationId, IEnumerable<PreEngagementAnswerDto> answers)
    {
        var tasks = await _context.TaskRepository.GetAllTask();

        if (tasks == null || !tasks.Any())
        {
            return false;
        }

        var statuses = tasks.Select(task =>
        {
            var answer = answers.FirstOrDefault(a => a.TaskId == task.TaskId);
            var isComplete = answer != null && !string.IsNullOrWhiteSpace(answer.AnswerJson) && !JsonHelper.IsEmptyJsonObject(answer.AnswerJson);
            
            return new TaskItemStatus
            {
                ApplicationId = applicationId,
                TaskId = task.TaskId,
                Status = isComplete ? TaskStatusEnum.Completed : TaskStatusEnum.NotStarted,
                CreatedByUpn = "USER",
                ModifiedByUpn = "USER"
            };
        });

        return await _context.TaskRepository.CreateTaskStatuses(statuses);
    }
}