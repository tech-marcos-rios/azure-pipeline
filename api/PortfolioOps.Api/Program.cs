using System.Text.Json.Serialization;
using Docker.DotNet;
using Microsoft.AspNetCore.HttpOverrides;
using PortfolioOps.Api.Configuration;
using PortfolioOps.Api.Endpoints;
using PortfolioOps.Api.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, config) => config
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Services.Configure<List<MonitoredProjectOptions>>(builder.Configuration.GetSection("MonitoredProjects"));
    builder.Services.Configure<ProjectSecretsOptions>(builder.Configuration.GetSection("ProjectSecrets"));

    var dockerHost = builder.Configuration["DockerHost"] ?? "unix:///var/run/docker.sock";
    builder.Services.AddSingleton<IDockerClient>(_ => new DockerClientConfiguration(new Uri(dockerHost)).CreateClient());

    builder.Services.AddHttpClient(nameof(HttpHealthCheckService), client => client.Timeout = TimeSpan.FromSeconds(5));

    builder.Services.AddSingleton<IHealthCheckService, HttpHealthCheckService>();
    builder.Services.AddSingleton<IDockerStatusService, DockerStatusService>();
    builder.Services.AddSingleton<IDatabaseStatusService, PostgresStatusService>();
    builder.Services.AddSingleton<IDiskStatusService, DiskStatusService>();
    builder.Services.AddSingleton<IStatusAggregationService, StatusAggregationService>();

    var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? "http://localhost:3000").Split(',');
    builder.Services.AddCors(options =>
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()));

    // Enums como string ("Healthy", no 0) en el JSON — el frontend no debería
    // tener que conocer el orden de declaración del enum en C#.
    builder.Services.ConfigureHttpJsonOptions(options =>
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
        options.SwaggerDoc("v1", new() { Title = "Portfolio Ops API", Version = "v1" }));

    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // Mismo motivo que en el resto del portfolio: Caddy corre en el mismo
    // host y le habla por localhost, y el puerto de esta API está atado a
    // 127.0.0.1 — nadie le pega directo sin pasar por Caddy primero.
    var forwardedHeadersOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    };
    forwardedHeadersOptions.KnownNetworks.Clear();
    forwardedHeadersOptions.KnownProxies.Clear();
    app.UseForwardedHeaders(forwardedHeadersOptions);

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Portfolio Ops API v1"));
    }

    app.UseSerilogRequestLogging();
    app.UseCors();
    app.MapHealthChecks("/health");
    app.MapStatusEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
