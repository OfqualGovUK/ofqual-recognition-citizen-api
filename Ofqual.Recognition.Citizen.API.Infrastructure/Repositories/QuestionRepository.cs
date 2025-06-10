using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models;
using System.Data;
using Serilog;
using Dapper;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories;

public class QuestionRepository : IQuestionRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _transaction;

    public QuestionRepository(IDbConnection connection, IDbTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<QuestionDetails?> GetQuestion(string taskNameUrl, string questionNameUrl)
    {
        try
        {
            var query = @"
                SELECT
                    Q.QuestionId,
                    Q.QuestionContent,
                    Q.TaskId,
                    Q.QuestionNameUrl AS CurrentQuestionNameUrl,
                    QT.QuestionTypeName,
                    (
                        SELECT TOP 1 prev.QuestionNameUrl
                        FROM recognitionCitizen.Question prev
                        WHERE prev.TaskId = Q.TaskId
                        AND prev.OrderNumber < Q.OrderNumber
                        ORDER BY prev.OrderNumber DESC
                    ) AS PreviousQuestionNameUrl,
                    (
                        SELECT TOP 1 next.QuestionNameUrl
                        FROM recognitionCitizen.Question next
                        WHERE next.TaskId = Q.TaskId
                        AND next.OrderNumber > Q.OrderNumber
                        ORDER BY next.OrderNumber ASC
                    ) AS NextQuestionNameUrl,
                    T.TaskNameUrl
                FROM recognitionCitizen.Question Q
                JOIN recognitionCitizen.QuestionType QT ON Q.QuestionTypeId = QT.QuestionTypeId
                JOIN recognitionCitizen.Task T ON Q.TaskId = T.TaskId
                WHERE Q.QuestionNameUrl = @questionNameUrl
                AND T.TaskNameUrl = @taskNameUrl";

            return await _connection.QueryFirstOrDefaultAsync<QuestionDetails>(query, new
            {
                taskNameUrl,
                questionNameUrl
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving question for TaskNameUrl: {TaskNameUrl}, QuestionNameUrl: {QuestionNameUrl}", taskNameUrl, questionNameUrl);
            return null;
        }
    }

    public async Task<QuestionDetails?> GetQuestion(Guid taskId, Guid questionId)
    {
        try
        {
            var query = @"
                SELECT
                    Q.QuestionId,
                    Q.QuestionContent,
                    Q.TaskId,
                    Q.QuestionNameUrl AS CurrentQuestionNameUrl,
                    QT.QuestionTypeName,
                    (
                        SELECT TOP 1 prev.QuestionNameUrl
                        FROM recognitionCitizen.Question prev
                        WHERE prev.TaskId = Q.TaskId
                        AND prev.OrderNumber < Q.OrderNumber
                        ORDER BY prev.OrderNumber DESC
                    ) AS PreviousQuestionNameUrl,
                    (
                        SELECT TOP 1 next.QuestionNameUrl
                        FROM recognitionCitizen.Question next
                        WHERE next.TaskId = Q.TaskId
                        AND next.OrderNumber > Q.OrderNumber
                        ORDER BY next.OrderNumber ASC
                    ) AS NextQuestionNameUrl,
                    T.TaskNameUrl
                FROM recognitionCitizen.Question Q
                INNER JOIN recognitionCitizen.QuestionType QT ON Q.QuestionTypeId = QT.QuestionTypeId
                INNER JOIN recognitionCitizen.Task T ON Q.TaskId = T.TaskId
                WHERE Q.QuestionId = @questionId AND T.TaskId = @taskId";

            var result = await _connection.QueryFirstOrDefaultAsync<QuestionDetails>(query, new
            {
                taskId,
                questionId
            }, _transaction);

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving question for TaskId: {TaskId}, QuestionId: {questionId}", taskId, questionId);
            return null;
        }
    }

    public async Task<bool> UpsertQuestionAnswer(Guid applicationId, Guid questionId, string answer)
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
                        ModifiedByUpn = @ModifiedByUpn
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
                CreatedByUpn = "USER", // TODO: replace once auth gets added
                ModifiedByUpn = "USER" // TODO: replace once auth gets added
            }, _transaction);

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "Error upserting application answer. ApplicationId: {ApplicationId}, QuestionId: {QuestionId}, Answer: {Answer}",
                applicationId,
                questionId,
                answer);
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
                    a.Answer
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


    public async Task<bool> CheckIfQuestionAnswerExists(Guid taskId, Guid questionId, string questionItemName, string questionItemAnswer) =>
       await _connection.QuerySingleAsync<bool>(@"SELECT ISNULL((   
                                                            SELECT TOP(1) 1 [row] 
                                                            FROM   [recognitionCitizen].[ApplicationAnswers] AS A
                                                            JOIN   [recognitionCitizen].[Question] AS Q ON Q.QuestionId = A.QuestionId 
                                                            JOIN   [recognitionCitizen].[Task] AS T ON Q.TaskId = T.TaskId 
                                                            WHERE  T.TaskId = @taskId
                                                            AND    Q.QuestionId = @QuestionId
                                                            AND    JSON_VALUE(A.[Answer], @questionItemName) = @questionItemAnswer),0);",
           new { taskId, questionId, questionItemName, questionItemAnswer }, _transaction);

    public async Task<IEnumerable<Question>> GetAllQuestions()
    {
        try
        {
            const string query = @"
                SELECT
                    QuestionId,
                    TaskId,
                    OrderNumber,
                    QuestionTypeId,
                    QuestionContent,
                    QuestionNameUrl,
                    CreatedDate,
                    ModifiedDate,
                    CreatedByUpn,
                    ModifiedByUpn
                FROM [recognitionCitizen].[Question]";

            return await _connection.QueryAsync<Question>(query, transaction: _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to fetch all questions");
            return Enumerable.Empty<Question>();
        }
    }
}