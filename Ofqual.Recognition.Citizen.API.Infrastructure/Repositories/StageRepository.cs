using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using System.Data;
using Serilog;
using Dapper;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories;

public class StageRepository : IStageRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _transaction;

    public StageRepository(IDbConnection connection, IDbTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<StageQuestionDetails?> GetStageQuestionByTaskAndQuestionUrl(Stage stageId, string taskNameUrl, string questionNameUrl)
    {
        try
        {
            var query = @"
                SELECT *
                FROM (
                    SELECT
                        vst.StageId AS StageId,
                        q.QuestionId,
                        q.QuestionContent,
                        q.TaskId,
                        q.QuestionNameUrl AS CurrentQuestionNameUrl,
                        qt.QuestionTypeName,
                        t.TaskNameUrl AS CurrentTaskNameUrl,
                        LEAD(q.QuestionNameUrl) OVER (ORDER BY vst.OrderNumber) AS NextQuestionNameUrl,
                        LEAD(t.TaskNameUrl) OVER (ORDER BY vst.OrderNumber) AS NextTaskNameUrl,
                        LAG(q.QuestionNameUrl) OVER (ORDER BY vst.OrderNumber) AS PreviousQuestionNameUrl,
                        LAG(t.TaskNameUrl) OVER (ORDER BY vst.OrderNumber) AS PreviousTaskNameUrl
                    FROM recognitionCitizen.Question q
                    INNER JOIN recognitionCitizen.QuestionType qt ON q.QuestionTypeId = qt.QuestionTypeId
                    INNER JOIN recognitionCitizen.Task t ON q.TaskId = t.TaskId
                    INNER JOIN recognitionCitizen.v_StageTask vst ON vst.TaskId = t.TaskId
                    WHERE vst.StageId = @stageId
                ) ordered
                WHERE ordered.CurrentTaskNameUrl = @taskNameUrl
                AND ordered.CurrentQuestionNameUrl = @questionNameUrl;";

            return await _connection.QueryFirstOrDefaultAsync<StageQuestionDetails>(query, new
            {
                taskNameUrl,
                questionNameUrl,
                stageId = (int)stageId
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving question for Stage: {Stage}, TaskNameUrl: {TaskNameUrl}, QuestionNameUrl: {QuestionNameUrl}",
                stageId, taskNameUrl, questionNameUrl);
            return null;
        }
    }

    public async Task<StageQuestionDto?> GetFirstQuestionByStage(Stage stageId)
    {
        try
        {
            var query = @"
                SELECT TOP 1
                    q.QuestionId,
                    q.TaskId,
                    t.TaskNameUrl AS CurrentTaskNameUrl,
                    q.QuestionNameUrl AS CurrentQuestionNameUrl
                FROM recognitionCitizen.Question q
                INNER JOIN recognitionCitizen.Task t ON q.TaskId = t.TaskId
                INNER JOIN recognitionCitizen.v_StageTask vst ON vst.TaskId = t.TaskId
                WHERE vst.StageId = @stageId
                ORDER BY vst.OrderNumber;";

            return await _connection.QueryFirstOrDefaultAsync<StageQuestionDto>(query, new
            {
                stageId = (int)stageId
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving first question for Stage: {Stage}", stageId);
            return null;
        }
    }

    public async Task<StageStatusView?> GetStageStatus(Guid applicationId, Stage stageId)
    {
        try
        {
            const string query = @"
                SELECT
                    ApplicationId,
                    StageId,
                    StageName,
                    StatusId,
                    Status,
                    StageStartDate,
                    StageCompletionDate
                FROM [recognitionCitizen].[v_StageStatus]
                WHERE ApplicationId = @ApplicationId AND StageId = @StageId;";
            
            return await _connection.QueryFirstOrDefaultAsync<StageStatusView>(query, new
            {
                ApplicationId = applicationId,
                StageId = (int)stageId
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to retrieve stage status view for ApplicationId: {ApplicationId}, StageId: {StageId}", applicationId, stageId);
            return null;
        }
    }

    public async Task<IEnumerable<StageTaskView>?> GetAllStageTasksByStageId(Stage stageId)
    {
        try
        {
            const string query = @"
                SELECT
                    StageId,
                    StageName,
                    TaskId,
                    Task,
                    OrderNumber
                FROM [recognitionCitizen].[v_StageTask]
                WHERE StageId = @stageId";

            return await _connection.QueryAsync<StageTaskView>(query, new
            {
                stageId = (int)stageId
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving stage tasks for StageId: {StageId}", stageId);
            return Enumerable.Empty<StageTaskView>();
        }
    }

    public async Task<bool> UpsertStageStatusRecord(StageStatus stageStatus)
    {
        try
        {
            const string query = @"
                MERGE [recognitionCitizen].[StageStatus] AS target
                USING (
                    SELECT
                        @ApplicationId AS ApplicationId,
                        @StageId AS StageId
                ) AS source
                ON target.ApplicationId = source.ApplicationId AND target.StageId = source.StageId
                WHEN MATCHED THEN
                    UPDATE SET
                        StatusId = @StatusId,
                        StageCompletionDate = @StageCompletionDate,
                        ModifiedByUpn = @ModifiedByUpn,
                        ModifiedDate = GETDATE()
                WHEN NOT MATCHED THEN
                    INSERT (
                        ApplicationId,
                        StageId,
                        StatusId,
                        StageStartDate,
                        StageCompletionDate,
                        CreatedByUpn,
                        ModifiedByUpn
                    )
                    VALUES (
                        @ApplicationId,
                        @StageId,
                        @StatusId,
                        @StageStartDate,
                        @StageCompletionDate,
                        @CreatedByUpn,
                        @ModifiedByUpn
                    );";

            var rowsAffected = await _connection.ExecuteAsync(query, new
            {
                stageStatus.ApplicationId,
                StageId = (int)stageStatus.StageId,
                StatusId = (int)stageStatus.StatusId,
                stageStatus.StageStartDate,
                stageStatus.StageCompletionDate,
                stageStatus.CreatedByUpn,
                stageStatus.ModifiedByUpn
            }, _transaction);

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to upsert stage status record. ApplicationId: {ApplicationId}, StageId: {StageId}, StatusId: {StatusId}", stageStatus.ApplicationId, stageStatus.StageId, stageStatus.StatusId);
            return false;
        }
    }
}