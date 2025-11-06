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

    public async Task<QuestionDetails?> GetQuestionByTaskAndQuestionUrl(string taskNameUrl, string questionNameUrl)
    {
        try
        {
            var query = @"
                SELECT
                    Q.QuestionId,
                    Q.QuestionContent,
                    Q.TaskId,
                    Q.QuestionNameUrl AS CurrentQuestionNameUrl,
                    Q.QuestionTypeKey AS QuestionType,
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
                LEFT JOIN recognitionCitizen.QuestionType QT ON Q.QuestionTypeId = QT.QuestionTypeId
                JOIN recognitionCitizen.Task T ON Q.TaskId = T.TaskId
                WHERE Q.QuestionNameUrl = @questionNameUrl
                AND T.TaskNameUrl = @taskNameUrl";

            var result = await _connection.QueryFirstOrDefaultAsync<QuestionDetails>(query, new
            {
                taskNameUrl,
                questionNameUrl
            }, _transaction);

            if (result != null && result.QuestionTypeName == null && result.QuestionType == null)
            {
                throw new InvalidOperationException($"QuestionType data is missing for TaskNameUrl: {taskNameUrl}, QuestionNameUrl: {questionNameUrl}");
            }

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving question for TaskNameUrl: {TaskNameUrl}, QuestionNameUrl: {QuestionNameUrl}", taskNameUrl, questionNameUrl);
            return null;
        }
    }

    public async Task<QuestionDetails?> GetQuestionByQuestionId(Guid questionId)
    {
        try
        {
            var query = @"
                SELECT
                    Q.QuestionId,
                    Q.QuestionContent,
                    Q.TaskId,
                    Q.QuestionNameUrl AS CurrentQuestionNameUrl,
                    Q.QuestionTypeKey AS QuestionType,
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
                LEFT JOIN recognitionCitizen.QuestionType QT ON Q.QuestionTypeId = QT.QuestionTypeId
                JOIN recognitionCitizen.Task T ON Q.TaskId = T.TaskId
                WHERE Q.QuestionId = @questionId";
            
            var result = await _connection.QueryFirstOrDefaultAsync<QuestionDetails>(query, new
            {
                questionId
            }, _transaction);

            if (result != null && result.QuestionTypeName == null && result.QuestionType == null)
            {
                throw new InvalidOperationException($"QuestionType data is missing for QuestionId: {questionId}");
            }

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving question by QuestionId: {QuestionId}", questionId);
            return null;
        }
    }

    public async Task<IEnumerable<Question>> GetAllQuestions()
    {
        try
        {
            // First, check for questions with missing QuestionType data
            const string validationQuery = @"
                SELECT Q.QuestionId, Q.QuestionNameUrl                            
                FROM [recognitionCitizen].[Question] Q
                WHERE NOT EXISTS
                    (   SELECT * 
                        FROM [recognitionCitizen].[QuestionType] QT 
                        WHERE Q.QuestionTypeId = QT.QuestionTypeId
                    );";

            var validationResults = await _connection.QueryAsync<dynamic>(validationQuery, transaction: _transaction);

            if(validationResults.Any())
            {
                Log.Error("QuestionType data is missing for folowing Question Id's:");         
                foreach (var row in validationResults)
                {
                    Log.Error("\t- ID: {QuestionId}: QuestionUrl: {QuestionUrl}\n", row.QuestionId, row.QuestionUrl);
                }
                throw new InvalidOperationException("Data integrity check failed: Some questions have missing QuestionType data. See logs for details.");
            }

            // If validation passes, fetch the questions
            const string query = @"
                SELECT
                    QuestionId,
                    QuestionId,
                    TaskId,
                    OrderNumber,
                    QuestionTypeId,
                    QuestionTypeKey AS QuestionType,
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