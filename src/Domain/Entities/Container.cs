using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Domain.Entities;

/// <summary>
/// Options for creating a new Container.
/// </summary>
/// <param name="ContainerNumber">11-char BIC/ISO 6346 container number (owner code + type code + serial + check digit).</param>
/// <param name="ContainerTypeId">Container type identifier.</param>
/// <param name="IsoCode">ISO code (e.g. "22G1").</param>
/// <param name="SizeFeet">Size in feet (20 or 40).</param>
/// <param name="MaxWeightKg">Maximum weight in kg.</param>
/// <param name="TareWeightKg">Tare weight in kg.</param>
/// <param name="ManufactureDate">Manufacture date.</param>
/// <param name="Owner">Owner code (e.g. "CMA").</param>
/// <param name="Condition">Container condition (default: Normal).</param>
public sealed record CreateContainerOptions(
    string ContainerNumber,
    Guid ContainerTypeId,
    string IsoCode,
    int SizeFeet,
    decimal MaxWeightKg,
    decimal TareWeightKg,
    DateTimeOffset ManufactureDate,
    string Owner,
    ContainerCondition Condition = ContainerCondition.Normal);

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

    public static Container Create(CreateContainerOptions options)
    {
        var normalized = options.ContainerNumber.Trim().ToUpperInvariant();
        Domain.Common.Rules.BusinessRuleValidator.CheckRule(
            new Domain.Common.Rules.ContainerNumberCheckDigitRule(normalized));

        return new Container
        {
            ContainerNumberRaw = normalized,
            ContainerTypeId = options.ContainerTypeId,
            IsoCode = options.IsoCode,
            SizeFeet = options.SizeFeet,
            MaxWeightKg = options.MaxWeightKg,
            TareWeightKg = options.TareWeightKg,
            ManufactureDate = options.ManufactureDate,
            Owner = options.Owner,
            Condition = options.Condition
        };
    }

#pragma warning disable S107 // Parameters in facade method
    public static Container Create(
        string containerNumber,
        Guid containerTypeId,
        string isoCode,
        int sizeFeet,
        decimal maxWeightKg,
        decimal tareWeightKg,
        DateTimeOffset manufactureDate,
        string owner,
        ContainerCondition condition = ContainerCondition.Normal) =>
        Create(new CreateContainerOptions(containerNumber, containerTypeId, isoCode, sizeFeet,
            maxWeightKg, tareWeightKg, manufactureDate, owner, condition));
#pragma warning restore S107
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