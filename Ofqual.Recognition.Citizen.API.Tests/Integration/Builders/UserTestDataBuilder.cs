using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Dapper;

namespace Ofqual.Recognition.Citizen.Tests.Integration.Builders;

public static class UserTestDataBuilder
{
    public static async Task<User> CreateTestUser(UnitOfWork unitOfWork, User user)
    {
        await unitOfWork.Connection.ExecuteAsync(@"
            INSERT INTO [recognitionCitizen].[RecognitionCitizenUser]
            (UserId, B2CId, EmailAddress, DisplayName, CreatedByUpn, ModifiedByUpn, CreatedDate, ModifiedDate)
            VALUES (@UserId, @B2CId, @EmailAddress, @DisplayName, @CreatedByUpn, @ModifiedByUpn, @CreatedDate, @ModifiedDate);",
            user,
            unitOfWork.Transaction);

        return user;
    }
}