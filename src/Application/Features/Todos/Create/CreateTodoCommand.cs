
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Application.Features.Todos.Create;

/// <summary>
/// Lệnh tạo một công việc Todo mới. Trả về <see cref="CreateTodoResponse"/>.
/// </summary>
/// <param name="Title">Tiêu đề công việc.</param>
/// <param name="Description">Mô tả chi tiết (tùy chọn).</param>
public sealed record CreateTodoCommand(string Title, string? Description) : ICommand<Result<CreateTodoResponse>>;

/// <summary>
/// DTO trả về thông tin công việc Todo vừa được tạo.
/// </summary>
/// <param name="Id">Mã định danh Todo.</param>
/// <param name="Title">Tiêu đề công việc.</param>
/// <param name="Description">Mô tả chi tiết.</param>
public sealed record CreateTodoResponse(Guid Id, string Title, string? Description);
