using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Dapper;

namespace Ofqual.Recognition.Citizen.Tests.Integration.Builders;

public static class StageTestDataBuilder
{
    public static async Task CreateStageTask(UnitOfWork unitOfWork, int stageId, Guid taskId, int order)
    {
        await unitOfWork.Connection.ExecuteAsync(@"
            INSERT INTO recognitionCitizen.StageTask
                (StageId, TaskId, OrderNumber, Enabled, CreatedDate, ModifiedDate, CreatedByUpn)
            VALUES
                (@StageId, @TaskId, @OrderNumber, 1, @Now, @Now, @Upn);",
            new
            {
                StageId = stageId,
                TaskId = taskId,
                OrderNumber = order,
                Now = DateTime.UtcNow,
                Upn = "test@ofqual.gov.uk"
            },
            unitOfWork.Transaction);
    }
    
    public static async Task CreateStageStatus(UnitOfWork unitOfWork, StageStatus stageStatus)
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
    }
}