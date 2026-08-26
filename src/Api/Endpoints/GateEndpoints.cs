using TechSpherex.CleanArchitecture.Api.Extensions;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Application.Features.Gate;
using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Api.Endpoints;

public static class GateEndpoints
{
    public static void MapGateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/gate")
            .WithTags("Gate Operations")
            .RequireAuthorization();

        group.MapPost("/in", GateIn)
            .AddEndpointFilter<ValidationFilter<GateInContainerCommand>>()
            .WithName("GateInContainer")
            .WithSummary("Register a container entering the depot (start of EIR).");

        group.MapPost("/out", GateOut)
            .AddEndpointFilter<ValidationFilter<GateOutContainerCommand>>()
            .WithName("GateOutContainer")
            .WithSummary("Register a container exiting the depot (requires a valid Delivery Order).");

        group.MapPost("/move", Move)
            .AddEndpointFilter<ValidationFilter<MoveContainerInYardCommand>>()
            .WithName("MoveContainerInYard")
            .WithSummary("Move a container from its current slot to a new slot inside the depot.");
    }

    private static async Task<IResult> GateIn(
        GateInContainerCommand command,
        ICommandHandler<GateInContainerCommand, Result<ContainerMovementResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GateOut(
        GateOutContainerCommand command,
        ICommandHandler<GateOutContainerCommand, Result<ContainerMovementResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Move(
        MoveContainerInYardCommand command,
        ICommandHandler<MoveContainerInYardCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    public static void MapMovementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/containers")
            .WithTags("Gate Operations");

        group.MapGet("/{number}/movements", GetHistory)
            .WithName("GetContainerMovementHistory")
            .WithSummary("Get the EIR (movement history) of a container.");
    }

    private static async Task<IResult> GetHistory(
        string number,
        IQueryHandler<GetContainerMovementHistoryQuery, Result<IReadOnlyList<ContainerMovementResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetContainerMovementHistoryQuery(number), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}