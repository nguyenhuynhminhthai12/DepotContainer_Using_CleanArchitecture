using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Domain.Entities;

/// <summary>
/// A Block is a physical or virtual area inside a Depot that groups container storage positions.
/// Real blocks are organised in a 3-D grid: Bay (length) → Row (width) → Tier (height).
/// Virtual blocks (IsVirtual=true) only track the container without a slot grid.
/// </summary>
public sealed class Block : AuditableEntity, ITenantEntity
{
    public Guid DepotId { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsVirtual { get; set; }

    public int? MaxBay { get; set; }
    public int? MaxRow { get; set; }
    public int? MaxTier { get; set; }

    public string TenantId { get; set; } = "default";

    /// <summary>Position inside its Depot for display purposes (1-based).</summary>
    public int DisplayOrder { get; set; }

    public Depot? Depot { get; set; }
}