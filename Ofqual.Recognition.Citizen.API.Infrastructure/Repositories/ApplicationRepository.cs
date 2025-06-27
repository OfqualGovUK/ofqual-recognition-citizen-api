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


    /// <summary>
    /// Checks and complete an application
    /// </summary>
    /// <param name="applicationId">The application to complete</param>
    /// <param name="upn">UPN of user to complete</param>    
    public async Task<ApplicationStatus?> CheckAndCompleteApplication(Guid applicationId, string upn)
    {
        try
        {
            //if application is already completed or non-existant, return  
            if (await IsApplicationSubmitted(applicationId) ?? true)
                return null;

            if (await CheckApplicationForPendingTasks(applicationId) > 0) 
                return ApplicationStatus.InProgress;

            await _connection.QueryAsync(@"
                UPDATE [recognitionCitizen].[Application];
                SET     SubmittedDate = @SubmittedDate,
                        ModifiedDate = @SubmittedDate,
                        ModifiedByUpn = @UserUpn                
                WHERE   ApplicationId = @ApplicationId;
                SELECT  @@ROWCOUNT;", new 
                {   
                    SubmittedDate = DateTime.UtcNow,
                    UserUpn = upn,
                    ApplicationId = applicationId
                }, _transaction);
            return ApplicationStatus.Completed;            
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception raised when attempting to submit an application in ApplicationRepository::CheckAndCompleteApplication");
            throw;
        }        
    }


    /// <summary>
    /// Check if an application has already been submitted
    /// </summary>
    /// <param name="applicationId">The application identifier</param>
    /// <returns>
    ///     true if completed, 
    ///     false if in progress, 
    ///     null if application or associated tasks do not exist 
    /// </returns>
    public async Task<bool?> IsApplicationSubmitted(Guid applicationId) =>
        await _connection.QuerySingleOrDefaultAsync<bool?>(@"
            SELECT CONVERT(BIT, 
                       CASE WHEN a.SubmittedDate IS NULL
                       THEN 0 ELSE 1 END) AS [Submitted]
            FROM   [recognitionCitizen].[Application] AS a                            
            WHERE  ApplicationId = @ApplicationId 
            AND    EXISTS ( SELECT * 
                            FROM   [recognitionCitizen].[TaskStatus] AS ts 
                            WHERE  ts.ApplicationId = a.ApplicationId);", 
            new { applicationId }, _transaction);

    private async Task<int> CheckApplicationForPendingTasks(Guid applicationId) =>
        await _connection.QuerySingleAsync<int>(@"
            SELECT COUNT(*) AS [Count]
            FROM   [recognitionCitizen].[Task] AS t           
            JOIN   [recognitionCitizen].[TaskStatus] ts ON ts.TaskId = t.TaskId
            WHERE  ts.[Status] <> @CompletedStatus
            AND    ts.ApplicationId = @ApplicationId
            AND    NOT EXISTS ( 
                       SELECT * 
                       FROM [recognitionCitizen].[StageTask] st 
                       WHERE st.TaskId = t.TaskId 
                       AND st.Enabled = 1 
                       AND st.StageId NOT IN @ExcludedStages);",
            new
            {
                completedStatus = (int)TaskStatusEnum.Completed,
                excludedStages = new int
                [
                    (int)TaskStage.Information,
                    (int)TaskStage.Declare,
                    (int)TaskStage.Review
                ],
                applicationId
            }, _transaction);

    
    

}
