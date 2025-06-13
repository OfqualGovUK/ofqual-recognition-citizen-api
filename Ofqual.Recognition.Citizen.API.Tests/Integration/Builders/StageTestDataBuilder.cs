using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Dapper;

namespace Ofqual.Recognition.Citizen.Tests.Integration.Builders;

public static class StageTestDataBuilder
{
    public static async Task<StageTask> CreateStageTask(UnitOfWork unitOfWork, StageTask stageTask)
    {
        await unitOfWork.Connection.ExecuteAsync(@"
            INSERT INTO recognitionCitizen.StageTask
                (StageId, TaskId, OrderNumber, Enabled, CreatedDate, ModifiedDate, CreatedByUpn, ModifiedByUpn)
            VALUES
                (@StageId, @TaskId, @OrderNumber, @Enabled, @CreatedDate, @ModifiedDate, @CreatedByUpn, @ModifiedByUpn);",
            new
            {
                StageId = (int)stageTask.StageId,
                stageTask.TaskId,
                stageTask.OrderNumber,
                stageTask.Enabled,
                stageTask.CreatedDate,
                stageTask.ModifiedDate,
                stageTask.CreatedByUpn,
                stageTask.ModifiedByUpn
            },
            unitOfWork.Transaction);

        return stageTask;
    }

    public static async Task<StageStatus> CreateStageStatus(UnitOfWork unitOfWork, StageStatus stageStatus)
    {
        await unitOfWork.Connection.ExecuteAsync(@"
            INSERT INTO recognitionCitizen.StageStatus
                (ApplicationId, StageId, StatusId, StageStartDate, StageCompletionDate,
                 CreatedByUpn, ModifiedByUpn, CreatedDate, ModifiedDate)
            VALUES
                (@ApplicationId, @StageId, @StatusId, @StageStartDate, @StageCompletionDate,
                 @CreatedByUpn, @ModifiedByUpn, @CreatedDate, @ModifiedDate);",
            new
            {
                stageStatus.ApplicationId,
                StageId = (int)stageStatus.StageId,
                StatusId = (int)stageStatus.StatusId,
                stageStatus.StageStartDate,
                stageStatus.StageCompletionDate,
                stageStatus.CreatedByUpn,
                stageStatus.ModifiedByUpn,
                stageStatus.CreatedDate,
                stageStatus.ModifiedDate
            },
            unitOfWork.Transaction);

        return stageStatus;
    }
}