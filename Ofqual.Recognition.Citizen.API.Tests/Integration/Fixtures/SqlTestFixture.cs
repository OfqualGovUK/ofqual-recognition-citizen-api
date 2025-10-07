using Ofqual.Recognition.Citizen.Tests.Integration.Utils;
using Microsoft.Extensions.Configuration;
using Xunit;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.Tests.Integration.Builders;

namespace Ofqual.Recognition.Citizen.Tests.Integration.Fixtures;

public class SqlTestFixture : IAsyncLifetime
{
    private IConfiguration _config = null!;
    private ContainerBootstrapper _containerBootstrapper = null!;

    public async Task InitializeAsync()
    {
        _config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.Test.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.Test.Development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        _containerBootstrapper = new ContainerBootstrapper(_config);
    }

    public async Task<UnitOfWork> InitNewTestDatabaseContainer()
    {
        var connection = await _containerBootstrapper.InitDbContainer();
        var unitOfWork = new UnitOfWork(connection);

        await KeyValueTestDataBuilder.PopulateKeyValue(unitOfWork);

        return unitOfWork;
    }

    public async Task DisposeAsync()
    {
        await _containerBootstrapper.DisposeAsync();
    }
}