using FluentValidation;
using TechSpherex.CleanArchitecture.Domain.Entities;

namespace TechSpherex.CleanArchitecture.Application.Features.Containers;

/// <summary>
/// Validator cho <see cref="CreateContainerCommand"/> — xác thực dữ liệu đầu vào khi tạo container.
/// </summary>
public sealed class CreateContainerValidator : AbstractValidator<CreateContainerCommand>
{
    public CreateContainerValidator()
    {
        RuleFor(x => x.ContainerNumber)
            .NotEmpty()
            .Length(11)
            .WithMessage("Container number must be exactly 11 characters.");

        RuleFor(x => x.ContainerTypeId).NotEqual(Guid.Empty);
        RuleFor(x => x.IsoCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.SizeFeet).Must(s => s is 20 or 40).WithMessage("SizeFeet must be 20 or 40.");
        RuleFor(x => x.MaxWeightKg).GreaterThan(0);
        RuleFor(x => x.TareWeightKg).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Owner).NotEmpty().MaximumLength(100);

        RuleFor(x => x.Condition)
            .Must(c => Enum.TryParse<ContainerCondition>(c, out _))
            .WithMessage("Condition phải là một ContainerCondition hợp lệ (Normal, Damaged, Dented, Twisted, Cracked, Leaking, Other).");
    }
}
