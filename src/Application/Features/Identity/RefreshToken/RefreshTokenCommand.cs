
using TechSpherex.CleanArchitecture.Application.Abstractions.Identity;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Application.Features.Identity.RefreshToken;

/// <summary>
/// Lệnh làm mới access token bằng refresh token. Trả về <see cref="TokenResponse"/> mới.
/// </summary>
/// <param name="AccessToken">Access token hiện tại (sắp hết hạn).</param>
/// <param name="RefreshToken">Refresh token để cấp token mới.</param>
public sealed record RefreshTokenCommand(string AccessToken, string RefreshToken) : ICommand<Result<TokenResponse>>;
