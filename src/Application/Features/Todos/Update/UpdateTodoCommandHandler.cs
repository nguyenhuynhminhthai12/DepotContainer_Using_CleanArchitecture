
using TechSpherex.CleanArchitecture.Application.Abstractions.Data;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Application.Features.Todos.Update;

/// <summary>
/// Xử lý lệnh cập nhật thông tin Todo (tiêu đề, mô tả).
/// </summary>
/// <param name="dbContext">Context cơ sở dữ liệu.</param>
public sealed class UpdateTodoCommandHandler(IAppDbContext dbContext) : ICommandHandler<UpdateTodoCommand>
{
    /// <inheritdoc/>
    public async Task<Result> HandleAsync(UpdateTodoCommand command, CancellationToken cancellationToken = default)
    {
        var todo = await dbContext.Todos.FindAsync([command.Id], cancellationToken);
        if (todo is null)
            return Result.Failure(Error.NotFound("Todo.NotFound", $"Todo with ID '{command.Id}' was not found."));

        todo.Title = command.Title;
        todo.Description = command.Description;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
