using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models;
using System.Data;
using Serilog;
using Dapper;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _transaction;

    public UserRepository(IDbConnection connection, IDbTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<User?> CreateUser(string oid, string displayName, string emailAddress)
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
            Log.Error(ex, "Failed to create user with OID: {Oid}", oid);
            return null;
        }
    }
}