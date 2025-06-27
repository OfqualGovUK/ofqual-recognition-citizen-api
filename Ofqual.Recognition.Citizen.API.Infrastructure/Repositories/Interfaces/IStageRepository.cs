using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

public interface IStageRepository
{
    public Task<StageQuestionDetails?> GetStageQuestionByTaskAndQuestionUrl(TaskStage stageId, string taskNameUrl, string questionNameUrl);
    public Task<StageQuestionDto?> GetFirstQuestionByStage(TaskStage stageId);
    public Task<StageStatusView?> GetStageStatus(Guid applicationId, TaskStage stageId);
    public Task<IEnumerable<StageTaskView>?> GetAllStageTasksByStageId(TaskStage stageId);
    public Task<bool> UpsertStageStatusRecord(StageStatus stageStatus);
}
