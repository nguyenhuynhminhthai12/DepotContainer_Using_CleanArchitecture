namespace TechSpherex.CleanArchitecture.Application.Abstractions.Tenancy;

/// <summary>
/// Đại diện cho metadata của một tenant.
/// </summary>
public sealed record TenantInfo
{
    /// <summary>ID duy nhất của tenant.</summary>
    public required string Id { get; init; }

    /// <summary>Tên hiển thị của tenant.</summary>
    public required string Name { get; init; }

    /// <summary>Chuỗi kết nối cơ sở dữ liệu (tuỳ chọn, dùng cho multi-tenant riêng biệt DB).</summary>
    public string? ConnectionString { get; init; }

    /// <summary>Cho biết tenant có đang hoạt động không.</summary>
    public bool IsActive { get; init; } = true;

    /// <summary>Tenant mặc định cho chế độ single-tenant hoặc fallback.</summary>
    public static TenantInfo Default => new()
    {
        Id = "default",
        Name = "Default Tenant",
        IsActive = true
    };
}
