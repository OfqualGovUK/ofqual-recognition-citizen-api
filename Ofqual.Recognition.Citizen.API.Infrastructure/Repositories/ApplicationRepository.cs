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

    public async Task<bool> UpdateApplicationSubmittedDate(Guid applicationId, string modifiedByUpn)
    {
        try
        {
            const string query = @"
                UPDATE [recognitionCitizen].[Application]
                SET 
                    SubmittedDate = GETDATE(),
                    ModifiedDate = GETDATE(),
                    ModifiedByUpn = @ModifiedByUpn
                WHERE ApplicationId = @ApplicationId";

            var rowsAffected = await _connection.ExecuteAsync(query, new
            {
                ApplicationId = applicationId,
                ModifiedByUpn = modifiedByUpn
            }, _transaction);

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to set SubmittedDate for ApplicationId: {ApplicationId}", applicationId);
            return false;
        }
    }

    public async Task<Application?> GetLatestApplication(string oid)
    {
        try
        {
            const string query = @"
                SELECT TOP 1 
                    app.ApplicationId,
                    app.OwnerUserId,
                    app.SubmittedDate,
                    app.ApplicationReleaseDate,
                    app.OrganisationId,
                    app.CreatedDate,
                    app.ModifiedDate,
                    app.CreatedByUpn,
                    app.ModifiedByUpn
                FROM [recognitionCitizen].[Application] AS app
                INNER JOIN [recognitionCitizen].[RecognitionCitizenUser] ON app.OwnerUserId = UserId
                WHERE B2CId = @oid
                ORDER BY app.CreatedDate DESC";

            return await _connection.QuerySingleOrDefaultAsync<Application>(query, new { oid }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to retrieve latest application for OID: {Oid}", oid);
            return null;
        }
    }

    public async Task<Application?> GetApplicationById(Guid applicationId)
    {
        try
        {
            const string query = @"
                SELECT 
                    app.ApplicationId,
                    app.OwnerUserId,
                    app.SubmittedDate,
                    app.ApplicationReleaseDate,
                    app.OrganisationId,
                    app.CreatedDate,
                    app.ModifiedDate,
                    app.CreatedByUpn,
                    app.ModifiedByUpn
                FROM [recognitionCitizen].[Application] AS app
                WHERE app.ApplicationId = @applicationId";

            return await _connection.QuerySingleOrDefaultAsync<Application>(query, new { applicationId }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to retrieve application with ID: {ApplicationId}", applicationId);
            return null;
        }
    }

    public async Task<string?> GetContactNameById(Guid applicationId)
    {
        try
        {
            const string query = @"
                SELECT  
                    o.Name
                FROM [recognitionCitizen].[Application] AS a
                JOIN [organisation].[Organisation] AS o 
                    ON o.Id = a.OrganisationId
                WHERE a.ApplicationId = @ApplicationId
                AND a.SubmittedDate IS NOT NULL;";

            return await _connection.QuerySingleOrDefaultAsync<string>(query, new { applicationId }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to retrieve contact name with ID: {ApplicationId}", applicationId);
            return null;
        }
    }
}