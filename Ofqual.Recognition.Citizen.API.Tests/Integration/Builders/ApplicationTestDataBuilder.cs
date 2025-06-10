using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Dapper;

namespace Ofqual.Recognition.Citizen.Tests.Integration.Builders;

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