namespace TechSpherex.CleanArchitecture.Domain.Common.Rules;

/// <summary>
/// A Delivery Order must still be valid (not expired) to authorise a Gate Out.
/// </summary>
public sealed class DeliveryOrderNotExpiredRule : IBusinessRule
{
    private readonly DateTimeOffset _expiryDate;
    private readonly DateTimeOffset _now;

    public DeliveryOrderNotExpiredRule(DateTimeOffset expiryDate, DateTimeOffset now)
    {
        _expiryDate = expiryDate;
        _now = now;
    }

    public string RuleCode => "DeliveryOrder.NotExpired";
    public string Message => "Delivery order has expired and cannot authorise Gate Out.";
    public int Priority => 1;

    public bool IsBroken() => _expiryDate < _now;
}