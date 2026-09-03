using TechSpherex.CleanArchitecture.Api.Extensions;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Application.Features.Yard;
using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Api.Endpoints;

/// <summary>
/// Nhóm các endpoint REST cho chức năng Yard (quản lý Depot, Block, YardMap).
/// </summary>
public static class YardEndpoints
{
    /// <summary>
    /// Đăng ký tất cả endpoint Yard vào <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    /// <param name="app">Route builder để đăng ký endpoint.</param>
    public static void MapYardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/yard")
            .WithTags("Yard");

        group.MapGet("/depots", GetDepots)
            .WithName("GetDepots")
            .WithSummary("Liệt kê tất cả depot");

        group.MapGet("/depots/{depotId:guid}/map", GetYardMap)
            .WithName("GetYardMap")
            .WithSummary("Lấy bản đồ yard (Block → Bay → Row → Tier grid + trạng thái chiếm slot).");

        var blocks = app.MapGroup("/api/blocks")
            .WithTags("Yard")
            .RequireAuthorization();

        blocks.MapPost("/", CreateBlock)
            .AddEndpointFilter<ValidationFilter<CreateBlockCommand>>()
            .WithName("CreateBlock")
            .WithSummary("Tạo Block thực (có lưới Bay/Row/Tier).");

        blocks.MapPost("/virtual", CreateVirtualBlock)
            .AddEndpointFilter<ValidationFilter<CreateVirtualBlockCommand>>()
            .WithName("CreateVirtualBlock")
            .WithSummary("Tạo Block ảo (không có lưới vị trí).");

        blocks.MapPatch("/{id:guid}/resize", ResizeBlock)
            .AddEndpointFilter<ValidationFilter<ResizeBlockCommand>>()
            .WithName("ResizeBlock")
            .WithSummary("Thay đổi kích thước Block và tự động tạo slot còn thiếu.");

        blocks.MapPut("/{id:guid}", UpdateBlock)
            .WithName("UpdateBlock")
            .WithSummary("Cập nhật mã và tên Block.");

        blocks.MapDelete("/{id:guid}", DeleteBlock)
            .WithName("DeleteBlock")
            .WithSummary("Xóa Block nếu tất cả slot đều trống.");
    }

    /// <summary>Xử lý GET /api/yard/depots — lấy danh sách depot.</summary>
    private static async Task<IResult> GetDepots(
        IQueryHandler<GetDepotsQuery, Result<IReadOnlyList<DepotDto>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetDepotsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Xử lý GET /api/yard/depots/{depotId}/map — lấy bản đồ yard.</summary>
    private static async Task<IResult> GetYardMap(
        Guid depotId,
        IQueryHandler<GetYardMapQuery, Result<YardMapDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetYardMapQuery(depotId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Xử lý POST /api/blocks — tạo Block thực.</summary>
    private static async Task<IResult> CreateBlock(
        CreateBlockCommand command,
        ICommandHandler<CreateBlockCommand, Result<CreateBlockResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/blocks/{result.Value!.Id}", result.Value)
            : result.ToProblemDetails();
    }

    /// <summary>Xử lý POST /api/blocks/virtual — tạo Block ảo.</summary>
    private static async Task<IResult> CreateVirtualBlock(
        CreateVirtualBlockCommand command,
        ICommandHandler<CreateVirtualBlockCommand, Result<CreateBlockResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/blocks/{result.Value!.Id}", result.Value)
            : result.ToProblemDetails();
    }

    /// <summary>Xử lý PATCH /api/blocks/{id}/resize — thay đổi kích thước Block.</summary>
    private static async Task<IResult> ResizeBlock(
        Guid id,
        ResizeBlockRequest request,
        ICommandHandler<ResizeBlockCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ResizeBlockCommand(id, request.MaxBay, request.MaxRow, request.MaxTier), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>Xử lý PUT /api/blocks/{id} — cập nhật mã và tên Block.</summary>
    private static async Task<IResult> UpdateBlock(
        Guid id,
        UpdateBlockRequest request,
        ICommandHandler<UpdateBlockCommand, Result<CreateBlockResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new UpdateBlockCommand(id, request.Code, request.Name), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Xử lý DELETE /api/blocks/{id} — xóa Block.</summary>
    private static async Task<IResult> DeleteBlock(
        Guid id,
        ICommandHandler<DeleteBlockCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteBlockCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

/// <summary>
/// Yêu cầu thay đổi kích thước Block.
/// </summary>
/// <param name="MaxBay">Số Bay tối đa mới.</param>
/// <param name="MaxRow">Số Row tối đa mới.</param>
/// <param name="MaxTier">Số Tier tối đa mới.</param>
public sealed record ResizeBlockRequest(int MaxBay, int MaxRow, int MaxTier);

/// <summary>
/// Yêu cầu cập nhật Block.
/// </summary>
/// <param name="Code">Mã code mới.</param>
/// <param name="Name">Tên mới.</param>
public sealed record UpdateBlockRequest(string Code, string Name);
