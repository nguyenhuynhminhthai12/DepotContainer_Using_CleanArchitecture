using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Domain.Entities;

/// <summary>
/// Equipment Interchange Receipt (EIR) — one lifecycle entry per container movement through the depot.
/// </summary>
public sealed class ContainerMovement : AuditableEntity, ITenantEntity
{
    public Guid ContainerId { get; set; }
    public Guid LineOperatorId { get; set; }

    /// <summary>Storage location when in-yard (null for virtual blocks / not in yard).</summary>
    public Guid? YardSlotId { get; set; }
    public Guid? BlockId { get; set; }

    /// <summary>Classification assigned at gate-in (A / B / C).</summary>
    public string Classification { get; set; } = "A";
    public ContainerCondition ConditionAtGateIn { get; set; } = ContainerCondition.Normal;
    public ContainerCondition? ConditionAtGateOut { get; set; }

    public string? VehicleInNumber { get; set; }
    public string? DriverInName { get; set; }
    public DateTimeOffset GateInAt { get; set; }

    public string? VehicleOutNumber { get; set; }
    public string? DriverOutName { get; set; }
    public DateTimeOffset? GateOutAt { get; set; }

    public MovementStatus Status { get; set; } = MovementStatus.InYard;

    public Guid? DeliveryOrderId { get; set; }

    public string TenantId { get; set; } = "default";

    public Container? Container { get; set; }
    public LineOperator? LineOperator { get; set; }
    public YardSlot? YardSlot { get; set; }
    public Block? Block { get; set; }
    public DeliveryOrder? DeliveryOrder { get; set; }
}

public enum MovementStatus
{
    InYard,
    GateOut
}