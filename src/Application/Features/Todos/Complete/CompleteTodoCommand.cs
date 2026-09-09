
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Application.Features.Todos.Complete;

/// <summary>
/// Lệnh đánh dấu một công việc là đã hoàn thành.
/// </summary>
/// <param name="Id">Mã định danh công việc.</param>
public sealed record CompleteTodoCommand(Guid Id) : ICommand;
