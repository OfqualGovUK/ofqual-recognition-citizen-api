using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services;

public class StageService : IStageService
{
    private readonly IUnitOfWork _context;

    public StageService(IUnitOfWork context)
    {
        _context = context;
    }

    public async Task<bool> EvaluateAndUpsertStageStatus(Guid applicationId, StageEnum stage)
    {
        // Get all tasks for the specified stage
        var stageTasks = (await _context.StageRepository.GetAllStageTasksByStageId(stage))?.ToList();

        // If no tasks are found, return false
        if (stageTasks == null || !stageTasks.Any())
        {
            return false;
        }

        // Get all questions related to the tasks
        var questions = (await _context.QuestionRepository.GetAllQuestions())?.ToList();

        // If no questions are found, return false
        if (questions == null || !questions.Any())
        {
            return false;
        }

        // Get all answers for the application. Default to an empty list if none found
        var answers = (await _context.ApplicationAnswersRepository.GetAllApplicationAnswers(applicationId))?.ToList() ?? new List<TaskQuestionAnswer>();

        // Group questions by TaskId they belong to
        var questionsByTask = questions
            .GroupBy(q => q.TaskId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Create a HashSet of answered question IDs for quick lookup
        var answeredQuestionIds = answers
            .Select(a => a.QuestionId)
            .ToHashSet();

        // Filter and flatten the questions for the current stage tasks
        var totalStageQuestions = stageTasks
            .Where(t => questionsByTask.ContainsKey(t.TaskId))
            .SelectMany(t => questionsByTask[t.TaskId])
            .ToList();

        // Count how many questions have been answered
        int answeredCount = totalStageQuestions.Count(q => answeredQuestionIds.Contains(q.QuestionId));

        // Determine the new status
        TaskStatusEnum newStatus;

        if (answeredCount == 0)
        {
            newStatus = TaskStatusEnum.NotStarted;
        }
        else if (answeredCount == totalStageQuestions.Count)
        {
            newStatus = TaskStatusEnum.Completed;
        }
        else
        {
            newStatus = TaskStatusEnum.InProgress;
        }

        // Check the existing stage status for the application
        StageStatus? existingStatus = await _context.StageRepository.GetStageStatus(applicationId, stage);

        // If the status hasn't changed it skips the update
        if (existingStatus != null && existingStatus.StatusId == newStatus)
        {
            return true;
        }

        var now = DateTime.UtcNow;
        var user = "USER";

        var stageStatus = new StageStatus
        {
            ApplicationId = applicationId,
            StageId = stage,
            StatusId = newStatus,
            StageStartDate = existingStatus?.StageStartDate ?? now,
            StageCompletionDate = newStatus == TaskStatusEnum.Completed ? now : null,
            CreatedByUpn = user,
            ModifiedByUpn = user
        };

        // If the stage status already exists, update it; otherwise, insert a new record
        await _context.StageRepository.UpsertStageStatusRecord(stageStatus);

        return true;
    }
}