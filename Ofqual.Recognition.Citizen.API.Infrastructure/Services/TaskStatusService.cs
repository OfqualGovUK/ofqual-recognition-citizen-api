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

    public async Task<bool> DetermineAndCreateTaskStatuses(Guid applicationId, IEnumerable<PreEngagementAnswerDto>? answers)
    {
        var tasks = (await _context.TaskRepository.GetAllTask())?.ToList();
        if (tasks == null || !tasks.Any())
        {
            return false;
        }

        var questions = (await _context.QuestionRepository.GetAllQuestions())?.ToList();
        if (questions == null || !questions.Any())
        {
            return false;
        }

        var questionsByTask = questions
            .GroupBy(q => q.TaskId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var answeredQuestionIds = answers?
            .Where(a => !string.IsNullOrWhiteSpace(a.AnswerJson) && !JsonHelper.IsEmptyJsonObject(a.AnswerJson))
            .Select(a => a.QuestionId)
            .ToHashSet() ?? new HashSet<Guid>();

        var statuses = tasks.Select(task =>
        {
            var taskQuestions = questionsByTask.TryGetValue(task.TaskId, out var qList) ? qList : new List<Question>();

            TaskStatusEnum status;

            if (taskQuestions.Count == 0)
            {
                status = TaskStatusEnum.NotStarted;
            }
            else
            {
                var answeredCount = taskQuestions.Count(q => answeredQuestionIds.Contains(q.QuestionId));
                if (answeredCount == 0)
                {
                    status = TaskStatusEnum.NotStarted;
                }
                else if (answeredCount == taskQuestions.Count)
                {
                    status = TaskStatusEnum.Completed;
                }
                else
                {
                    status = TaskStatusEnum.InProgress;
                }
            }

            return new TaskItemStatus
            {
                ApplicationId = applicationId,
                TaskId = task.TaskId,
                Status = status,
                CreatedByUpn = "USER",
                ModifiedByUpn = "USER"
            };
        });

        return await _context.TaskRepository.CreateTaskStatuses(statuses);
    }
}