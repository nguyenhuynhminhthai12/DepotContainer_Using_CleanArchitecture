/// <summary>
/// Điểm khởi chạy chính (Entry Point) của Web API Quản lý Bãi Container Depot (TechSpherex Clean Architecture).
/// Cấu hình Aspire Service Defaults, Serilog, PostgreSQL, Redis HybridCache, gRPC, Scalar OpenAPI và Multi-tenancy.
/// </summary>
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
#pragma warning disable S1075 // URL liên hệ OpenAPI
    const string techSpherexContactUrl = "https://TechSpherex.com";
#pragma warning restore S1075 // URL liên hệ OpenAPI
    var builder = WebApplication.CreateBuilder(args);

    // Các thiết lập mặc định của Aspire service (OpenTelemetry, health check, service discovery)
    builder.AddServiceDefaults();

    // Cấu hình ghi log Serilog
    builder.Host.UseSerilog((context, loggerConfiguration) =>
        loggerConfiguration.ReadFrom.Configuration(context.Configuration));

    // Cơ chế dự phòng khi chạy local dev: đọc chuỗi kết nối từ appsettings.Development.json khi
    // các sidecar khám phá dịch vụ của Aspire không có sẵn. Điều này cho phép chạy trực tiếp API +
    // Postgres docker + Redis docker (phục vụ smoke test qua Postman / curl).
    var dbConn = builder.Configuration.GetConnectionString("TechSpherex-db");
    var cacheConn = builder.Configuration.GetConnectionString("TechSpherex-cache");
    if (!string.IsNullOrWhiteSpace(dbConn) && !string.IsNullOrWhiteSpace(cacheConn))
    {
        builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(dbConn));
        builder.Services.AddStackExchangeRedisCache(o => o.Configuration = cacheConn);
    }
    else
    {
        // PostgreSQL được quản lý bởi Aspire
        builder.AddNpgsqlDbContext<AppDbContext>("TechSpherex-db");

        // Redis được quản lý bởi Aspire (cho bộ nhớ đệm HybridCache L2)
        builder.AddRedisDistributedCache("TechSpherex-cache");
    }

    // Đăng ký các dịch vụ Application & Infrastructure (bao gồm HybridCache, CORS, RuleEngine)
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Đăng ký các dịch vụ gRPC
    builder.Services.AddGrpc();

    // Xử lý ngoại lệ toàn cục (Global exception handling)
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    // Cấu hình OpenAPI kèm cơ chế xác thực JWT Bearer
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
#pragma warning disable S1075 // URL liên hệ OpenAPI
                Url = new Uri(techSpherexContactUrl)
#pragma warning restore S1075 // URL liên hệ OpenAPI
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
                [schemeReference] = []
            };

            document.Security ??= [];
            document.Security.Add(securityRequirement);
            return Task.CompletedTask;
        });
    });

    // Đăng ký ProblemDetails
    builder.Services.AddProblemDetails();

    var app = builder.Build();

    // Middleware xử lý ngoại lệ toàn cục
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

    // Middleware Multi-tenant (đặt trước xác thực để ngữ cảnh tenant khả dụng)
    app.UseMiddleware<TenantMiddleware>();

    // Cấu hình CORS (trước khi xác thực)
    app.UseCors();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseSerilogRequestLogging();

    // Ánh xạ các REST endpoint
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

    // Ánh xạ các dịch vụ gRPC
    app.MapGrpcService<TodoGrpcService>();
    app.MapGrpcService<ContainerGrpcService>();
    app.MapGrpcService<YardGrpcService>();

    // Các endpoint mặc định của Aspire (health, alive)
    app.MapDefaultEndpoints();

    // Khởi tạo dữ liệu mẫu (seed database) trong môi trường Development
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

