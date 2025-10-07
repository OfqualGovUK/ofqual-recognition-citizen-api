using Ofqual.Recognition.Citizen.API.Infrastructure;
using Dapper;
using System.Data;

namespace Ofqual.Recognition.Citizen.Tests.Integration.Builders;

public static class KeyValueTestDataBuilder
{
    public static async Task<int> PopulateKeyValue(UnitOfWork unitOfWork)
    {
        const string sql = @"[recognitionCitizen].[PopulateKeyValue]";

        var returnCode = await unitOfWork.Connection.ExecuteScalarAsync<int>(
            sql,
            commandType: CommandType.StoredProcedure,
            transaction: unitOfWork.Transaction);

        return returnCode;
    }
}