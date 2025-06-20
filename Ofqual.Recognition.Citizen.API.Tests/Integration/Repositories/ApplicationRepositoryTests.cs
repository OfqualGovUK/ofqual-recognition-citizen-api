using Moq;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories;
using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.Tests.Integration.Fixtures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Ofqual.Recognition.Citizen.API.Tests.Integration.Repositories;

public class ApplicationRepositoryTests : IClassFixture<SqlTestFixture>
{
    private readonly SqlTestFixture _fixture;
    private readonly Mock<IUserInformationService> _mockUserInformationService;

    public ApplicationRepositoryTests(SqlTestFixture fixture)
    {
        _fixture = fixture;
        _mockUserInformationService = new Mock<IUserInformationService>();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateApplication_ShouldCreateApplicationWithUser()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection, _mockUserInformationService.Object);

        // Set up UserInformationService mock
        string upn = "test@test.com";
        string oid = Guid.NewGuid().ToString();
        string displayName = "Test Name";

        _mockUserInformationService
            .Setup(_ => _.GetCurrentUserUpn())
            .Returns(upn);
        _mockUserInformationService
            .Setup(_ => _.GetCurrentUserObjectId())
            .Returns(oid);
        _mockUserInformationService
            .Setup(_ => _.GetCurrentUserDisplayName())
            .Returns(displayName);

        // Act

        Application? value = await unitOfWork.ApplicationRepository.CreateApplication();

        unitOfWork.Commit();

        // Assert

        Assert.True(value != null);
        Assert.Equal(upn, value.CreatedByUpn);
        Assert.Equal(upn, value.ModifiedByUpn);
    }
}
