using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Domain.Common;
using TechSpherex.CleanArchitecture.Domain.Entities;

namespace TechSpherex.CleanArchitecture.Application.Features.Gate;

public sealed record GateInContainerCommand(
    string ContainerNumber,
    Guid LineOperatorId,
    Guid BlockId,
    int? Bay,
    int? Row,
    int? Tier,
    string Classification,
    string ConditionAtGateIn,
    string? VehicleInNumber,
    string? DriverInName) : ICommand<Result<ContainerMovementResponse>>;

public sealed record GateOutContainerCommand(
    string ContainerNumber,
    Guid DeliveryOrderId,
    string? VehicleOutNumber,
    string? DriverOutName,
    string ConditionAtGateOut) : ICommand<Result<ContainerMovementResponse>>;

public sealed record MoveContainerInYardCommand(
    string ContainerNumber,
    Guid NewBlockId,
    int NewBay,
    int NewRow,
    int NewTier) : ICommand<Result>;

public sealed record ContainerMovementResponse(
    Guid Id,
    Guid ContainerId,
    Guid LineOperatorId,
    Guid? YardSlotId,
    Guid? BlockId,
    string Classification,
    string ConditionAtGateIn,
    string? ConditionAtGateOut,
    string? VehicleInNumber,
    string? DriverInName,
    DateTimeOffset GateInAt,
    string? VehicleOutNumber,
    string? DriverOutName,
    DateTimeOffset? GateOutAt,
    string Status,
    Guid? DeliveryOrderId);

public sealed record GetContainerMovementHistoryQuery(string ContainerNumber)
    : IQuery<Result<IReadOnlyList<ContainerMovementResponse>>>;