using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models;
using System.Data;
using Serilog;
using Dapper;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories;

public class ApplicationRepository : IApplicationRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _transaction;

    public ApplicationRepository(IDbConnection connection, IDbTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<Application?> CreateApplication()
    {
        try
        {
            const string query = @"
                INSERT INTO [recognitionCitizen].[Application] (
                    CreatedByUpn,
                    ModifiedByUpn
                ) OUTPUT INSERTED.* VALUES (
                    @CreatedByUpn,
                    @ModifiedByUpn
                )";

            return await _connection.QuerySingleAsync<Application>(query, new
            {
                CreatedByUpn = "USER", // TODO: replace once auth gets added
                ModifiedByUpn = "USER" // TODO: replace once auth gets added
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating a new application");
            return null;
        }
    }

    public async Task<IEnumerable<TaskQuestionAnswer?>> GetAllApplicationAnswers(Guid applicationId)
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
            Log.Error(ex, "Error retrieving application Task Question Answers for ApplicationId: {ApplicationId}", applicationId);
            return null;
        }
    }
}
