
using TechSpherex.CleanArchitecture.Application.Abstractions.Identity;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Application.Features.Identity.Login;

/// <summary>
/// Lệnh đăng nhập — xác thực người dùng bằng email/password và trả về <see cref="TokenResponse"/>.
/// </summary>
/// <param name="Email">Email của người dùng.</param>
/// <param name="Password">Mật khẩu của người dùng.</param>
public sealed record LoginCommand(string Email, string Password) : ICommand<Result<TokenResponse>>;
