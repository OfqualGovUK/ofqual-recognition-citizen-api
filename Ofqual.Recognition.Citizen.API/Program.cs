using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Models;
using CorrelationId.DependencyInjection;
using Microsoft.Extensions.Options;
using CorrelationId.HttpClient;
using Microsoft.Data.SqlClient;
using Serilog.Sinks.Http;
using System.Reflection;
using Serilog.Events;
using CorrelationId;
using System.Data;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

#region Services

// Add HttpClient service with Correlation ID forwarding
builder.Services.AddHttpClient<HttpClient>().AddCorrelationIdForwarding();

// Configure Serilog logging
builder.Host.UseSerilog((ctx, svc, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(svc)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Environment", ctx.Configuration.GetValue<string>("LogzIo:Environment") ?? "Unknown")
    .Enrich.WithProperty("Assembly", Assembly.GetEntryAssembly()?.GetName()?.Name ?? "Ofqual.Recognition.Citizen.API")
    .MinimumLevel.Override("CorrelationId", LogEventLevel.Error)
    .WriteTo.Console(
        restrictedToMinimumLevel: ctx.Configuration.GetValue<string>("LogzIo:Environment") == "LOCAL"
            ? LogEventLevel.Verbose
            : LogEventLevel.Error)
    .WriteTo.LogzIoDurableHttp(
        requestUri: ctx.Configuration.GetValue<string>("LogzIo:Uri") ?? string.Empty,
        bufferBaseFileName: "Buffer",
        bufferRollingInterval: BufferRollingInterval.Hour,
        bufferFileSizeLimitBytes: 524288000L,
        retainedBufferFileCountLimit: 12
    )
);

// Add Correlation ID service for tracking requests across logs
builder.Services.AddCorrelationId(opt =>
    {
        opt.AddToLoggingScope = true;
        opt.UpdateTraceIdentifier = true;
    }
).WithTraceIdentifierProvider();

// Register database connection
builder.Services.AddScoped<IDbConnection>(sp =>
    new SqlConnection(builder.Configuration.GetConnectionString("OfqualODS"))
);

// Register application services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ITaskStatusService, TaskStatusService>();
builder.Services.AddScoped<IApplicationAnswersService, ApplicationAnswersService>();
builder.Services.AddScoped<IStageService, StageService>();

// Register AntiVirus service
builder.Services.Configure<AntiVirusConfiguration>(builder.Configuration.GetSection("AntiVirus"));
builder.Services.AddSingleton<AntiVirusConfiguration>(sp =>
    sp.GetRequiredService<IOptions<AntiVirusConfiguration>>().Value);
builder.Services.AddScoped<IAntiVirusService, AntiVirusService>();

// Register Azure blob storage service 
var blobConnectionString = builder.Configuration.GetConnectionString("AzureBlobStorage")!;
builder.Services.AddSingleton<IAzureBlobStorageService>(new AzureBlobStorageService(blobConnectionString));

// Add controllers
builder.Services.AddControllers();

// Enable Swagger for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS Policy
builder.Services.AddCors(o => o.AddPolicy("CORS_POLICY", builder =>
{
    builder.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader();
}));

#endregion

var app = builder.Build();

#region Middleware

// Configure middleware and request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ofqual Recognition Citizen API V1");
    });
};

app.UseCorrelationId();
app.UseSerilogRequestLogging(opt =>
{
    opt.EnrichDiagnosticContext = (dc, hc) =>
    {
        dc.Set("RequestHost", hc.Request.Host.Value);
        dc.Set("RequestScheme", hc.Request.Scheme);
        dc.Set("UserAgent", hc.Request.Headers["User-Agent"].ToString());
    };
});
app.UseCors("CORS_POLICY");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

#endregion

app.Run();