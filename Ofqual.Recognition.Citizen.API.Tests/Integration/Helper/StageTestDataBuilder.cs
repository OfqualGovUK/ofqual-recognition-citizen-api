using Ofqual.Recognition.Citizen.API.Infrastructure;
using Dapper;

namespace Ofqual.Recognition.Citizen.Tests.Integration.Helper;

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
}