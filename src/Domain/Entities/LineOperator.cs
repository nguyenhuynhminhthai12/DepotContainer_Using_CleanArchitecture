using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Domain.Entities;

/// <summary>
/// Line Operator — shipping line that owns/manages the containers (e.g. CMA CGM, MSC, HMM, Maersk).
/// </summary>
public sealed class LineOperator : AuditableEntity, ITenantEntity
{
    /// <summary>3-letter BIC owner code that prefixes container numbers (e.g. "CMA", "MSK").</summary>
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Country { get; set; }
    public bool IsActive { get; set; } = true;

    public string TenantId { get; set; } = "default";
}