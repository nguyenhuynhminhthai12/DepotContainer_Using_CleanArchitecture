
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Application.Features.Todos.Delete;

/// <summary>
/// Lệnh xóa một Todo theo ID.
/// </summary>
/// <param name="Id">Mã định danh Todo cần xóa.</param>
public sealed record DeleteTodoCommand(Guid Id) : ICommand;
