using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models;
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
                    ReviewFlag,
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
}