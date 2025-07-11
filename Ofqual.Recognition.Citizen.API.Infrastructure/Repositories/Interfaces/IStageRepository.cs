﻿using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

public interface IStageRepository
{
    public Task<StageQuestionDetails?> GetStageQuestionByTaskAndQuestionUrl(StageType stageId, string taskNameUrl, string questionNameUrl);
    public Task<StageQuestionDto?> GetFirstQuestionByStage(StageType stageId);
    public Task<StageStatusView?> GetStageStatus(Guid applicationId, StageType stageId);
    public Task<IEnumerable<StageTaskView>?> GetAllStageTasksByStageId(StageType stageId);
    public Task<StageTaskView?> GetStageTaskByTaskId(Guid taskId);
    public Task<bool> UpsertStageStatusRecord(StageStatus stageStatus);
}
