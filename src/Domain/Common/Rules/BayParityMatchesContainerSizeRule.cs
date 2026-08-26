namespace TechSpherex.CleanArchitecture.Domain.Common.Rules;

/// <summary>
/// Validates that the slot's Bay parity matches the container size.
/// Odd Bays (1, 3, 5, …) host 20 ft containers.
/// Even Bays (2, 4, 6, …) host 40 ft containers.
/// </summary>
public sealed class BayParityMatchesContainerSizeRule : IBusinessRule
{
    private readonly int _bay;
    private readonly int _containerSizeFeet;

    public BayParityMatchesContainerSizeRule(int bay, int containerSizeFeet)
    {
        _bay = bay;
        _containerSizeFeet = containerSizeFeet;
    }

    public string RuleCode => "Yard.BayParityMatchesContainerSize";
    public string Message => "Odd Bays host 20 ft containers; even Bays host 40 ft containers.";
    public int Priority => 1;

    public bool IsBroken()
    {
        var bayIsOdd = _bay % 2 != 0;
        var sizeIs20 = _containerSizeFeet == 20;
        return bayIsOdd != sizeIs20;
    }
}