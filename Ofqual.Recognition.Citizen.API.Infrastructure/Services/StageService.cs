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
        var stageTasks = (await _context.StageRepository.GetAllStageTasksByStageId(stage))?.ToList();
        if (stageTasks == null || !stageTasks.Any())
        {
            return false;
        }

        var allTaskStatuses = (await _context.TaskRepository.GetTaskStatusesByApplicationId(applicationId))?.ToList();
        if (allTaskStatuses == null || !allTaskStatuses.Any())
        {
            return false;
        }

        var stageTaskIds = stageTasks.Select(t => t.TaskId).ToHashSet();
        var relevantTaskStatuses = allTaskStatuses
            .Where(ts => stageTaskIds.Contains(ts.TaskId))
            .ToList();

        if (!relevantTaskStatuses.Any())
        {
            return false;
        }

        StatusType newStatus;

        if (relevantTaskStatuses.Any(ts => ts.Status == StatusType.InProgress))
        {
            newStatus = StatusType.InProgress;
        }
        else if (relevantTaskStatuses.All(ts => ts.Status == StatusType.NotStarted))
        {
            newStatus = StatusType.NotStarted;
        }
        else if (relevantTaskStatuses.All(ts => ts.Status == StatusType.Completed))
        {
            newStatus = StatusType.Completed;
        }
        else if (relevantTaskStatuses.All(ts => ts.Status == StatusType.CannotStartYet))
        {
            newStatus = StatusType.CannotStartYet;
        }
        else
        {
            newStatus = StatusType.InProgress;
        }

        StageStatusView? existingStatus = await _context.StageRepository.GetStageStatus(applicationId, stage);
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

        await _context.StageRepository.UpsertStageStatusRecord(stageStatus);

        return true;
    }
}