using System.Data;
using Dapper;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Ofqual.Recognition.Citizen.API.Core.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Models.TaskStatuses;
using Serilog;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly IDbTransaction _dbTransaction;

    public TaskRepository(IDbTransaction dbTransaction)
    {
        _dbTransaction = dbTransaction;
    }

    public async Task<IEnumerable<ITaskItem>> GetAllTask()
    {
        try
        {
            const string query = @"
                SELECT
                    TaskId,
                    TaskName,
                    SectionId,
                    OrderNumber,
                    CreatedDate,
                    ModifiedDate,
                    CreatedByUpn,
                    ModifiedByUpn
                FROM [recognitionCitizen].[Task]";
            return await _dbTransaction.Connection!.QueryAsync<TaskItem>(query, null, _dbTransaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving all tasks");
            return Enumerable.Empty<TaskItem>();
        }
    }

    public async Task<IEnumerable<TaskItemTaskStatusSection>> GetTaskStatusesByApplicationId(Guid applicationId)
    {
        try
        {
            var query = @"
                SELECT
                    S.SectionId,
                    S.SectionName,
                    S.OrderNumber AS SectionOrderNumber,
                    T.TaskId,
                    T.TaskName,
                    T.OrderNumber AS TaskOrderNumber,
                    TS.Status
                FROM recognitionCitizen.TaskStatus TS
                INNER JOIN recognitionCitizen.Task T ON TS.TaskId = T.TaskId
                INNER JOIN recognitionCitizen.Section S ON T.SectionId = S.SectionId
                WHERE TS.ApplicationId = @applicationId
                ORDER BY S.OrderNumber, T.OrderNumber";

            return await _dbTransaction.Connection!.QueryAsync<TaskItemTaskStatusSection>(query, new
            {
                applicationId
            }, _dbTransaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving task statuses for ApplicationId: {ApplicationId}", applicationId);
            return Enumerable.Empty<TaskItemTaskStatusSection>();
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

            int rowsAffected = await _dbTransaction.Connection!.ExecuteAsync(query, taskStatusEntries, _dbTransaction);

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

            var rowsAffected = await _dbTransaction.Connection!.ExecuteAsync(query, new
            {
                applicationId,
                taskId,
                modifiedByUpn = "USER", // TODO: replace once auth gets added
                status = (int)status
            }, _dbTransaction);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating task status for ApplicationId: {ApplicationId}, TaskId: {TaskId}, Status: {Status}", applicationId, taskId, status);
            return false;
        }
    }
}