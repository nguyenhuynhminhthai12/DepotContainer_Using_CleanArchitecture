var builder = DistributedApplication.CreateBuilder(args);

/// <summary>
/// Cấu hình Aspire orchestration cho toàn bộ hệ thống.
/// Khai báo các container infrastructure (PostgreSQL, Redis) và ánh xạ API project.
/// </summary>
var postgres = builder.AddPostgres("postgres")
    .WithImageTag("17.6")
    .WithPgAdmin()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("techspherex-postgres-data");

var database = postgres.AddDatabase("TechSpherex-db");

var redis = builder.AddRedis("TechSpherex-cache")
    .WithRedisInsight()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("techspherex-redis-data");

/// <summary>
/// Thêm API project với tham chiếu đến database và redis,
/// cấu hình HTTP/HTTPS endpoints.
/// </summary>
builder.AddProject<Projects.TechSpherex_CleanArchitecture_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(redis)
    .WaitFor(redis)
    .WithHttpEndpoint(port: 5200, name: "http")
    .WithHttpsEndpoint(port: 7200, name: "https")
    .WithExternalHttpEndpoints();

var app = builder.Build();
await app.RunAsync();
