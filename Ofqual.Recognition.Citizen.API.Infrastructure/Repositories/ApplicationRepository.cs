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
    public IUserInformationService _userInformationService;

    public ApplicationRepository(IDbConnection connection, IDbTransaction transaction, IUserInformationService userInformationService)
    {
        _connection = connection;
        _transaction = transaction;
        _userInformationService = userInformationService;
    }

    public async Task<Application?> CreateApplication()
    {
        string oid = _userInformationService.GetCurrentUserObjectId();
        string displayName = _userInformationService.GetCurrentUserDisplayName();
        string upn = _userInformationService.GetCurrentUserUpn();

        User? user = await CreateUser(oid, displayName, upn);

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
                OwnerUserId = user.UserId,
                CreatedByUpn = upn,
                ModifiedByUpn = upn
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating a new application");
            return null;
        }
    }

    private async Task<User?> CreateUser(string oid, string displayName, string emailAddress)
    {
        try
        {
            const string query = @"
                INSERT INTO [recognitionCitizen].[RecognitionCitizenUser] (
                    UserId,
                    B2CId,
                    EmailAddress,
                    DisplayName,
                    CreatedByUpn,
                    ModifiedByUpn
                ) 
                OUTPUT INSERTED.* 
                VALUES (
                    @UserId,
                    @B2CId,
                    @EmailAddress,
                    @DisplayName,
                    @CreatedByUpn,
                    @ModifiedByUpn
                )";

            return await _connection.QuerySingleAsync<User>(query, new
            {
                UserId = Guid.NewGuid(), // TEMPORARY: IF IN REVIEW DO NOT ACCEPT
                B2CId = oid,
                EmailAddress = emailAddress,
                DisplayName = displayName,
                CreatedByUpn = emailAddress,
                ModifiedByUpn = emailAddress,
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating a new user");
            return null;
        }
    }
}
