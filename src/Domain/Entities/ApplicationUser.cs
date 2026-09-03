
using Microsoft.AspNetCore.Identity;
namespace TechSpherex.CleanArchitecture.Domain.Entities;

/// <summary>
/// Người dùng hệ thống — kế thừ từ <see cref="IdentityUser"/> của ASP.NET Core Identity.
/// Lưu trữ thông tin cá nhân và token làm mới (refresh token) cho xác thực JWT.
/// </summary>
public sealed class ApplicationUser : IdentityUser
{
    /// <summary>Họ (tên đệm) của người dùng.</summary>
    public string FirstName { get; set; } = default!;

    /// <summary>Tên của người dùng.</summary>
    public string LastName { get; set; } = default!;

    /// <summary>Refresh token dùng để lấy access token mới.</summary>
    public string? RefreshToken { get; set; }

    /// <summary>Thời điểm hết hạn của refresh token.</summary>
    public DateTimeOffset? RefreshTokenExpiryTime { get; set; }
}
