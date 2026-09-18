
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Application.Features.Todos.Delete;

/// <summary>
/// Lệnh xóa một công việc theo ID.
/// </summary>
/// <param name="Id">Mã định danh công việc cần xóa.</param>
public sealed record DeleteTodoCommand(Guid Id) : ICommand;
