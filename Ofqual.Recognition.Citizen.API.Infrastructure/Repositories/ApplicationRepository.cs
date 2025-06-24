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

    public async Task<Application?> CreateApplication(string oid, string displayName, string upn)
    {
        try
        {
            User user = await CreateUser(oid, displayName, upn);

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
                OwnerUserId = user.UserId,
                CreatedByUpn = upn,
                ModifiedByUpn = upn
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception raised when trying to create an application in ApplicationRepository::CreateApplication");
            return null;
        }
    }

    private async Task<User> CreateUser(string oid, string displayName, string emailAddress)
    {
        try
        {
            const string query = @"
                INSERT INTO [recognitionCitizen].[RecognitionCitizenUser] (
                    B2CId,
                    EmailAddress,
                    DisplayName,
                    CreatedByUpn,
                    ModifiedByUpn
                ) 
                OUTPUT INSERTED.* 
                VALUES (
                    @B2CId,
                    @EmailAddress,
                    @DisplayName,
                    @CreatedByUpn,
                    @ModifiedByUpn
                )";

            return await _connection.QuerySingleAsync<User>(query, new
            {
                B2CId = oid,
                EmailAddress = emailAddress,
                DisplayName = displayName,
                CreatedByUpn = emailAddress,
                ModifiedByUpn = emailAddress,
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception raised when trying to create a user in ApplicationRepository::CreateUser");
            throw;
        }
    }
}
