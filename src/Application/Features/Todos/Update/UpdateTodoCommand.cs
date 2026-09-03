
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Application.Features.Todos.Update;

/// <summary>
/// Lệnh cập nhật tiêu đề và mô tả của một Todo.
/// </summary>
/// <param name="Id">Mã định danh Todo cần cập nhật.</param>
/// <param name="Title">Tiêu đề mới.</param>
/// <param name="Description">Mô tả mới (tùy chọn).</param>
public sealed record UpdateTodoCommand(Guid Id, string Title, string? Description) : ICommand;
