
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Application.Features.Identity.Register;

/// <summary>
/// Lệnh đăng ký tài khoản người dùng mới. Trả về <see cref="Result"/> (không mang giá trị).
/// </summary>
/// <param name="FirstName">Họ của người dùng.</param>
/// <param name="LastName">Tên của người dùng.</param>
/// <param name="Email">Email đăng ký (đồng thời là tên đăng nhập).</param>
/// <param name="Password">Mật khẩu đăng ký.</param>
public sealed record RegisterCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password) : ICommand;
