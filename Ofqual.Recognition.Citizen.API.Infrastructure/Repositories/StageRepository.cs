using Dapper;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Models.StageStatus;
using Ofqual.Recognition.Citizen.API.Core.Models.StageTask;
using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories
{
    public class StageRepository : IStageRepository
    {
        private readonly IDbConnection _connection;
        private readonly IDbTransaction _transaction;

        public StageRepository(IDbConnection connection, IDbTransaction transaction)
        {
            _connection = connection;
            _transaction = transaction;
        }

        public async Task<bool> UpsertStageStatusRecord(Guid applicationId, StageStatus stageStatus)
        {
            try
            {
                var query = @"
                MERGE [recognitionCitizen].[StageStatus] AS target
                USING (SELECT @applicationId AS ApplicationId, @StageId AS StageId) AS source
                    ON target.ApplicationId = source.ApplicationId
                WHEN MATCHED THEN
                     UPDATE SET
                        ModifiedByUpn = @ModifiedByUpn,
                        ModifiedDate = GETDATE()
                WHEN NOT MATCHED THEN
                    INSERT (
                        ApplicationId,
                        StageId,
                        StatusId,
                        StageStartDate,
                        StageCompletionDate,
                        CreatedDate,
                        ModifiedDate,
                        CreatedByUpn,
                        ModifiedByUpn
                    )
                    VALUES(
                        @ApplicationId,
                        @StageId,
                        @StatusId,
                        @StageStartDate,
                        @StageCompletionDate,
                        GETDATE(),
                        GETDATE(),
                        @CreatedByUpn,
                        @ModifiedByUpn
                        );";

                var rowsAffected = await _connection.ExecuteAsync(query, new 
                {
                    applicationId,
                    stageStatus.StageId,
                    stageStatus.StatusId,
                    stageStatus.StageStartDate,
                    stageStatus.StageCompletionDate,
                    stageStatus.CreatedDate,
                    stageStatus.ModifiedDate,
                    stageStatus.CreatedByUpn,
                    stageStatus.ModifiedByUpn
                }, _transaction);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating task status for ApplicationId: {ApplicationId}, StageStatus: {StageStatus}", applicationId, stageStatus);
                return false;
            }
        }

        public async Task<IEnumerable<StageTask>> GetAllStageTasksByStage(StageEnum stage)
        {
            const string query =
                @"SELECT 
                    StageId,
                    StageName,
                    TaskId,
                    Task,
                    OrderNumber
                  FROM [recognitionCitizen].[v_StageTask]
                  WHERE StageId = @Stage";

            return await _connection.QueryAsync<StageTask>(query, new
            {
                Stage = stage
            }, _transaction);
        }
    }
}