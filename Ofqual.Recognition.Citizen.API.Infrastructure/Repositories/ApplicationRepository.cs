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

    public async Task<Application?> CreateApplication(Guid userId, string upn)
    {
        try
        {
            const string query = @"
                INSERT INTO [recognitionCitizen].[Application] (
                    OwnerUserId,
                    CreatedByUpn,
                    ModifiedByUpn
                ) 
                OUTPUT INSERTED.* 
                VALUES (
                    @OwnerUserId,
                    @CreatedByUpn,
                    @ModifiedByUpn
                )";

            return await _connection.QuerySingleAsync<Application>(query, new
            {
                OwnerUserId = userId,
                CreatedByUpn = upn,
                ModifiedByUpn = upn
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to create application for userId: {UserId}", userId);
            return null;
        }
    }

    public async Task<Application?> GetLatestApplication(string oid)
    {
        try
        {
            const string query = @"
                SELECT TOP 1 * FROM [recognitionCitizen].[Application] AS app
                INNER JOIN [recognitionCitizen].[RecognitionCitizenUser] ON app.OwnerUserId = UserId
                WHERE B2CId = @oid
                ORDER BY app.CreatedDate DESC";

            return await _connection.QuerySingleAsync<Application>(query, new
            {
                oid
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to retrieve latest application for OID: {Oid}", oid);
            return null;
        }
    }
}