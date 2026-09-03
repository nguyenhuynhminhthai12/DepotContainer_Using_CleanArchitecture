
using FluentValidation;
namespace TechSpherex.CleanArchitecture.Application.Features.Identity.Login;

/// <summary>
/// Validator cho <see cref="LoginCommand"/> — xác thực email và mật khẩu khi đăng nhập.
/// </summary>
public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}
