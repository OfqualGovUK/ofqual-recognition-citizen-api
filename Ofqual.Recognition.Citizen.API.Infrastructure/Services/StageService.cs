using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services;

public class StageService : IStageService
{
    private readonly IUnitOfWork _context;
    private readonly IUserInformationService _userInformationService;

    public StageService(IUnitOfWork context, IUserInformationService userInformationService)
    {
        _context = context;
        _userInformationService = userInformationService;
    }

    public async Task<bool> EvaluateAndUpsertAllStageStatus(Guid applicationId)
    {
        bool preEngagementUpdated = await EvaluateAndUpsertStageStatus(applicationId, StageType.PreEngagement);
        if (!preEngagementUpdated)
        {
            return false;
        }

        bool mainApplicationUpdated = await EvaluateAndUpsertStageStatus(applicationId, StageType.MainApplication);
        if (!mainApplicationUpdated)
        {
            return false;
        }

        bool declarationUpdated = await EvaluateAndUpsertStageStatus(applicationId, StageType.Declaration);
        if (!declarationUpdated)
        {
            return false;
        }

        return true;
    }

    public async Task<bool> EvaluateAndUpsertStageStatus(Guid applicationId, StageType stage)
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
        var answers = (await _context.ApplicationAnswersRepository.GetAllApplicationAnswers(applicationId))?.ToList() ?? new List<SectionTaskQuestionAnswer>();

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
        StatusType newStatus;

        if (answeredCount == 0)
        {
            newStatus = StatusType.NotStarted;
        }
        else if (answeredCount == totalStageQuestions.Count)
        {
            newStatus = StatusType.Completed;
        }
        else
        {
            newStatus = StatusType.InProgress;
        }

        // Check the existing stage status for the application
        StageStatusView? existingStatus = await _context.StageRepository.GetStageStatus(applicationId, stage);

        // If the status hasn't changed it skips the update
        if (existingStatus != null && existingStatus.StatusId == newStatus)
        {
            return true;
        }

        var now = DateTime.UtcNow;
        string upn = _userInformationService.GetCurrentUserUpn();

        var stageStatus = new StageStatus
        {
            ApplicationId = applicationId,
            StageId = stage,
            StatusId = newStatus,
            StageStartDate = existingStatus?.StageStartDate ?? now,
            StageCompletionDate = newStatus == StatusType.Completed ? now : null,
            CreatedByUpn = upn,
            ModifiedByUpn = upn
        };

        // If the stage status already exists, update it; otherwise, insert a new record
        await _context.StageRepository.UpsertStageStatusRecord(stageStatus);

        return true;
    }
}