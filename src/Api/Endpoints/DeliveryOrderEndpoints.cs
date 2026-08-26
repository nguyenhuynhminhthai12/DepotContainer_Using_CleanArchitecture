using TechSpherex.CleanArchitecture.Api.Extensions;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Application.Features.DeliveryOrders;
using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Api.Endpoints;

public static class DeliveryOrderEndpoints
{
    public static void MapDeliveryOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/delivery-orders")
            .WithTags("Delivery Orders")
            .RequireAuthorization();

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateDeliveryOrderCommand>>()
            .WithName("CreateDeliveryOrder")
            .WithSummary("Create a new Delivery / Release Order.");

        group.MapGet("/active", GetActive)
            .WithName("GetActiveDeliveryOrders")
            .WithSummary("List active (non-expired, non-closed) Delivery Orders.");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetDeliveryOrderById")
            .WithSummary("Get a Delivery Order by id.");

        group.MapPost("/{id:guid}/close", Close)
            .WithName("CloseDeliveryOrder")
            .WithSummary("Close a Delivery Order (prevents further Gate-Out authorisations).");
    }

    private static async Task<IResult> Create(
        CreateDeliveryOrderCommand command,
        ICommandHandler<CreateDeliveryOrderCommand, Result<DeliveryOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/delivery-orders/{result.Value!.Id}", result.Value)
            : result.ToProblemDetails();
    }

    private static async Task<IResult> GetActive(
        IQueryHandler<GetActiveDeliveryOrdersQuery, Result<IReadOnlyList<DeliveryOrderResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetActiveDeliveryOrdersQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetDeliveryOrderByIdQuery, Result<DeliveryOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetDeliveryOrderByIdQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Close(
        Guid id,
        ICommandHandler<CloseDeliveryOrderCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new CloseDeliveryOrderCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}