using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Domain.Entities;

/// <summary>
/// Delivery Order / Release Order — authorises the depot to release empty containers
/// to a specific customer. Mandatory for any Gate Out operation.
/// </summary>
public sealed class DeliveryOrder : AuditableEntity, ITenantEntity
{
    public string OrderNumber { get; set; } = default!;
    public Guid CustomerId { get; set; }
    public Guid LineOperatorId { get; set; }

    /// <summary>Last date the depot may release containers under this order.</summary>
    public DateTimeOffset ExpiryDate { get; set; }

    /// <summary>Vessel voyage that will take the containers out of Vietnam.</summary>
    public string? VesselVoyage { get; set; }

    public string? Notes { get; set; }

    public bool IsClosed { get; set; }

    public string TenantId { get; set; } = "default";

    public Customer? Customer { get; set; }
    public LineOperator? LineOperator { get; set; }
    public ICollection<DeliveryOrderLine> Lines { get; set; } = [];

    public bool IsExpiredAt(DateTimeOffset now) => ExpiryDate < now;
}

public sealed class DeliveryOrderLine : AuditableEntity
{
    public Guid DeliveryOrderId { get; set; }
    public Guid ContainerTypeId { get; set; }
    public int RequestedQuantity { get; set; }
    public int DeliveredQuantity { get; set; }

    public DeliveryOrder? DeliveryOrder { get; set; }
    public ContainerType? ContainerType { get; set; }
}