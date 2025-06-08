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
        var stageTasks = (await _context.StageRepository.GetAllStageTasksByStageId(stage))?.ToList();
        if (stageTasks == null || !stageTasks.Any())
        {
            return false;
        }

        var questions = (await _context.QuestionRepository.GetAllQuestions())?.ToList();
        if (questions == null || !questions.Any())
        {
            return false;
        }

        var answers = (await _context.ApplicationAnswersRepository.GetAllApplicationAnswers(applicationId))?.ToList() ?? new List<TaskQuestionAnswer>();

        var questionsByTask = questions
            .GroupBy(q => q.TaskId)
            .ToDictionary(g => g.Key, g => g.ToList());
        
        var answeredQuestionIds = answers
            .Select(a => a.QuestionId)
            .ToHashSet();
        
        var totalStageQuestions = stageTasks
            .Where(t => questionsByTask.ContainsKey(t.TaskId))
            .SelectMany(t => questionsByTask[t.TaskId])
            .ToList();
        
        int answeredCount = totalStageQuestions.Count(q => answeredQuestionIds.Contains(q.QuestionId));

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

        StageStatus? existingStatus = await _context.StageRepository.GetStageStatus(applicationId, stage);
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

        await _context.StageRepository.UpsertStageStatusRecord(stageStatus);

        return true;
    }
}