using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

public interface IStageRepository
{
    public Task<StageQuestionDetails?> GetStageQuestionByTaskAndQuestionUrl(StageEnum stageId, string taskNameUrl, string questionNameUrl);
    public Task<StageQuestionDto?> GetFirstQuestionByStage(StageEnum stageId);
    public Task<StageStatus?> GetStageStatus(Guid applicationId, StageEnum stageId);
    public Task<IEnumerable<StageTaskView>?> GetAllStageTasksByStageId(StageEnum stageId);
    public Task<bool> UpsertStageStatusRecord(StageStatus stageStatus);
}
