using CorrelationId;
using CorrelationId.DependencyInjection;
using CorrelationId.HttpClient;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Middleware;
using Ofqual.Recognition.Frontend.Infrastructure.Services;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Http;
using System.Data;
using System.Reflection;
using System.Security.Claims;

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
builder.Services.AddScoped<IDbConnection>(_ =>
    builder.Configuration.GetValue<bool>("FeatureFlag:UseSQLManagedIdentity")
    ? new SqlConnection(builder.Configuration.GetConnectionString("OfqualODSManaged"))
    : new SqlConnection(builder.Configuration.GetConnectionString("OfqualODS")));

builder.Services.AddHttpContextAccessor();

// Register application services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ITaskStatusService, TaskStatusService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IApplicationAnswersService, ApplicationAnswersService>();
builder.Services.AddScoped<IStageService, StageService>();
builder.Services.AddSingleton<IFeatureFlagService, FeatureFlagService>();
builder.Services.AddTransient<IUserInformationService, UserInformationService>();
builder.Services.AddTransient<IApplicationService, ApplicationService>();
builder.Services.AddTransient<IAttachmentService, AttachmentService>();

// Register Gov UK Notify service
builder.Services.Configure<GovUkNotifyConfiguration>(builder.Configuration.GetSection("GovUkNotify"));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<GovUkNotifyConfiguration>>().Value);
builder.Services.AddScoped<IGovUkNotifyService, GovUkNotifyService>();

// Register AntiVirus service
builder.Services.Configure<AntiVirusConfiguration>(builder.Configuration.GetSection("AntiVirus"));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<AntiVirusConfiguration>>().Value);
builder.Services.AddScoped<IAntiVirusService, AntiVirusService>();

// Register Azure blob storage service 
builder.Services.Configure<AzureBlobStorageConfiguration>(builder.Configuration.GetSection("AzureBlobStorage"));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<AzureBlobStorageConfiguration>>().Value);
builder.Services.AddScoped<IAzureBlobStorageService, AzureBlobStorageService>();

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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(options =>
    {
        builder.Configuration.Bind("AzureAdB2C", options);
        // Refer to https://learn.microsoft.com/en-us/entra/identity-platform/claims-validation for validation parameters
        options.TokenValidationParameters = new TokenValidationParameters
        {
            SaveSigninToken = true,
            NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" // This is the object id of the user
        };
        options.Events = new JwtBearerEvents()
        {
            OnTokenValidated = (context) =>
            {
                IEnumerable<Claim> oid = context.Principal!.Claims.Where(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
                if (oid.Count() != 1)
                {
                    context.Fail("Invalid Token: No name identifier present");
                }
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = (context) =>
            {
                Console.WriteLine($"[AUTH FAILED] {context.Exception.Message}");
                if (context.Exception is SecurityTokenInvalidAudienceException)
                    Console.WriteLine("[AUDIENCE ERROR] Check if the audience is correct.");
                else if (context.Exception is SecurityTokenInvalidIssuerException)
                    Console.WriteLine("[ISSUER ERROR] Check the token issuer.");
                else if (context.Exception is SecurityTokenInvalidSignatureException)
                    Console.WriteLine("[SIGNATURE ERROR] Token signature could not be validated.");
                else if (context.Exception is SecurityTokenExpiredException)
                    Console.WriteLine("[EXPIRED] The token is expired.");
                return Task.CompletedTask;
            }
        };
    },
    options => { builder.Configuration.Bind("AzureAdB2C", options); });

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
        dc.Set("UserAgent", hc.Request.Headers.UserAgent.ToString());
    };
});
app.UseCors("CORS_POLICY");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<CheckApplicationIdMiddleware>(); // This MUST come after UseAuthentication and UseAuthorization middlewares, as the checks need a logged in user!
app.UseMiddleware<PreventReadOnlyEditMiddleware>(); // This MUST come after CheckApplicationIdMiddleware, as you need to check the user can access in the first place before checking if they can edit!
app.MapControllers();

#endregion

app.Run();