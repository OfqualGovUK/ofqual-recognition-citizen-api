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