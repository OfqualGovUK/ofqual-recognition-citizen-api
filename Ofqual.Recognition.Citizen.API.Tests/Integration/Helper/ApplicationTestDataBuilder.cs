
using Dapper;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Infrastructure;

namespace Ofqual.Recognition.Citizen.Tests.Integration.Helper;

public static class ApplicationTestDataBuilder
{
    public static async Task<Application> CreateTestApplication(UnitOfWork unitOfWork)
    {
        var application = new Application
        {
            ApplicationId = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        };

        await unitOfWork.Connection.ExecuteAsync(@"
            INSERT INTO [recognitionCitizen].[Application]
            (ApplicationId, CreatedDate, ModifiedDate, CreatedByUpn)
            VALUES (@ApplicationId, @CreatedDate, @ModifiedDate, @CreatedByUpn);",
            application,
            unitOfWork.Transaction);

        return application;
    }
}