using TechSpherex.CleanArchitecture.Api.Extensions;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Application.Features.Todos.Complete;
using TechSpherex.CleanArchitecture.Application.Features.Todos.Create;
using TechSpherex.CleanArchitecture.Application.Features.Todos.Delete;
using TechSpherex.CleanArchitecture.Application.Features.Todos.Get;
using TechSpherex.CleanArchitecture.Application.Features.Todos.GetAll;
using TechSpherex.CleanArchitecture.Application.Features.Todos.Update;
using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Api.Endpoints;

/// <summary>
/// Nhóm các endpoint REST cho chức năng Todo (CRUD + hoàn thành).
/// </summary>
public static class TodoEndpoints
{
    /// <summary>
    /// Đăng ký tất cả endpoint Todo vào <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    /// <param name="app">Route builder để đăng ký endpoint.</param>
    public static void MapTodoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/todos")
            .WithTags("Todos")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllTodos")
            .WithSummary("Lấy danh sách todo có phân trang");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetTodoById")
            .WithSummary("Lấy một todo theo ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateTodoCommand>>()
            .WithName("CreateTodo")
            .WithSummary("Tạo một todo mới");

        group.MapPut("/{id:guid}", Update)
            .AddEndpointFilter<ValidationFilter<UpdateTodoRequest>>()
            .WithName("UpdateTodo")
            .WithSummary("Cập nhật một todo đã tồn tại");

        group.MapPatch("/{id:guid}/complete", Complete)
            .WithName("CompleteTodo")
            .WithSummary("Đánh dấu một todo là đã hoàn thành");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteTodo")
            .WithSummary("Xóa một todo");
    }

    /// <summary>Xử lý GET /api/todos — lấy danh sách todo có phân trang.</summary>
    private static async Task<IResult> GetAll(
        int? page,
        int? pageSize,
        IQueryHandler<GetAllTodosQuery, Result<PagedResult<TodoDetailResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetAllTodosQuery(page ?? 1, pageSize ?? 10);
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Xử lý GET /api/todos/{id} — lấy một todo theo ID.</summary>
    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetTodoQuery, Result<TodoDetailResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetTodoQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Xử lý POST /api/todos — tạo một todo mới.</summary>
    private static async Task<IResult> Create(
        CreateTodoCommand command,
        ICommandHandler<CreateTodoCommand, Result<CreateTodoResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/todos/{result.Value!.Id}", result.Value)
            : result.ToProblemDetails();
    }

    /// <summary>Xử lý PUT /api/todos/{id} — cập nhật một todo đã tồn tại.</summary>
    private static async Task<IResult> Update(
        Guid id,
        UpdateTodoRequest request,
        ICommandHandler<UpdateTodoCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTodoCommand(id, request.Title, request.Description);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>Xử lý PATCH /api/todos/{id}/complete — đánh dấu todo hoàn thành.</summary>
    private static async Task<IResult> Complete(
        Guid id,
        ICommandHandler<CompleteTodoCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new CompleteTodoCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>Xử lý DELETE /api/todos/{id} — xóa một todo.</summary>
    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteTodoCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteTodoCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

/// <summary>
/// Yêu cầu cập nhật todo (dùng cho PUT).
/// </summary>
/// <param name="Title">Tiêu đề mới.</param>
/// <param name="Description">Mô tả mới (tùy chọn).</param>
public sealed record UpdateTodoRequest(string Title, string? Description);
