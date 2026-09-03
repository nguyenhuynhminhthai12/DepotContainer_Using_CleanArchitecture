using TechSpherex.CleanArchitecture.Api.Extensions;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Application.Features.Containers;
using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Api.Endpoints;

/// <summary>
/// Nhóm các endpoint REST cho chức năng quản lý Container (CRUD).
/// </summary>
public static class ContainerEndpoints
{
    /// <summary>
    /// Đăng ký tất cả endpoint Container vào <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    /// <param name="app">Route builder để đăng ký endpoint.</param>
    public static void MapContainerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/containers")
            .WithTags("Containers");

        group.MapGet("/", GetContainers)
            .WithName("GetContainers")
            .WithSummary("Liệt kê container có phân trang, lọc theo hành đường/tình trạng/tìm kiếm.");

        group.MapGet("/{number}", GetByNumber)
            .WithName("GetContainerByNumber")
            .WithSummary("Lấy một container theo số thùng hàng 11 ký tự.");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateContainerCommand>>()
            .RequireAuthorization()
            .WithName("CreateContainer")
            .WithSummary("Đăng ký container mới (xác thực chữ số kiểm tra ISO 6346).");

        group.MapPut("/{id:guid}", Update)
            .RequireAuthorization()
            .WithName("UpdateContainer")
            .WithSummary("Cập nhật thông tin và tình trạng container.");

        group.MapDelete("/{id:guid}", Delete)
            .RequireAuthorization()
            .WithName("DeleteContainer")
            .WithSummary("Xóa container nếu không đang chiếm yard slot.");
    }

    /// <summary>Xử lý GET /api/containers — liệt kê container có phân trang.</summary>
    private static async Task<IResult> GetContainers(
        int? page,
        int? pageSize,
        Guid? lineOperatorId,
        string? condition,
        string? search,
        IQueryHandler<GetContainersQuery, Result<PagedResult<ContainerResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetContainersQuery(
            page ?? 1,
            pageSize ?? 20,
            lineOperatorId,
            condition,
            search);
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Xử lý GET /api/containers/{number} — lấy container theo số thùng hàng.</summary>
    private static async Task<IResult> GetByNumber(
        string number,
        IQueryHandler<GetContainerByNumberQuery, Result<ContainerResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetContainerByNumberQuery(number), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Xử lý POST /api/containers — tạo container mới.</summary>
    private static async Task<IResult> Create(
        CreateContainerCommand command,
        ICommandHandler<CreateContainerCommand, Result<ContainerResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/containers/{result.Value!.ContainerNumber}", result.Value)
            : result.ToProblemDetails();
    }

    /// <summary>Xử lý PUT /api/containers/{id} — cập nhật thông tin container.</summary>
    private static async Task<IResult> Update(
        Guid id,
        UpdateContainerCommand command,
        ICommandHandler<UpdateContainerCommand, Result<ContainerResponse>> handler,
        CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return TypedResults.BadRequest("Route ID does not match body ID.");

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Xử lý DELETE /api/containers/{id} — xóa container.</summary>
    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteContainerCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteContainerCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
