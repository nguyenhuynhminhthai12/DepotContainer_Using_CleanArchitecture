using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Domain.Entities;

/// <summary>
/// Lookup table for ISO container types — Dry / Reefer / Open Top / Flat Rack / Bunker / Ventilated / Specialized.
/// Seeded per Phụ lục II (ISO 6346 Type Codes).
/// </summary>
public sealed class ContainerType : AuditableEntity, ITenantEntity
{
    /// <summary>ISO 6346 type-designation character (e.g. "22G1", "45G1").</summary>
    public string Code { get; set; } = default!;

    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    /// <summary>High-level family (Dry / Reefer / OpenTop / FlatRack / Bunker / Ventilated / Specialized).</summary>
    public string Family { get; set; } = default!;

    public bool IsActive { get; set; } = true;
    public string TenantId { get; set; } = "default";
}