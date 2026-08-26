using FluentValidation;

namespace TechSpherex.CleanArchitecture.Application.Features.Yard;

public sealed class CreateBlockValidator : AbstractValidator<CreateBlockCommand>
{
    public CreateBlockValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.DepotId)
            .NotEqual(Guid.Empty);

        When(x => !x.IsVirtual, () =>
        {
            RuleFor(x => x.MaxBay).NotNull().GreaterThan(0)
                .WithMessage("MaxBay is required for a non-virtual block.");
            RuleFor(x => x.MaxRow).NotNull().GreaterThan(0)
                .WithMessage("MaxRow is required for a non-virtual block.");
            RuleFor(x => x.MaxTier).NotNull().GreaterThan(0)
                .WithMessage("MaxTier is required for a non-virtual block.");
        }).Otherwise(() =>
        {
            RuleFor(x => x.MaxBay).Null();
            RuleFor(x => x.MaxRow).Null();
            RuleFor(x => x.MaxTier).Null();
        });
    }
}

public sealed class CreateVirtualBlockValidator : AbstractValidator<CreateVirtualBlockCommand>
{
    public CreateVirtualBlockValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DepotId).NotEqual(Guid.Empty);
    }
}

public sealed class ResizeBlockValidator : AbstractValidator<ResizeBlockCommand>
{
    public ResizeBlockValidator()
    {
        RuleFor(x => x.BlockId).NotEqual(Guid.Empty);
        RuleFor(x => x.MaxBay).GreaterThan(0);
        RuleFor(x => x.MaxRow).GreaterThan(0);
        RuleFor(x => x.MaxTier).GreaterThan(0);
    }
}