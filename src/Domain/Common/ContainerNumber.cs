namespace TechSpherex.CleanArchitecture.Domain.Common;

/// <summary>
/// Strongly-typed container number following the BIC/ISO 6346 standard.
/// Format: 11 characters — 3-letter owner code + 1-letter type code + 6-digit serial + 1 check digit.
/// Validated via the Modulo 11 algorithm (see <see cref="Domain.Common.Rules.ContainerNumberCheckDigitRule"/>).
/// </summary>
public readonly record struct ContainerNumber
{
    public string Value { get; }

    public ContainerNumber(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != 11)
            throw new ArgumentException("Container number must be exactly 11 characters.", nameof(value));
        Value = normalized;
    }

    public override string ToString() => Value;
    public static implicit operator string(ContainerNumber number) => number.Value;
    public static explicit operator ContainerNumber(string value) => new(value);
}