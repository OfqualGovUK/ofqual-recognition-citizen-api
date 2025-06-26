using Dapper;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Serilog;
using System.Data;

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

    public async Task<Application?> GetLatestApplication(string oid)
    {
        try
        {
            const string query = @"
                SELECT TOP 1 * FROM [recognitionCitizen].[Application] AS app
                INNER JOIN [recognitionCitizen].[RecognitionCitizenUser] ON app.OwnerUserId = UserId
                WHERE B2CId = @oid
                ORDER BY app.CreatedDate DESC
            ";

            return await _connection.QuerySingleAsync<Application>(query, new
            {
                oid
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception raised when trying to retrieve an application in ApplicationRepository::GetLatestApplication");
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

    public async Task<ApplicationStatus?> CheckIfReadyToSubmit(Guid applicationId)
    {
        var result = await _connection.QuerySingleAsync<int>(@"
            SELECT COUNT(*) AS [Count]
            FROM   [recognitionCitizen].[Task] AS t           
            JOIN   [recognitionCitizen].[TaskStatus] ts ON ts.TaskId = t.TaskId
            WHERE  t.TaskNameUrl NOT IN(N'get-engagement', N'declaration-submit') --replace w/ tasktype check when implemented                              
            AND    ts.[Status] <> @CompletedStatus
            AND    ts.ApplicationId = @ApplicationId;",
            new
            {
                completedStatus = (int)TaskStatusEnum.Completed,
                applicationId
            },
            _transaction);

        return result > 0
            ? ApplicationStatus.InProgress
            : ApplicationStatus.Completed;
    }
}
