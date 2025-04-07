using Docker.DotNet;
using Docker.DotNet.BasicAuth;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Ofqual.Recognition.Citizen.Tests.Integration.Utils;

public class ContainerBootstrapper : IAsyncDisposable
{
    private readonly IConfiguration _config;
    private MemoryStream? _containerStdin;
    private MemoryStream? _containerStdout;
    private IOutputConsumer? _containerOutputConsumer;
    public IContainer? DbContainer { get; private set; }

    public ContainerBootstrapper(IConfiguration config)
    {
        _config = config;
    }

    public async Task<SqlConnection> InitDbContainer()
    {
        await PullContainer();

        _containerStdin = new MemoryStream();
        _containerStdout = new MemoryStream();
        _containerOutputConsumer = Consume.RedirectStdoutAndStderrToStream(_containerStdin, _containerStdout);

        var imageName = $"{Get("RegistryEndpoint")}/{Get("ImagePath")}:latest";

        DbContainer = new ContainerBuilder()
            .WithImage(imageName)
            .WithPortBinding(1433, true)
            .WithOutputConsumer(_containerOutputConsumer)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilPortIsAvailable(1433))
            .WithCleanUp(true)
            .Build();

        await DbContainer.StartAsync();

        var mappedPort = DbContainer.GetMappedPublicPort(1433);
        var sqlConnectionStr = CreateSqlConnectionStr(mappedPort);

        await WaitForSqlServerReady(sqlConnectionStr);

        return new SqlConnection(sqlConnectionStr);
    }

    private string CreateSqlConnectionStr(int port)
    {
        var connStr = $"Server=localhost,{port};Initial Catalog={Get("DatabaseName")};" +
                      $"User ID={Get("SqlUsername")};Password={Get("SqlPassword")};TrustServerCertificate=True;";

        return connStr;
    }

    private async Task WaitForSqlServerReady(string connectionString, int maxRetries = 10, int initialDelayMs = 500)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                return;
            }
            catch (SqlException)
            {
                if (attempt == maxRetries)
                {
                    throw;
                }

                await Task.Delay(initialDelayMs * attempt);
            }
        }
    }

    private async Task PullContainer()
    {
        var dockerClient = new DockerClientConfiguration(
            credentials: new BasicAuthCredentials(Get("RegistryUsername"), Get("RegistryPassword")))
            .CreateClient();

        var fullImage = $"{Get("RegistryEndpoint")}/{Get("ImagePath")}";
        var tag = "latest";

        await dockerClient.Images.CreateImageAsync(
            new ImagesCreateParameters
            {
                FromImage = fullImage,
                Tag = tag
            },
            new AuthConfig
            {
                Username = Get("RegistryUsername"),
                Password = Get("RegistryPassword")
            },
            new Mock<IProgress<JSONMessage>>().Object
        );
    }

    private string Get(string key) => _config[$"TestSettings:{key}"]!;

    public async ValueTask DisposeAsync()
    {
        _containerOutputConsumer?.Dispose();
        if (_containerStdin is not null)
        {
            await _containerStdin.DisposeAsync();
        }

        if (_containerStdout is not null)
        {
            await _containerStdout.DisposeAsync();
        }

        if (DbContainer is not null)
        {
            await DbContainer.StopAsync();
            await DbContainer.DisposeAsync();
            DbContainer = null;
        }

        _containerStdin = null;
        _containerStdout = null;
        _containerOutputConsumer = null;

        GC.SuppressFinalize(this);
    }
}