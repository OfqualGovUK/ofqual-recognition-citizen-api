using System.Data;
using Dapper;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly IDbTransaction _dbTransaction;

    public TaskRepository(IDbTransaction dbTransaction)
    {
        _dbTransaction = dbTransaction;
    }

    public async Task<List<TaskStatusDto>> GetTasksByApplicationId(Guid applicationId)
    {
        var query = @"
            SELECT
                T.TaskId,
                T.TaskName,
                T.SectionId,
                T.OrderNumber,
                TS.Status,
                TS.CreatedDate AS TaskStatusCreatedDate,
                TS.ModifiedDate AS TaskStatusModifiedDate
            FROM RecognitionCitizen.TaskStatus TS
            INNER JOIN RecognitionCitizen.Task T ON TS.TaskId = T.TaskId
            WHERE TS.ApplicationId = @applicationId";
        return (await _dbTransaction.Connection!.QueryAsync<TaskStatusDto>(query, new
        {
            applicationId
        }, _dbTransaction)).ToList();
    }

    public async Task<bool> UpdateTaskStatus(Guid applicationId, Guid taskId, TaskStatusEnum status)
    {
        var query = @"
            UPDATE RecognitionCitizen.TaskStatus
            SET Status = @status,
                ModifiedDate = GETDATE(),
                ModifiedByUpn = @modifiedByUpn
            WHERE ApplicationId = @applicationId
            AND TaskId = @taskId";

        var rowsAffected = await _dbTransaction.Connection!.ExecuteAsync(query, new
        {
            applicationId,
            taskId,
            modifiedByUpn = "USER",
            status = (int)status
        }, _dbTransaction);
        
        return rowsAffected > 0;
    }
}