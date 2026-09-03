using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Application.Features.DeliveryOrders;

public sealed record DeliveryOrderLineDto(Guid ContainerTypeId, int RequestedQuantity, int DeliveredQuantity);

public sealed record DeliveryOrderResponse(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string CustomerName,
    Guid LineOperatorId,
    string LineOperatorName,
    DateTimeOffset ExpiryDate,
    string? VesselVoyage,
    bool IsClosed,
    IReadOnlyList<DeliveryOrderLineDto> Lines);

public sealed record CreateDeliveryOrderCommand(
    string OrderNumber,
    Guid CustomerId,
    Guid LineOperatorId,
    DateTimeOffset ExpiryDate,
    string? VesselVoyage,
    string? Notes,
    IReadOnlyList<DeliveryOrderLineDto> Lines) : ICommand<Result<DeliveryOrderResponse>>;

public sealed record GetDeliveryOrderByIdQuery(Guid Id) : IQuery<Result<DeliveryOrderResponse>>;

public sealed record GetActiveDeliveryOrdersQuery() : IQuery<Result<IReadOnlyList<DeliveryOrderResponse>>>;

public sealed record CloseDeliveryOrderCommand(Guid Id) : ICommand<Result>;

public sealed record UpdateDeliveryOrderCommand(
    Guid Id,
    DateTimeOffset ExpiryDate,
    string? VesselVoyage,
    string? Notes,
    IReadOnlyList<DeliveryOrderLineDto>? Lines) : ICommand<Result<DeliveryOrderResponse>>;

public sealed record DeleteDeliveryOrderCommand(Guid Id) : ICommand<Result>;