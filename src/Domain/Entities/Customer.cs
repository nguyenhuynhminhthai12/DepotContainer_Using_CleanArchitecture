using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Domain.Entities;

/// <summary>
/// Customer (shipper / consignee / trucking company) who is authorised to receive containers.
/// </summary>
public sealed class Customer : AuditableEntity, ITenantEntity
{
    /// <summary>Vietnamese Tax Code (MST).</summary>
    public string TaxCode { get; set; } = default!;

    public string Name { get; set; } = default!;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;

    public string TenantId { get; set; } = "default";
}