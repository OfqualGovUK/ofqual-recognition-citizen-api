using System.Data;
using Dapper;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Serilog;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories;

public class ApplicationRepository : IApplicationRepository
{
    private readonly IDbTransaction _dbTransaction;

    public ApplicationRepository(IDbTransaction dbTransaction)
    {
        _dbTransaction = dbTransaction;
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
            return await _dbTransaction.Connection!.QuerySingleAsync<Application>(query, new
            {
                CreatedByUpn = "USER", // TODO: replace once auth gets added
                ModifiedByUpn = "USER" // TODO: replace once auth gets added
            }, _dbTransaction);
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
                    Answer
                ) OUTPUT INSERTED.* VALUES (
                    @ApplicationId,
                    @QuestionId,
                    @Answer
                )";

            var rowsAffected = await _dbTransaction.Connection!.ExecuteAsync(query, new
            {
                applicationId,
                questionId,
                answer,
            }, _dbTransaction);
            
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error inserting application answer. ApplicationId: {ApplicationId}, QuestionId: {QuestionId}, Answer: {Answer}", applicationId, questionId, answer);
            return false;
        }
    }
}
