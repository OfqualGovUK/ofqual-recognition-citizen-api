using System.Data;
using Dapper;
using Ofqual.Recognition.Citizen.API.Core.Models;
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
}
