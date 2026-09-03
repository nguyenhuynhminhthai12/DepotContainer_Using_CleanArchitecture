
using TechSpherex.CleanArchitecture.Domain.Entities;
namespace TechSpherex.CleanArchitecture.Application.Abstractions.Identity;

/// <summary>
/// Dịch vụ tạo và làm mới token JWT cho người dùng.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Tạo access token và refresh token cho người dùng đã cung cấp.
    /// </summary>
    /// <param name="user">Người dùng cần tạo token.</param>
    /// <param name="cancellationToken">Token hủy.</param>
    /// <returns>Đối tượng <see cref="TokenResponse"/> chứa access token, refresh token và thời gian hết hạn.</returns>
    Task<TokenResponse> GenerateTokenAsync(ApplicationUser user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Làm mới access token bằng refresh token cũ.
    /// </summary>
    /// <param name="accessToken">Access token hiện tại (sắp hết hạn).</param>
    /// <param name="refreshToken">Refresh token để cấp token mới.</param>
    /// <param name="cancellationToken">Token hủy.</param>
    /// <returns>Đối tượng <see cref="TokenResponse"/> mới.</returns>
    Task<TokenResponse> RefreshTokenAsync(string accessToken, string refreshToken, CancellationToken cancellationToken = default);
}

/// <summary>
/// Kết quả trả về chứa token JWT.
/// </summary>
/// <param name="AccessToken">Access token JWT.</param>
/// <param name="RefreshToken">Refresh token dùng để làm mới.</param>
/// <param name="ExpiresAt">Thời điểm access token hết hạn.</param>
public sealed record TokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
