
using TechSpherex.CleanArchitecture.Application.Abstractions.Data;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Application.Features.Todos.Get;

/// <summary>
/// Xử lý truy vấn lấy thông tin chi tiết một mục công việc theo ID.
/// </summary>
public sealed class GetTodoQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetTodoQuery, Result<TodoDetailResponse>>
{
    /// <inheritdoc/>
    public async Task<Result<TodoDetailResponse>> HandleAsync(GetTodoQuery query, CancellationToken cancellationToken = default)
    {
        var todo = await dbContext.Todos.FindAsync([query.Id], cancellationToken);
        if (todo is null)
            return Result.Failure<TodoDetailResponse>(Error.NotFound("Todo.NotFound", $"Todo with ID '{query.Id}' was not found."));

        var response = new TodoDetailResponse(todo.Id, todo.Title, todo.Description, todo.IsCompleted, todo.CompletedAt, todo.CreatedAt);
        return Result.Success(response);
    }
}
