
using FluentValidation;
namespace TechSpherex.CleanArchitecture.Application.Features.Identity.RefreshToken;

// Copyright (c) 2026 TechSpherex
/// <summary>
/// Validator cho <see cref="RefreshTokenCommand"/> — yêu cầu access token và refresh token không rỗng.
/// </summary>
public sealed class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.AccessToken)
            .NotEmpty()
            .WithMessage("Access token is required.");

        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required.");
    }
}
