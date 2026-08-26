using FluentValidation;

namespace TechSpherex.CleanArchitecture.Application.Features.DeliveryOrders;

public sealed class CreateDeliveryOrderValidator : AbstractValidator<CreateDeliveryOrderCommand>
{
    public CreateDeliveryOrderValidator()
    {
        RuleFor(x => x.OrderNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.CustomerId).NotEqual(Guid.Empty);
        RuleFor(x => x.LineOperatorId).NotEqual(Guid.Empty);
        RuleFor(x => x.ExpiryDate).GreaterThan(DateTimeOffset.UtcNow.AddDays(-1))
            .WithMessage("ExpiryDate must be in the future.");
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one container-type line is required.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ContainerTypeId).NotEqual(Guid.Empty);
            line.RuleFor(l => l.RequestedQuantity).GreaterThan(0);
            line.RuleFor(l => l.DeliveredQuantity).GreaterThanOrEqualTo(0);
        });
    }
}