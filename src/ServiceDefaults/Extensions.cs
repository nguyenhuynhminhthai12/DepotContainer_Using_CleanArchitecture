using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
namespace TechSpherex.CleanArchitecture.ServiceDefaults;

/// <summary>
/// Lớp tiện ích cấu hình các dịch vụ mặc định cho tất cả microservice trong hệ thống Aspire.
/// Bao gồm OpenTelemetry (logging, metrics, tracing), health checks và service discovery.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Thêm tất cả dịch vụ mặc định vào builder: OpenTelemetry, health checks, service discovery.
    /// </summary>
    /// <param name="builder">Host application builder.</param>
    /// <returns>Builder sau khi đã đăng ký.</returns>
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();
        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        return builder;
    }

    /// <summary>
    /// Cấu hình OpenTelemetry: logging, metrics (ASP.NET, HTTP client, runtime) và tracing.
    /// </summary>
    /// <param name="builder">Host application builder.</param>
    /// <returns>Builder sau khi đã đăng ký.</returns>
    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    /// <summary>
    /// Thêm exporter OpenTelemetry (OTLP) nếu biến môi trường OTEL_EXPORTER_OTLP_ENDPOINT được đặt.
    /// </summary>
    /// <param name="builder">Host application builder.</param>
    /// <returns>Builder sau khi đã đăng ký.</returns>
    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    /// <summary>
    /// Thêm health check mặc định kiểm tra service đang chạy ("self").
    /// </summary>
    /// <param name="builder">Host application builder.</param>
    /// <returns>Builder sau khi đã đăng ký.</returns>
    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    /// <summary>
    /// Ánh xạ các endpoint health check mặc định (/health và /alive).
    /// </summary>
    /// <param name="app">Web application.</param>
    /// <returns>App sau khi đã đăng ký endpoint.</returns>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health");

        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        return app;
    }
}
