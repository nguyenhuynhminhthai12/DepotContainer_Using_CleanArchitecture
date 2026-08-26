using FluentAssertions;
using TechSpherex.CleanArchitecture.Domain.Common.Rules;

namespace TechSpherex.CleanArchitecture.Application.UnitTests.Domain;

public sealed class BayParityMatchesContainerSizeRuleTests
{
    [Theory]
    [InlineData(1, 20)]   // odd bay, 20ft ✓
    [InlineData(3, 20)]   // odd bay, 20ft ✓
    [InlineData(2, 40)]   // even bay, 40ft ✓
    [InlineData(4, 40)]   // even bay, 40ft ✓
    public void Valid_Combinations_Should_Pass(int bay, int sizeFeet)
    {
        new BayParityMatchesContainerSizeRule(bay, sizeFeet).IsBroken().Should().BeFalse();
    }

    [Theory]
    [InlineData(2, 20)]   // even bay, 20ft ✗
    [InlineData(1, 40)]   // odd bay, 40ft ✗
    [InlineData(4, 20)]   // even bay, 20ft ✗
    public void Invalid_Combinations_Should_Fail(int bay, int sizeFeet)
    {
        new BayParityMatchesContainerSizeRule(bay, sizeFeet).IsBroken().Should().BeTrue();
    }
}

public sealed class DeliveryOrderRulesTests
{
    [Fact]
    public void NotExpired_Rule_Passes_When_Expiry_Is_In_The_Future()
    {
        var rule = new DeliveryOrderNotExpiredRule(DateTimeOffset.UtcNow.AddDays(7), DateTimeOffset.UtcNow);
        rule.IsBroken().Should().BeFalse();
    }

    [Fact]
    public void NotExpired_Rule_Fails_When_Already_Expired()
    {
        var rule = new DeliveryOrderNotExpiredRule(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow);
        rule.IsBroken().Should().BeTrue();
    }

    [Fact]
    public void QuantityAvailable_Rule_Passes_When_Delivered_Less_Than_Requested()
    {
        var rule = new DeliveryOrderQuantityAvailableRule(requestedQuantity: 5, deliveredQuantity: 2);
        rule.IsBroken().Should().BeFalse();
    }

    [Fact]
    public void QuantityAvailable_Rule_Fails_When_Delivered_Meets_Requested()
    {
        var rule = new DeliveryOrderQuantityAvailableRule(requestedQuantity: 5, deliveredQuantity: 5);
        rule.IsBroken().Should().BeTrue();
    }
}

public sealed class YardSlotNotOccupiedRuleTests
{
    [Fact]
    public void Passes_When_Slot_Free()
    {
        new YardSlotNotOccupiedRule(false).IsBroken().Should().BeFalse();
    }

    [Fact]
    public void Fails_When_Slot_Occupied()
    {
        new YardSlotNotOccupiedRule(true).IsBroken().Should().BeTrue();
    }
}