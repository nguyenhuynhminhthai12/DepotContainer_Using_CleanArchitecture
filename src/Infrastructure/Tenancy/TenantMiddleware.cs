
using TechSpherex.CleanArchitecture.Application.Abstractions.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// Middleware xác thực và đặt ngữ cảnh tenant cho mỗi yêu cầu.
/// phải được đăng ký trước middleware xác thực để ngữ cảnh tenant có sẵn sớm.
/// </summary>
namespace TechSpherex.CleanArchitecture.Infrastructure.Tenancy;
public sealed class TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<TenantMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context, ITenantProvider tenantProvider)
    {
        var tenantId = tenantProvider.TenantId;
        var tenant = tenantProvider.CurrentTenant;

        if (tenant is { IsActive: false })
        {
            _logger.LogWarning("Tenant {TenantId} is inactive. Rejecting request.", tenantId);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                title = "Forbidden",
                status = 403,
                detail = $"Tenant '{tenantId}' is not active."
            });
            return;
        }

        // Phù ngữ cảnh log Serilog bằng thông tin tenant
        using (Serilog.Context.LogContext.PushProperty("TenantId", tenantId))
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Request for tenant: {TenantId}", tenantId);
            }
            await _next(context);
        }
    }
}
