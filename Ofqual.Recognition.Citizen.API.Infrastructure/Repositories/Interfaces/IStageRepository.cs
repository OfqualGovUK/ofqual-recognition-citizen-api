using Ofqual.Recognition.Citizen.API.Core.Enums;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Models.StageStatus;
using Ofqual.Recognition.Citizen.API.Core.Models.StageTask;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces
{
    public interface IStageRepository
    {
        /// <summary>
        /// Upserts a stage status record for the given application and stageStatus.
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="stageStatus"></param>
        /// <returns></returns>
        public Task<bool> UpsertStageStatusRecord(Guid applicationId, StageStatus stageStatus);

        /// <summary>
        /// Retrieves all tasks associated with a specific stage for a given application.
        /// </summary>
        /// <param name="stage"></param>
        /// <returns></returns>
        public Task<IEnumerable<StageTask>> GetAllStageTasksByStage(StageEnum stage);
    }
}
