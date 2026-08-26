using FluentAssertions;
using TechSpherex.CleanArchitecture.Domain.Common.Rules;

namespace TechSpherex.CleanArchitecture.Application.UnitTests.Domain;

public sealed class ContainerNumberCheckDigitRuleTests
{
    [Theory]
    [InlineData("CMAU1234564")]   // 4 = computed Modulo-11 check digit
    [InlineData("MSCU1234566")]   // 6 = computed check digit
    [InlineData("MAEU1234567")]   // 7 = computed check digit
    public void Valid_Container_Numbers_Should_Pass(string containerNumber)
    {
        var rule = new ContainerNumberCheckDigitRule(containerNumber);
        rule.IsBroken().Should().BeFalse($"{containerNumber} is a valid BIC/ISO 6346 number");
    }

    [Theory]
    [InlineData("CMAU1234560")]   // wrong check digit (0 vs expected 5)
    [InlineData("CMAU123456X")]   // non-digit last char
    [InlineData("12312312345")]   // missing letters, wrong length
    [InlineData("CMAU1234567X")]  // too long
    [InlineData("")]
    [InlineData("cmaU1234560")]   // lowercase normalised then wrong check digit
    public void Invalid_Container_Numbers_Should_Fail(string containerNumber)
    {
        var rule = new ContainerNumberCheckDigitRule(containerNumber);
        rule.IsBroken().Should().BeTrue($"{containerNumber} should fail validation");
    }

    [Fact]
    public void Rule_Should_Expose_Stable_Code_And_Message()
    {
        var rule = new ContainerNumberCheckDigitRule("CMAU1234560");
        rule.RuleCode.Should().Be("Container.NumberCheckDigit");
        rule.Message.Should().NotBeNullOrWhiteSpace();
        rule.Priority.Should().Be(1);
    }
}