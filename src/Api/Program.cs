using Scalar.AspNetCore;
using Serilog;
using TechSpherex.CleanArchitecture.Api.Endpoints;
using TechSpherex.CleanArchitecture.Api.Extensions;
using TechSpherex.CleanArchitecture.Api.GrpcServices;
using TechSpherex.CleanArchitecture.Application;
using TechSpherex.CleanArchitecture.Infrastructure;
using TechSpherex.CleanArchitecture.Infrastructure.Persistence;
using TechSpherex.CleanArchitecture.Infrastructure.Tenancy;
using TechSpherex.CleanArchitecture.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
#pragma warning disable S1075 // OpenAPI contact URL
    const string techSpherexContactUrl = "https://TechSpherex.com";
#pragma warning restore S1075 // OpenAPI contact URL
    var builder = WebApplication.CreateBuilder(args);

    // Aspire service defaults (OpenTelemetry, health checks, service discovery)
    builder.AddServiceDefaults();

    // Serilog
    builder.Host.UseSerilog((context, loggerConfiguration) =>
        loggerConfiguration.ReadFrom.Configuration(context.Configuration));

    // Local dev fallback: read connection strings from appsettings.Development.json when
    // the Aspire service-discovery sidecars are not available. This lets us run API +
    // dockerised Postgres + dockerised Redis directly (for Postman / curl smoke tests).
    var dbConn = builder.Configuration.GetConnectionString("TechSpherex-db");
    var cacheConn = builder.Configuration.GetConnectionString("TechSpherex-cache");
    if (!string.IsNullOrWhiteSpace(dbConn) && !string.IsNullOrWhiteSpace(cacheConn))
    {
        builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(dbConn));
        builder.Services.AddStackExchangeRedisCache(o => o.Configuration = cacheConn);
    }
    else
    {
        // Aspire-managed PostgreSQL
        builder.AddNpgsqlDbContext<AppDbContext>("TechSpherex-db");

        // Aspire-managed Redis (for HybridCache L2)
        builder.AddRedisDistributedCache("TechSpherex-cache");
    }

    // Application & Infrastructure (includes HybridCache, CORS, RuleEngine)
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // gRPC services
    builder.Services.AddGrpc();

    // Global exception handling
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    // OpenAPI with JWT Bearer security scheme
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            var info = document.Info ?? new Microsoft.OpenApi.OpenApiInfo();
            info.Title = "Container Depot Management API";
            info.Description = "A production-ready Clean Architecture system for managing container depots (Block / Bay / Row / Tier yard layout, Gate In/Out EIR, Delivery Orders, reports) — built on .NET 10 by TechSpherex.";
            info.Contact = new Microsoft.OpenApi.OpenApiContact
            {
                Name = "TechSpherex",
#pragma warning disable S1075 // OpenAPI contact URL
                Url = new Uri(techSpherexContactUrl)
#pragma warning restore S1075 // OpenAPI contact URL
            };
            document.Info = info;

            var components = document.Components ?? new Microsoft.OpenApi.OpenApiComponents();
            components.SecuritySchemes ??= new Dictionary<string, Microsoft.OpenApi.IOpenApiSecurityScheme>();
            components.SecuritySchemes["Bearer"] = new Microsoft.OpenApi.OpenApiSecurityScheme
            {
                Type = Microsoft.OpenApi.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Enter your JWT token"
            };

            document.Components = components;

            var schemeReference = new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer");
            var securityRequirement = new Microsoft.OpenApi.OpenApiSecurityRequirement
            {
                [schemeReference] = new List<string>()
            };

            document.Security ??= [];
            document.Security.Add(securityRequirement);
            return Task.CompletedTask;
        });
    });

    // ProblemDetails
    builder.Services.AddProblemDetails();

    var app = builder.Build();

    // Global exception handler
    app.UseExceptionHandler();
    app.UseStatusCodePages();
    app.UseHttpsRedirection();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("Container Depot Management API");
            options.WithTheme(ScalarTheme.BluePlanet);
            options.WithDefaultHttpClient(ScalarTarget.Shell, ScalarClient.Curl);
        });
    }

    // Multi-tenant middleware (before auth so tenant context is available)
    app.UseMiddleware<TenantMiddleware>();

    // CORS (before auth)
    app.UseCors();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseSerilogRequestLogging();

    // Map REST endpoints
    // Copyright (c) 2026 TechSpherex
    app.MapIdentityEndpoints();
    app.MapTodoEndpoints();
    app.MapAgentEndpoints();
    app.MapYardEndpoints();
    app.MapContainerEndpoints();
    app.MapGateEndpoints();
    app.MapMovementEndpoints();
    app.MapDeliveryOrderEndpoints();
    app.MapReportEndpoints();
    app.MapLookupEndpoints();

    // Map gRPC services
    app.MapGrpcService<TodoGrpcService>();
    app.MapGrpcService<ContainerGrpcService>();
    app.MapGrpcService<YardGrpcService>();

    // Aspire default endpoints (health, alive)
    app.MapDefaultEndpoints();

    // Seed database in development
    if (app.Environment.IsDevelopment())
    {
        await AppDbSeeder.SeedAsync(app.Services);
    }

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

