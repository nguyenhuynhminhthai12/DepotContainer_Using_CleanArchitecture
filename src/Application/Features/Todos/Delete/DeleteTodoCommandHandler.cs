
using TechSpherex.CleanArchitecture.Application.Abstractions.Data;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Application.Features.Todos.Delete;

/// <summary>
/// Xử lý lệnh xóa một Todo khỏi cơ sở dữ liệu.
/// </summary>
/// <param name="dbContext">Context cơ sở dữ liệu.</param>
public sealed class DeleteTodoCommandHandler(IAppDbContext dbContext) : ICommandHandler<DeleteTodoCommand>
{
    /// <inheritdoc/>
    public async Task<Result> HandleAsync(DeleteTodoCommand command, CancellationToken cancellationToken = default)
    {
        var todo = await dbContext.Todos.FindAsync([command.Id], cancellationToken);
        if (todo is null)
            return Result.Failure(Error.NotFound("Todo.NotFound", $"Todo with ID '{command.Id}' was not found."));

        dbContext.Todos.Remove(todo);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
