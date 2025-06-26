using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Helpers;
using Ofqual.Recognition.Citizen.API.Core.Mappers;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services;

public class TaskStatusService : ITaskStatusService
{
    private readonly IUnitOfWork _context;
    private readonly IUserInformationService _userInformationService;
    private readonly IStageService _stageService;

    public TaskStatusService(IUnitOfWork context, IUserInformationService userInformationService, IStageService stageService)
    {
        _context = context;
        _userInformationService = userInformationService;
        _stageService = stageService;
    }

    public async Task<bool> UpdateTaskAndStageStatus(Guid applicationId, Guid taskId, TaskStatusEnum status, StageType stageToUpdate)
    {
        string upn = _userInformationService.GetCurrentUserUpn();

        bool taskStatusUpdated = await _context.TaskRepository.UpdateTaskStatus(applicationId, taskId, status, upn);
        if (!taskStatusUpdated)
        {
            return false;
        }

        if (status == TaskStatusEnum.Completed)
        {
            bool stageStatusUpdated = await _stageService.EvaluateAndUpsertStageStatus(applicationId, stageToUpdate);
            if (!stageStatusUpdated)
            {
                return false;
            }
        }

        return true;
    }

    public async Task<IEnumerable<TaskItemStatusSectionDto>?> GetTaskStatusesForApplication(Guid applicationId)
    {
        var taskStatuses = await _context.TaskRepository.GetTaskStatusesByApplicationId(applicationId);
        if (taskStatuses == null || !taskStatuses.Any())
        {
            return null;
        }

        var application = await _context.ApplicationRepository.GetApplicationById(applicationId);
        if (application == null)
        {
            return null;
        }

        var declarationTasks = await _context.StageRepository.GetAllStageTasksByStageId(StageType.Declaration) ?? Enumerable.Empty<StageTaskView>();
        var informationTasks = await _context.StageRepository.GetAllStageTasksByStageId(StageType.Information) ?? Enumerable.Empty<StageTaskView>();

        var declarationTaskIds = new HashSet<Guid>(declarationTasks.Select(t => t.TaskId));
        var informationTaskIds = new HashSet<Guid>(informationTasks.Select(t => t.TaskId));

        var sectionDtos = TaskMapper.ToDto(taskStatuses);

        bool isSubmitted = application.SubmittedDate.HasValue && application.SubmittedDate.Value > DateTime.UtcNow;

        foreach (var section in sectionDtos)
        {
            foreach (var task in section.Tasks)
            {
                if (declarationTaskIds.Contains(task.TaskId) && task.Status == TaskStatusEnum.CannotStartYet)
                {
                    task.Hint = isSubmitted
                        ? "Not Yet Released"
                        : "You must complete all sections first";
                }
                else if (informationTaskIds.Contains(task.TaskId))
                {
                    task.Hint = "Learn more about the application before you apply";
                }
            }
        }

        return sectionDtos;
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

        var declarationTasks = (await _context.StageRepository.GetAllStageTasksByStageId(StageType.Declaration))?.ToList();
        if (declarationTasks == null || !declarationTasks.Any())
        {
            return false;
        }

        var declarationTaskIds = declarationTasks.Select(dt => dt.TaskId).ToHashSet();

        var questionsByTask = questions
            .GroupBy(q => q.TaskId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var answeredQuestionIds = answers?
            .Where(a => !string.IsNullOrWhiteSpace(a.AnswerJson) && !JsonHelper.IsEmptyJsonObject(a.AnswerJson))
            .Select(a => a.QuestionId)
            .ToHashSet() ?? new HashSet<Guid>();

        var now = DateTime.UtcNow;
        string upn = _userInformationService.GetCurrentUserUpn();

        var newTaskStatuses = tasks.Select(task =>
        {
            TaskStatusEnum status;

            if (declarationTaskIds.Contains(task.TaskId))
            {
                status = TaskStatusEnum.CannotStartYet;
            }
            else
            {
                var taskQuestions = questionsByTask.TryGetValue(task.TaskId, out var qList)
                    ? qList
                    : new List<Question>();

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
            }

            return new TaskItemStatus
            {
                ApplicationId = applicationId,
                TaskId = task.TaskId,
                Status = status,
                CreatedByUpn = upn,
                ModifiedByUpn = upn
            };
        });

        return await _context.TaskRepository.CreateTaskStatuses(newTaskStatuses);
    }
}