namespace TechSpherex.CleanArchitecture.Domain.Common.Rules;

/// <summary>
/// A YardSlot can hold at most one container at a time.
/// </summary>
public sealed class YardSlotNotOccupiedRule : IBusinessRule
{
    private readonly bool _isOccupied;

    public YardSlotNotOccupiedRule(bool isOccupied)
    {
        _isOccupied = isOccupied;
    }

    public string RuleCode => "Yard.SlotNotOccupied";
    public string Message => "Yard slot is already occupied by another container.";
    public int Priority => 1;

    public bool IsBroken() => _isOccupied;
}