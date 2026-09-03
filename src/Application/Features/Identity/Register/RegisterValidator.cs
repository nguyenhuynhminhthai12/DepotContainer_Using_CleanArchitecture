
using FluentValidation;
namespace TechSpherex.CleanArchitecture.Application.Features.Identity.Register;

// Copyright (c) 2026 TechSpherex
/// <summary>
/// Validator cho <see cref="RegisterCommand"/> — xác thực thông tin đăng ký người dùng.
/// </summary>
public sealed class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(6);
    }
}
