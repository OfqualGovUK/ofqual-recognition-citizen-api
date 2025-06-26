using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models;
using System.Data;
using Serilog;
using Dapper;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories;

public class ApplicationAnswersRepository : IApplicationAnswersRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _transaction;

    public ApplicationAnswersRepository(IDbConnection connection, IDbTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<IEnumerable<TaskQuestionAnswer>?> GetAllApplicationAnswers(Guid applicationId)
    {
        try
        {
            const string query = @"
                SELECT
                    AA.ApplicationId,
                    AA.QuestionId,
                    AA.Answer,
                    Q.TaskId,
                    Q.QuestionContent,
                    Q.QuestionNameUrl,
                    T.TaskName,
                    T.TaskNameUrl,
                    T.OrderNumber AS TaskOrder
                FROM [recognitionCitizen].[ApplicationAnswers] AA
                INNER JOIN [recognitionCitizen].[Question] Q ON AA.QuestionId = Q.QuestionId
                INNER JOIN [recognitionCitizen].[Task] T ON Q.TaskId = T.TaskId
                WHERE ApplicationId = @applicationId";

            return await _connection.QueryAsync<TaskQuestionAnswer>(query, new
            {
                applicationId
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to retrieve all task question answers for ApplicationId: {ApplicationId}", applicationId);
            return null;
        }
    }

    public async Task<bool> UpsertQuestionAnswer(Guid applicationId, Guid questionId, string answer, string upn)
    {
        try
        {
            const string query = @"
                MERGE [recognitionCitizen].[ApplicationAnswers] AS target
                USING (SELECT @ApplicationId AS ApplicationId, @QuestionId AS QuestionId) AS source
                    ON target.ApplicationId = source.ApplicationId AND target.QuestionId = source.QuestionId
                WHEN MATCHED THEN
                    UPDATE SET
                        Answer = @Answer,
                        ModifiedByUpn = @ModifiedByUpn,
                        ModifiedDate = GETDATE()
                WHEN NOT MATCHED THEN
                    INSERT (
                        ApplicationId, 
                        QuestionId,
                        Answer, 
                        CreatedByUpn, 
                        ModifiedByUpn
                    )
                    VALUES (
                        @ApplicationId, 
                        @QuestionId, 
                        @Answer, 
                        @CreatedByUpn, 
                        @ModifiedByUpn
                    );";

            var rowsAffected = await _connection.ExecuteAsync(query, new
            {
                applicationId,
                questionId,
                answer,
                CreatedByUpn = upn,
                ModifiedByUpn = upn
            }, _transaction);

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error upserting application answer. ApplicationId: {ApplicationId}, QuestionId: {QuestionId}, Answer: {Answer}", applicationId, questionId, answer);
            return false;
        }
    }

    public async Task<IEnumerable<TaskQuestionAnswer>> GetTaskQuestionAnswers(Guid applicationId, Guid taskId)
    {
        try
        {
            const string query = @"
                SELECT
                    t.TaskId,
                    t.TaskName,
                    t.TaskNameUrl,
                    t.OrderNumber AS TaskOrder,
                    q.QuestionId,
                    q.QuestionContent,
                    q.QuestionNameUrl,
                    a.Answer,
                    a.ApplicationId
                FROM [recognitionCitizen].[Task] t
                INNER JOIN [recognitionCitizen].[Question] q ON q.TaskId = t.TaskId
                LEFT JOIN [recognitionCitizen].[ApplicationAnswers] a
                    ON a.QuestionId = q.QuestionId AND a.ApplicationId = @ApplicationId
                WHERE t.TaskId = @TaskId
                ORDER BY t.OrderNumber, q.OrderNumber";

            return await _connection.QueryAsync<TaskQuestionAnswer>(query, new
            {
                ApplicationId = applicationId,
                TaskId = taskId
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to fetch question answers for TaskId: {TaskId}, ApplicationId: {ApplicationId}", taskId, applicationId);
            return Enumerable.Empty<TaskQuestionAnswer>();
        }
    }

    public async Task<QuestionAnswerDto?> GetQuestionAnswer(Guid applicationId, Guid questionId)
    {
        try
        {
            const string query = @"
                SELECT
                    q.QuestionId,
                    a.Answer
                FROM [recognitionCitizen].[Question] q
                LEFT JOIN [recognitionCitizen].[ApplicationAnswers] a
                    ON a.QuestionId = q.QuestionId AND a.ApplicationId = @ApplicationId
                WHERE q.QuestionId = @QuestionId";

            return await _connection.QuerySingleOrDefaultAsync<QuestionAnswerDto>(query, new
            {
                ApplicationId = applicationId,
                QuestionId = questionId
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to fetch answer for QuestionId: {QuestionId}, ApplicationId: {ApplicationId}", questionId, applicationId);
            return null;
        }
    }

    public async Task<bool> CheckIfQuestionAnswerExists(Guid questionId, string questionItemName, string questionItemAnswer)
    {
        try
        {
            const string query = @"
                SELECT ISNULL((
                    SELECT TOP(1) 1
                    FROM recognitionCitizen.ApplicationAnswers AS A
                    JOIN recognitionCitizen.Question AS Q ON Q.QuestionId = A.QuestionId
                    WHERE Q.QuestionId = @questionId
                    AND JSON_VALUE(A.Answer, CONCAT('$.', @questionItemName)) = @questionItemAnswer
                ), 0);";

            return await _connection.QuerySingleAsync<bool>(
                query,
                new
                {
                    questionId,
                    questionItemName,
                    questionItemAnswer
                },
                _transaction
            );
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking if answer exists for QuestionId: {QuestionId} and Key: {Key}", questionId, questionItemName);
            return false;
        }
    }
}
