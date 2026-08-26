using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Domain.Entities;

/// <summary>
/// Represents a container depot terminal — a physical site where containers are stored.
/// In a multi-tenant deployment, each Depot is a tenant.
/// </summary>
public sealed class Depot : AuditableEntity, ITenantEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Address { get; set; } = default!;
    public string? TimeZone { get; set; }
    public bool IsActive { get; set; } = true;
    public string TenantId { get; set; } = "default";
}