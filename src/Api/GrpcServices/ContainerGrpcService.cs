using Grpc.Core;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Application.Features.Containers;
using TechSpherex.CleanArchitecture.Application.Features.Gate;
using TechSpherex.CleanArchitecture.Domain.Common;
using AppContainerResponse = TechSpherex.CleanArchitecture.Application.Features.Containers.ContainerResponse;
using AppMovementResponse = TechSpherex.CleanArchitecture.Application.Features.Gate.ContainerMovementResponse;

namespace TechSpherex.CleanArchitecture.Api.GrpcServices;

/// <summary>
/// gRPC implementation of ContainerService.
/// Delegates to the same Application-layer CQRS handlers used by the REST endpoints,
/// ensuring consistent business logic across transport protocols.
/// </summary>
public sealed class ContainerGrpcService(
    IQueryHandler<GetContainerByNumberQuery, Result<AppContainerResponse>> getHandler,
    ICommandHandler<GateInContainerCommand, Result<AppMovementResponse>> gateInHandler,
    ICommandHandler<GateOutContainerCommand, Result<AppMovementResponse>> gateOutHandler,
    IQueryHandler<GetContainerMovementHistoryQuery, Result<IReadOnlyList<AppMovementResponse>>> historyHandler)
    : ContainerService.ContainerServiceBase
{
    public override async Task<ContainerResponse> GetContainer(GetContainerRequest request, ServerCallContext context)
    {
        var result = await getHandler.HandleAsync(new GetContainerByNumberQuery(request.ContainerNumber), context.CancellationToken);
        if (result.IsFailure)
            throw MapToRpcException(result.Error!);

        var c = result.Value!;
        return new ContainerResponse
        {
            Id = c.Id.ToString(),
            ContainerNumber = c.ContainerNumber,
            ContainerTypeId = c.ContainerTypeId.ToString(),
            IsoCode = c.IsoCode,
            SizeFeet = c.SizeFeet,
            ManufactureDate = c.ManufactureDate.ToString("O"),
            Owner = c.Owner,
            Condition = c.Condition
        };
    }

    public override async Task<MovementResponse> GateIn(GateInRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.LineOperatorId, out var lineOpId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid line_operator_id."));
        if (!Guid.TryParse(request.BlockId, out var blockId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid block_id."));

        var cmd = new GateInContainerCommand(
            request.ContainerNumber,
            lineOpId,
            blockId,
            request.Bay,
            request.Row,
            request.Tier,
            request.Classification,
            request.ConditionAtGateIn,
            request.HasVehicleInNumber ? request.VehicleInNumber : null,
            request.HasDriverInName ? request.DriverInName : null);

        var result = await gateInHandler.HandleAsync(cmd, context.CancellationToken);
        if (result.IsFailure)
            throw MapToRpcException(result.Error!);

        return MapMovement(result.Value!);
    }

    public override async Task<MovementResponse> GateOut(GateOutRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.DeliveryOrderId, out var orderId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid delivery_order_id."));

        var cmd = new GateOutContainerCommand(
            request.ContainerNumber,
            orderId,
            request.HasVehicleOutNumber ? request.VehicleOutNumber : null,
            request.HasDriverOutName ? request.DriverOutName : null,
            request.ConditionAtGateOut);

        var result = await gateOutHandler.HandleAsync(cmd, context.CancellationToken);
        if (result.IsFailure)
            throw MapToRpcException(result.Error!);

        return MapMovement(result.Value!);
    }

    public override async Task<MovementHistoryResponse> GetMovementHistory(GetMovementHistoryRequest request, ServerCallContext context)
    {
        var result = await historyHandler.HandleAsync(new GetContainerMovementHistoryQuery(request.ContainerNumber), context.CancellationToken);
        if (result.IsFailure)
            throw MapToRpcException(result.Error!);

        var response = new MovementHistoryResponse();
        foreach (var m in result.Value!)
            response.Items.Add(MapMovement(m));
        return response;
    }

    private static MovementResponse MapMovement(AppMovementResponse m) => new()
    {
        Id = m.Id.ToString(),
        ContainerId = m.ContainerId.ToString(),
        LineOperatorId = m.LineOperatorId.ToString(),
        BlockId = m.BlockId?.ToString() ?? string.Empty,
        Classification = m.Classification,
        ConditionAtGateIn = m.ConditionAtGateIn,
        GateInAt = m.GateInAt.ToString("O"),
        Status = m.Status,
        GateOutAt = m.GateOutAt?.ToString("O") ?? string.Empty
    };

    private static RpcException MapToRpcException(Error error) => error.Type switch
    {
        ErrorType.NotFound => new RpcException(new Status(StatusCode.NotFound, error.Message)),
        ErrorType.Validation => new RpcException(new Status(StatusCode.InvalidArgument, error.Message)),
        ErrorType.Conflict => new RpcException(new Status(StatusCode.AlreadyExists, error.Message)),
        _ => new RpcException(new Status(StatusCode.Internal, error.Message))
    };
}
