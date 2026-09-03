using TechSpherex.CleanArchitecture.Api.Extensions;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Application.Features.Gate;
using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Api.Endpoints;

/// <summary>
/// Nhóm các endpoint REST cho chức năng Gate (nhập/xuất cửa, di chuyển trong yard).
/// </summary>
public static class GateEndpoints
{
    /// <summary>
    /// Đăng ký tất cả endpoint Gate vào <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    /// <param name="app">Route builder để đăng ký endpoint.</param>
    public static void MapGateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/gate")
            .WithTags("Gate Operations")
            .RequireAuthorization();

        group.MapPost("/in", GateIn)
            .AddEndpointFilter<ValidationFilter<GateInContainerCommand>>()
            .WithName("GateInContainer")
            .WithSummary("Đăng ký container nhập cửa (bắt đầu EIR).");

        group.MapPost("/out", GateOut)
            .AddEndpointFilter<ValidationFilter<GateOutContainerCommand>>()
            .WithName("GateOutContainer")
            .WithSummary("Đăng ký container xuất cửa (yêu cầu Delivery Order hợp lệ).");

        group.MapPost("/move", Move)
            .AddEndpointFilter<ValidationFilter<MoveContainerInYardCommand>>()
            .WithName("MoveContainerInYard")
            .WithSummary("Di chuyển container từ slot hiện tại đến slot mới trong depot.");
    }

    /// <summary>Xử lý POST /api/gate/in — nhập cửa container.</summary>
    private static async Task<IResult> GateIn(
        GateInContainerCommand command,
        ICommandHandler<GateInContainerCommand, Result<ContainerMovementResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Xử lý POST /api/gate/out — xuất cửa container.</summary>
    private static async Task<IResult> GateOut(
        GateOutContainerCommand command,
        ICommandHandler<GateOutContainerCommand, Result<ContainerMovementResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Xử lý POST /api/gate/move — di chuyển container trong yard.</summary>
    private static async Task<IResult> Move(
        MoveContainerInYardCommand command,
        ICommandHandler<MoveContainerInYardCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Đăng ký endpoint lịch sử di chuyển container vào <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    /// <param name="app">Route builder để đăng ký endpoint.</param>
    public static void MapMovementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/containers")
            .WithTags("Gate Operations");

        group.MapGet("/{number}/movements", GetHistory)
            .WithName("GetContainerMovementHistory")
            .WithSummary("Lấy lịch sử di chuyển (EIR) của một container.");
    }

    /// <summary>Xử lý GET /api/containers/{number}/movements — lấy lịch sử di chuyển.</summary>
    private static async Task<IResult> GetHistory(
        string number,
        IQueryHandler<GetContainerMovementHistoryQuery, Result<IReadOnlyList<ContainerMovementResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetContainerMovementHistoryQuery(number), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
