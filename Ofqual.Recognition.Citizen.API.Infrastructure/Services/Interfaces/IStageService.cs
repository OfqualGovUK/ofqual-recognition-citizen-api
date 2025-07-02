using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;

public interface IStageService
{
    public Task<bool> EvaluateAndUpsertAllStageStatus(Guid applicationId);
    public Task<bool> EvaluateAndUpsertStageStatus(Guid applicationId, StageType stage);
}
