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
                    OrderNumber,
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

    public async Task<bool> CreateTaskStatuses(Guid applicationId, IEnumerable<ITaskItem> tasks)
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

            var taskStatusEntries = tasks.Select(task => new
            {
                ApplicationId = applicationId,
                task.TaskId,
                Status = TaskStatusEnum.NotStarted,
                CreatedByUpn = "USER", // TODO: replace once auth gets added
                ModifiedByUpn = "USER" // TODO: replace once auth gets added
            }).ToList();

            int rowsAffected = await _connection.ExecuteAsync(query, taskStatusEntries, _transaction);

            // Check if the number of inserted rows matches the number of tasks
            return rowsAffected == taskStatusEntries.Count;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating task statuses for ApplicationId: {ApplicationId}. Task count: {TaskCount}", applicationId, tasks.Count());
            return false;
        }
    }

    public async Task<bool> UpdateTaskStatus(Guid applicationId, Guid taskId, TaskStatusEnum status)
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
                modifiedByUpn = "USER", // TODO: replace once auth gets added
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