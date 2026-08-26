using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Domain.Entities;

/// <summary>
/// A single storage slot inside a non-virtual Block, identified by Bay/Row/Tier.
/// Odd Bays host 20 ft containers; Even Bays host 40 ft containers (two adjacent odd Bays equal one even Bay).
/// </summary>
public sealed class YardSlot : AuditableEntity, ITenantEntity
{
    public Guid BlockId { get; set; }
    public int Bay { get; set; }
    public int Row { get; set; }
    public int Tier { get; set; }
    public bool IsOccupied { get; set; }
    public Guid? CurrentContainerId { get; set; }

    public string TenantId { get; set; } = "default";

    public Block? Block { get; set; }
}