namespace TechSpherex.CleanArchitecture.Domain.Common.Rules;

/// <summary>
/// Validates the ISO 6346 / BIC check digit (Modulo 11) of a container number.
///
/// Algorithm (Phụ lục I):
///   1. Take the first 10 characters (3 owner code + 1 type + 6 serial).
///   2. Assign each character a numeric value:
///        – Letters: A=10, B=12, C=13, …, Z=38 (skip multiples of 11).
///        – Digits: 0..9.
///   3. Multiply each value by 2^(position) where position = 0..9 (rightmost is 0).
///   4. Sum all products.
///   5. Modulo 11 of the sum gives the expected check digit.
///   6. If modulo result is 10, the number is invalid (per ISO 6346).
///   7. The last character of the 11-char input must equal the modulo result.
/// </summary>
public sealed class ContainerNumberCheckDigitRule : IBusinessRule
{
    private readonly string _candidate;

    public ContainerNumberCheckDigitRule(string candidate)
    {
        _candidate = (candidate ?? string.Empty).Trim().ToUpperInvariant();
    }

    public string RuleCode => "Container.NumberCheckDigit";
    public string Message => "Container number failed ISO 6346 Modulo-11 check digit validation.";
    public int Priority => 1;

    public bool IsBroken()
    {
        if (_candidate.Length != 11) return true;

        // 1) Character-class validation: first 4 chars are letters A–Z, last 7 are digits.
        for (var i = 0; i < 4; i++)
        {
            var c = _candidate[i];
            if (c is < 'A' or > 'Z') return true;
        }
        for (var i = 4; i < 11; i++)
        {
            var c = _candidate[i];
            if (c is < '0' or > '9') return true;
        }

        var expected = ComputeCheckDigit(_candidate.AsSpan(0, 10));
        if (expected == 10) return true; // invalid per ISO

        return expected != (_candidate[10] - '0');
    }

    /// <summary>Computes the expected Modulo-11 check digit for the first 10 characters.</summary>
    public static int ComputeCheckDigit(ReadOnlySpan<char> tenChars)
    {
        if (tenChars.Length != 10)
            throw new ArgumentException("Need exactly 10 characters.", nameof(tenChars));

        ulong sum = 0;
        for (var i = 0; i < 10; i++)
        {
            var value = CharToValue(tenChars[i]);
            // ISO 6346: position is 1-based from the leftmost character.
            // Weight = 2^(position-1) = 2^i for i = 0 (leftmost) ... 9 (rightmost).
            sum += (ulong)value * (1UL << i);
        }

        var mod = (int)(sum % 11UL);
        return mod;
    }

    private static int CharToValue(char c)
    {
        if (c is >= '0' and <= '9') return c - '0';
        if (c is >= 'A' and <= 'Z') return LetterValue(c);
        throw new ArgumentException($"Invalid container-number character '{c}'.", nameof(c));
    }

    /// <summary>
    /// ISO 6346 letter-value table. Multiples of 11 are skipped, producing:
    /// A=10, B=12, C=13, …, J=20, K=21, L=23, M=24, …, U=32, V=34, …, Z=38.
    /// </summary>
    private static readonly int[] LetterValues =
    [
        10, 12, 13, 14, 15, 16, 17, 18, 19, 20, // A-J
        21, 23, 24, 25, 26, 27, 28, 29, 30, 31, // K-T
        32, 34, 35, 36, 37, 38                  // U-Z
    ];

    private static int LetterValue(char letter) => LetterValues[letter - 'A'];
}