/**
 * Bộ test cho các Business Rules trong Domain.
 * Bao gồm test quy tắc Bay Parity (số chẵn/lẻ của Bay phù hợp với kích thước container),
 * quy tắc Đơn giao hàng không hết hạn, đủ số lượng, và Slot Yard không bị chiếm.
 * Bản quyền (c) 2026 TechSpherex.
 */
using FluentAssertions;
using TechSpherex.CleanArchitecture.Domain.Common.Rules;

namespace TechSpherex.CleanArchitecture.Application.UnitTests.Domain;

public sealed class BayParityMatchesContainerSizeRuleTests
{
    [Theory]
    [InlineData(1, 20)]
    [InlineData(3, 20)]
    [InlineData(2, 40)]
    [InlineData(4, 40)]
    public void Valid_Combinations_Should_Pass(int bay, int sizeFeet) =>
        new BayParityMatchesContainerSizeRule(bay, sizeFeet).IsBroken().Should().BeFalse();

    [Theory]
    [InlineData(2, 20)]
    [InlineData(1, 40)]
    [InlineData(4, 20)]
    public void Invalid_Combinations_Should_Fail(int bay, int sizeFeet) =>
        new BayParityMatchesContainerSizeRule(bay, sizeFeet).IsBroken().Should().BeTrue();
}

public sealed class DeliveryOrderRulesTests
{
    [Fact]
    public void NotExpired_Rule_Passes_When_Expiry_Is_In_The_Future() =>
        new DeliveryOrderNotExpiredRule(DateTimeOffset.UtcNow.AddDays(7), DateTimeOffset.UtcNow).IsBroken().Should().BeFalse();

    [Fact]
    public void NotExpired_Rule_Fails_When_Already_Expired() =>
        new DeliveryOrderNotExpiredRule(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow).IsBroken().Should().BeTrue();

    [Fact]
    public void QuantityAvailable_Rule_Passes_When_Delivered_Less_Than_Requested() =>
        new DeliveryOrderQuantityAvailableRule(requestedQuantity: 5, deliveredQuantity: 2).IsBroken().Should().BeFalse();

    [Fact]
    public void QuantityAvailable_Rule_Fails_When_Delivered_Meets_Requested() =>
        new DeliveryOrderQuantityAvailableRule(requestedQuantity: 5, deliveredQuantity: 5).IsBroken().Should().BeTrue();
}

public sealed class YardSlotNotOccupiedRuleTests
{
    [Fact]
    public void Passes_When_Slot_Free() =>
        new YardSlotNotOccupiedRule(false).IsBroken().Should().BeFalse();

    [Fact]
    public void Fails_When_Slot_Occupied() =>
        new YardSlotNotOccupiedRule(true).IsBroken().Should().BeTrue();
}
