var builder = DistributedApplication.CreateBuilder(args);

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

