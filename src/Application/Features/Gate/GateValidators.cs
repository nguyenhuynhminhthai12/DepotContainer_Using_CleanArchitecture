using FluentValidation;
using TechSpherex.CleanArchitecture.Domain.Entities;

namespace TechSpherex.CleanArchitecture.Application.Features.Gate;

public sealed class GateInContainerValidator : AbstractValidator<GateInContainerCommand>
{
    public GateInContainerValidator()
    {
        RuleFor(x => x.ContainerNumber).NotEmpty().Length(11);
        RuleFor(x => x.LineOperatorId).NotEqual(Guid.Empty);
        RuleFor(x => x.BlockId).NotEqual(Guid.Empty);
        RuleFor(x => x.Classification).NotEmpty().MaximumLength(10);
        RuleFor(x => x.ConditionAtGateIn)
            .Must(c => Enum.TryParse<ContainerCondition>(c, out _))
            .WithMessage("ConditionAtGateIn must be a valid ContainerCondition.");

        When(x => x.Bay.HasValue && x.Row.HasValue && x.Tier.HasValue, () =>
        {
            RuleFor(x => x.Bay!.Value).GreaterThan(0);
            RuleFor(x => x.Row!.Value).GreaterThan(0);
            RuleFor(x => x.Tier!.Value).GreaterThan(0);
        });
    }
}

public sealed class GateOutContainerValidator : AbstractValidator<GateOutContainerCommand>
{
    public GateOutContainerValidator()
    {
        RuleFor(x => x.ContainerNumber).NotEmpty().Length(11);
        RuleFor(x => x.DeliveryOrderId).NotEqual(Guid.Empty);
        RuleFor(x => x.ConditionAtGateOut)
            .Must(c => Enum.TryParse<ContainerCondition>(c, out _))
            .WithMessage("ConditionAtGateOut must be a valid ContainerCondition.");
    }
}

public sealed class MoveContainerInYardValidator : AbstractValidator<MoveContainerInYardCommand>
{
    public MoveContainerInYardValidator()
    {
        RuleFor(x => x.ContainerNumber).NotEmpty().Length(11);
        RuleFor(x => x.NewBlockId).NotEqual(Guid.Empty);
        RuleFor(x => x.NewBay).GreaterThan(0);
        RuleFor(x => x.NewRow).GreaterThan(0);
        RuleFor(x => x.NewTier).GreaterThan(0);
    }
}