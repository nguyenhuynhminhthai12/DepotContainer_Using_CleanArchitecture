using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Domain.Entities;

/// <summary>
/// Container master data. ContainerNumber follows ISO 6346 (Modulo-11 check digit, validated by domain rule).
/// </summary>
public sealed class Container : AuditableEntity, ITenantEntity
{
    /// <summary>11-char BIC/ISO 6346 container number (owner code + type code + serial + check digit).</summary>
    public string ContainerNumberRaw { get; internal set; } = default!;

    public Guid ContainerTypeId { get; set; }
    public string IsoCode { get; set; } = default!;
    public int SizeFeet { get; set; }
    public decimal MaxWeightKg { get; set; }
    public decimal TareWeightKg { get; set; }
    public DateTimeOffset ManufactureDate { get; set; }
    public string Owner { get; set; } = default!;
    public ContainerCondition Condition { get; set; } = ContainerCondition.Normal;

    public string TenantId { get; set; } = "default";

    public ContainerType? ContainerType { get; set; }

    /// <summary>Strongly-typed view of <see cref="ContainerNumberRaw"/>.</summary>
    public ContainerNumber ContainerNumber => new(ContainerNumberRaw);

    public static Container Create(string containerNumber, Guid containerTypeId, string isoCode,
        int sizeFeet, decimal maxWeightKg, decimal tareWeightKg,
        DateTimeOffset manufactureDate, string owner, ContainerCondition condition = ContainerCondition.Normal)
    {
        var normalized = (containerNumber ?? string.Empty).Trim().ToUpperInvariant();
        Domain.Common.Rules.BusinessRuleValidator.CheckRule(
            new Domain.Common.Rules.ContainerNumberCheckDigitRule(normalized));

        return new Container
        {
            ContainerNumberRaw = normalized,
            ContainerTypeId = containerTypeId,
            IsoCode = isoCode,
            SizeFeet = sizeFeet,
            MaxWeightKg = maxWeightKg,
            TareWeightKg = tareWeightKg,
            ManufactureDate = manufactureDate,
            Owner = owner,
            Condition = condition
        };
    }
}

public enum ContainerCondition
{
    Normal,
    Damaged,
    Dented,
    Twisted,
    Cracked,
    Leaking,
    Other
}