using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Ofqual.Recognition.Citizen.Tests.Integration.Utils;
using Xunit;

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

    public Task<SqlConnection> InitNewTestDatabaseContainer()
    {
        return _containerBootstrapper.InitDbContainer();
    }

    public async Task DisposeAsync()
    {
        await _containerBootstrapper.DisposeAsync();
    }
}