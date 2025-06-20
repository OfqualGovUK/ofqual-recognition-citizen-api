using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.Tests.Integration.Fixtures;
using Xunit;

namespace Ofqual.Recognition.Citizen.API.Tests.Integration.Repositories;

public class ApplicationRepositoryTests : IClassFixture<SqlTestFixture>
{
    private readonly SqlTestFixture _fixture;

    public ApplicationRepositoryTests(SqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateApplication_WhenAuthorizedCorrectly__ShouldCreateApplicationWithUser()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Set up UserInformationService mock
        string upn = "test@test.com";
        string oid = Guid.NewGuid().ToString();
        string displayName = "Test Name";

        // Act

        Application? value = await unitOfWork.ApplicationRepository.CreateApplication(oid, displayName, upn);

        unitOfWork.Commit();

        // Assert

        Assert.True(value != null);
        Assert.Equal(upn, value.CreatedByUpn);
        Assert.Equal(upn, value.ModifiedByUpn);
    }
}
