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
                MERGE INTO recognitionCitizen.StageStatus AS target
                USING (SELECT @ApplicationId AS ApplicationId, @StageId AS StageId) AS Source
                    ON Target.ApplicationId = Source.ApplicationId
                WHEN MATCHED AND Target.StageId <> @StageId THEN
                    UPDATE SET
                        StageId = @StageId,
                        StatusId = @StatusId,
                        StageStartDate = @StageStartDate,
                        StageCompletionDate = @StageCompletionDate,
                        ModifiedByUpn = @ModifiedByUpn,
                        ModifiedDate = @ModifiedDate
                WHEN MATCHED THEN
                     UPDATE SET
                        ModifiedByUpn = @ModifiedByUpn,
                        ModifiedDate = @ModifiedDate
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
                        @CreatedDate,
                        @ModifiedDate,
                        @CreatedByUpn,
                        @ModifiedByUpn
                        )
                OUTPUT CASE WHEN $action = 'INSERT' THEN 1 ELSE 0 AS inserted";

                return await _connection.ExecuteScalarAsync<bool>(query, stageStatus, _transaction);
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