using TechSpherex.CleanArchitecture.Api.Extensions;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Application.Features.DeliveryOrders;
using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Api.Endpoints;

/// <summary>
/// Nhóm các endpoint REST cho chức năng Delivery Order (CRUD + đóng đơn).
/// </summary>
public static class DeliveryOrderEndpoints
{
    /// <summary>
    /// Đăng ký tất cả endpoint Delivery Order vào <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    /// <param name="app">Route builder để đăng ký endpoint.</param>
    public static void MapDeliveryOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/delivery-orders")
            .WithTags("Delivery Orders")
            .RequireAuthorization();

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateDeliveryOrderCommand>>()
            .WithName("CreateDeliveryOrder")
            .WithSummary("Tạo một Delivery / Release Order mới.");

        group.MapGet("/active", GetActive)
            .WithName("GetActiveDeliveryOrders")
            .WithSummary("Liệt kê các Delivery Order đang hoạt động (chưa đóng, chưa hết hạn).");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetDeliveryOrderById")
            .WithSummary("Lấy một Delivery Order theo ID.");

        group.MapPost("/{id:guid}/close", Close)
            .WithName("CloseDeliveryOrder")
            .WithSummary("Đóng một Delivery Order (ngăn Gate-Out tiếp theo).");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateDeliveryOrder")
            .WithSummary("Cập nhật một Delivery Order đã tồn tại.");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteDeliveryOrder")
            .WithSummary("Xóa Delivery Order nếu chưa có container nào được xuất.");
    }

    /// <summary>Xử lý POST /api/delivery-orders — tạo Delivery Order mới.</summary>
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

    /// <summary>Xử lý GET /api/delivery-orders/active — lấy các Delivery Order đang hoạt động.</summary>
    private static async Task<IResult> GetActive(
        IQueryHandler<GetActiveDeliveryOrdersQuery, Result<IReadOnlyList<DeliveryOrderResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetActiveDeliveryOrdersQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Xử lý GET /api/delivery-orders/{id} — lấy Delivery Order theo ID.</summary>
    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetDeliveryOrderByIdQuery, Result<DeliveryOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetDeliveryOrderByIdQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Xử lý POST /api/delivery-orders/{id}/close — đóng Delivery Order.</summary>
    private static async Task<IResult> Close(
        Guid id,
        ICommandHandler<CloseDeliveryOrderCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new CloseDeliveryOrderCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>Xử lý PUT /api/delivery-orders/{id} — cập nhật Delivery Order.</summary>
    private static async Task<IResult> Update(
        Guid id,
        UpdateDeliveryOrderCommand command,
        ICommandHandler<UpdateDeliveryOrderCommand, Result<DeliveryOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return TypedResults.BadRequest("Route ID does not match body ID.");
        }

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Xử lý DELETE /api/delivery-orders/{id} — xóa Delivery Order.</summary>
    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteDeliveryOrderCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteDeliveryOrderCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
