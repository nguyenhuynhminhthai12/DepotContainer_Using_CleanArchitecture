
using System.Security.Claims;
using TechSpherex.CleanArchitecture.Application.Abstractions.Identity;
using Microsoft.AspNetCore.Http;
namespace TechSpherex.CleanArchitecture.Infrastructure.Identity;

/// <summary>
/// Triển khai <see cref="ICurrentUser"/> lấy thông tin người dùng từ HttpContext hiện tại.
/// </summary>
/// <param name="httpContextAccessor">Accessor để truy cập HttpContext.</param>
public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    /// <inheritdoc/>
    public string? UserId => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <inheritdoc/>
    public string? Email => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

    /// <inheritdoc/>
    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
