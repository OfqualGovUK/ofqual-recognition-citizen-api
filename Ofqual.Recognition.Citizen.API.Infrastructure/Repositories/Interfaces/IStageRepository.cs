using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

public interface IStageRepository
{
    public Task<StageQuestionDetails?> GetStageQuestionByTaskAndQuestionUrl(Stage stageId, string taskNameUrl, string questionNameUrl);
    public Task<StageQuestionDto?> GetFirstQuestionByStage(Stage stageId);
    public Task<StageStatusView?> GetStageStatus(Guid applicationId, Stage stageId);
    public Task<IEnumerable<StageTaskView>?> GetAllStageTasksByStageId(Stage stageId);
    public Task<bool> UpsertStageStatusRecord(StageStatus stageStatus);
}
