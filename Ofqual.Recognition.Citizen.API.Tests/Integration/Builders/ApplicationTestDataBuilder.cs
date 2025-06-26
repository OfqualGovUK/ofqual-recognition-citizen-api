using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Dapper;

namespace Ofqual.Recognition.Citizen.Tests.Integration.Builders;

public static class ApplicationTestDataBuilder
{
    public static async Task<Application> CreateTestApplication(UnitOfWork unitOfWork, Application application)
    {
        await unitOfWork.Connection.ExecuteAsync(@"
            INSERT INTO [recognitionCitizen].[Application]
            (ApplicationId, OwnerUserId, SubmittedDate, ApplicationReleaseDate, OrganisationId, CreatedDate, ModifiedDate, CreatedByUpn, ModifiedByUpn)
            VALUES (@ApplicationId, @OwnerUserId, @SubmittedDate, @ApplicationReleaseDate, @OrganisationId, @CreatedDate, @ModifiedDate, @CreatedByUpn, @ModifiedByUpn);",
            application,
            unitOfWork.Transaction);

        return application;
    }
}