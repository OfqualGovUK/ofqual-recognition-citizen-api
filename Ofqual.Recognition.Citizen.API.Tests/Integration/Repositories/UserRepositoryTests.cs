using Ofqual.Recognition.Citizen.Tests.Integration.Fixtures;
using Xunit;

namespace Ofqual.Recognition.Citizen.Tests.Integration.Repositories;

public class UserRepositoryTests : IClassFixture<SqlTestFixture>
{
    private readonly SqlTestFixture _fixture;

    public UserRepositoryTests(SqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateUser_Should_Insert_And_Return_User()
    {
        // Initialise test container and connection
        var unitOfWork = await _fixture.InitNewTestDatabaseContainer();

        // Arrange
        var oid = Guid.NewGuid().ToString();
        var email = "test@ofqual.gov.uk";
        var displayName = "Test User";

        // Act
        var user = await unitOfWork.UserRepository.CreateUser(oid, displayName, email);
        unitOfWork.Commit();

        // Assert
        Assert.NotNull(user);
        Assert.Equal(Guid.Parse(oid), user!.B2CId);
        Assert.Equal(email, user.EmailAddress);
        Assert.Equal(displayName, user.DisplayName);
        Assert.Equal(email, user.CreatedByUpn);
        Assert.Equal(email, user.ModifiedByUpn);
        Assert.True(user.CreatedDate <= DateTime.UtcNow);
        Assert.True(user.ModifiedDate <= DateTime.UtcNow);

        // Clean up test container
        await _fixture.DisposeAsync();
    }
}
