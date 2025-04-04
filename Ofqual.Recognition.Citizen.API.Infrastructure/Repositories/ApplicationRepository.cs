using System.Data;
using Dapper;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Serilog;

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

    public async Task<Application> CreateApplication()
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
            return null!;
        }
    }

    public async Task<bool> InsertApplicationAnswer(Guid applicationId, Guid questionId, string answer)
    {
        try
        {
            const string query = @"
                INSERT INTO [recognitionCitizen].[ApplicationAnswers ] (
                    ApplicationId,
                    QuestionId,
                    Answer,
                    CreatedByUpn,
                    ModifiedByUpn
                ) OUTPUT INSERTED.* VALUES (
                    @ApplicationId,
                    @QuestionId,
                    @Answer,
                    @CreatedByUpn,
                    @ModifiedByUpn
                )";

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
}
