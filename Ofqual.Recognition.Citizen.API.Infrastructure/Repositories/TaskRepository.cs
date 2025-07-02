using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using System.Data;
using Serilog;
using Dapper;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _transaction;

    public TaskRepository(IDbConnection connection, IDbTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<IEnumerable<ITaskItem>> GetAllTask()
    {
        try
        {
            const string query = @"
                SELECT
                    TaskId,
                    TaskName,
                    TaskNameUrl,
                    SectionId,
                    OrderNumber,
                    CreatedDate,
                    ModifiedDate,
                    CreatedByUpn,
                    ModifiedByUpn
                FROM [recognitionCitizen].[Task]";

            return await _connection.QueryAsync<TaskItem>(query, null, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving all tasks");
            return Enumerable.Empty<TaskItem>();
        }
    }

    public async Task<TaskItem?> GetTaskByTaskNameUrl(string taskNameUrl)
    {
        try
        {
            const string query = @"
                SELECT
                    TaskId,
                    TaskName,
                    TaskNameUrl,
                    SectionId,
                    OrderNumber AS TaskOrderNumber,
                    CreatedDate,
                    ModifiedDate,
                    CreatedByUpn,
                    ModifiedByUpn
                FROM [recognitionCitizen].[Task]
                WHERE TaskNameUrl = @taskNameUrl";

            return await _connection.QueryFirstOrDefaultAsync<TaskItem>(query, new
            {
                taskNameUrl
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while retrieving task with URL: {TaskNameUrl}", taskNameUrl);
            return null;
        }
    }

    public async Task<IEnumerable<TaskItemStatusSection>> GetTaskStatusesByApplicationId(Guid applicationId)
    {
        try
        {
            var query = @"
                SELECT
                    S.SectionId,
                    S.SectionName,
                    S.OrderNumber AS SectionOrderNumber,
                    T.TaskId,
                    T.TaskNameUrl,
                    T.TaskName,
                    T.OrderNumber AS TaskOrderNumber,
                    TS.TaskStatusId,
                    TS.Status,
                    Q.QuestionNameUrl
                FROM recognitionCitizen.TaskStatus TS
                INNER JOIN recognitionCitizen.Task T ON TS.TaskId = T.TaskId
                INNER JOIN recognitionCitizen.Section S ON T.SectionId = S.SectionId
                INNER JOIN recognitionCitizen.Question Q on T.TaskId = Q.TaskId AND Q.OrderNumber = 1
                WHERE TS.ApplicationId = @applicationId
                ORDER BY S.OrderNumber, T.OrderNumber";

            return await _connection.QueryAsync<TaskItemStatusSection>(query, new
            {
                applicationId
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving task statuses for ApplicationId: {ApplicationId}", applicationId);
            return Enumerable.Empty<TaskItemStatusSection>();
        }
    }

    public async Task<bool> CreateTaskStatuses(IEnumerable<TaskItemStatus> statuses)
    {
        try
        {
            const string query = @"
                INSERT INTO [recognitionCitizen].[TaskStatus] (
                    ApplicationId,
                    TaskId,
                    Status,
                    CreatedByUpn,
                    ModifiedByUpn
                ) VALUES (
                    @ApplicationId,
                    @TaskId,
                    @Status,
                    @CreatedByUpn,
                    @ModifiedByUpn
                )";
            
            var rowsAffected = await _connection.ExecuteAsync(query, statuses, _transaction);
            
            return rowsAffected == statuses.Count();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating task statuses. Task count: {TaskCount}", statuses.Count());
            return false;
        }
    }

    public async Task<bool> UpdateTaskStatus(Guid applicationId, Guid taskId, StatusType status, string upn)
    {
        try
        {
            var query = @"
                UPDATE recognitionCitizen.TaskStatus
                SET Status = @status,
                    ModifiedDate = GETDATE(),
                    ModifiedByUpn = @modifiedByUpn
                WHERE ApplicationId = @applicationId
                AND TaskId = @taskId";

            var rowsAffected = await _connection.ExecuteAsync(query, new
            {
                applicationId,
                taskId,
                modifiedByUpn = upn,
                status = (int)status
            }, _transaction);

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating task status for ApplicationId: {ApplicationId}, TaskId: {TaskId}, Status: {Status}", applicationId, taskId, status);
            return false;
        }
    }
}