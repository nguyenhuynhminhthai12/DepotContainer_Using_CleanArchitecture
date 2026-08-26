namespace TechSpherex.CleanArchitecture.Domain.Common.Rules;

/// <summary>
/// A Delivery Order line can release at most its requested quantity.
/// </summary>
public sealed class DeliveryOrderQuantityAvailableRule : IBusinessRule
{
    private readonly int _requested;
    private readonly int _delivered;

    public DeliveryOrderQuantityAvailableRule(int requestedQuantity, int deliveredQuantity)
    {
        _requested = requestedQuantity;
        _delivered = deliveredQuantity;
    }

    public string RuleCode => "DeliveryOrder.QuantityAvailable";
    public string Message => "Delivery order has no remaining quantity for this container type.";
    public int Priority => 2;

    public bool IsBroken() => _delivered >= _requested;
}