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

    public async Task<QuestionDto?> GetQuestion(string questionURL)
    {
        try
        {
            var query = @"
                SELECT
                    Q.QuestionId,
                    Q.QuestionContent,
                    Q.TaskId,
                    Q.QuestionURL AS CurrentQuestionUrl,
                    QT.QuestionTypeName,
                    (
                        SELECT TOP 1 prev.QuestionURL
                        FROM recognitionCitizen.Question prev
                        WHERE prev.TaskId = Q.TaskId
                        AND prev.OrderNumber < Q.OrderNumber
                        ORDER BY prev.OrderNumber DESC
                    ) AS PreviousQuestionUrl
                FROM recognitionCitizen.Question Q
                INNER JOIN recognitionCitizen.QuestionType QT ON Q.QuestionTypeId = QT.QuestionTypeId
                WHERE Q.QuestionURL = @questionURL";

            var result = await _connection.QueryFirstOrDefaultAsync<QuestionDto>(query, new
            {
                questionURL
            }, _transaction);

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving question for QuestionURL: {questionURL}", questionURL);
            return null;
        }
    }

    public async Task<QuestionAnswerSubmissionResponseDto?> GetNextQuestionUrl(Guid currentQuestionId)
    {
        try
        {
            const string query = @"
                SELECT TOP 1
                    [next].QuestionURL AS NextQuestionUrl
                FROM [recognitionCitizen].[Question] AS [current]
                JOIN [recognitionCitizen].[Question] AS [next]
                    ON [current].TaskId = [next].TaskId
                WHERE [current].QuestionId = @QuestionId
                AND [next].OrderNumber > [current].OrderNumber
                ORDER BY [next].OrderNumber ASC";

            var result = await _connection.QueryFirstOrDefaultAsync<QuestionAnswerSubmissionResponseDto>(query, new
            {
                QuestionId = currentQuestionId
            }, _transaction);

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving next question URL for QuestionId: {QuestionId}", currentQuestionId);
            return null;
        }
    }

    public async Task<bool> InsertQuestionAnswer(Guid applicationId, Guid questionId, string answer)
    {
        try
        {
            const string query = @"
                IF NOT EXISTS (
                    SELECT 1 FROM [recognitionCitizen].[ApplicationAnswers]
                    WHERE ApplicationId = @ApplicationId AND QuestionId = @QuestionId
                )
                BEGIN
                    INSERT INTO [recognitionCitizen].[ApplicationAnswers] (
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
                    )
                END
            ";

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
            Log.Error(ex, "Error inserting application answer. ApplicationId: {ApplicationId}, QuestionId: {QuestionId}, Answer: {Answer}", applicationId, questionId, answer);
            return false;
        }
    }

    public async Task<IEnumerable<TaskQuestionAnswerDto>> GetTaskQuestionAnswers(Guid applicationId, Guid taskId)
    {
        try
        {
            const string query = @"
                SELECT
                    t.TaskId,
                    t.TaskName,
                    t.OrderNumber AS TaskOrder,
                    q.QuestionId,
                    q.QuestionContent,
                    q.QuestionURL,
                    a.Answer
                FROM [recognitionCitizen].[Task] t
                INNER JOIN [recognitionCitizen].[Question] q ON q.TaskId = t.TaskId
                LEFT JOIN [recognitionCitizen].[ApplicationAnswers] a
                    ON a.QuestionId = q.QuestionId AND a.ApplicationId = @ApplicationId
                WHERE t.TaskId = @TaskId
                ORDER BY t.OrderNumber, q.OrderNumber";

            var results = await _connection.QueryAsync<TaskQuestionAnswerDto>(query, new
            {
                ApplicationId = applicationId,
                TaskId = taskId
            }, _transaction);

            return results;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to fetch question answers for TaskId: {TaskId}, ApplicationId: {ApplicationId}", taskId, applicationId);
            return Enumerable.Empty<TaskQuestionAnswerDto>();
        }
    }
}