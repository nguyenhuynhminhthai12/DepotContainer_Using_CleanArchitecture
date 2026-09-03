namespace TechSpherex.CleanArchitecture.Application.Abstractions.Tenancy;

/// <summary>
/// Cung cấp bối cảnh tenant hiện tại cho request đang xử lý.
/// Được giải quyết từ HTTP headers, JWT claims, hoặc subdomain.
/// </summary>
public interface ITenantProvider
{
    /// <summary>ID tenant hiện tại (null nếu là thao tác hệ thống/toàn cục).</summary>
    string? TenantId { get; }

    /// <summary>Thông tin metadata của tenant hiện tại.</summary>
    TenantInfo? CurrentTenant { get; }
}
