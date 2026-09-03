
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Application.Features.Todos.Get;

/// <summary>
/// Truy vấn lấy thông tin chi tiết một Todo theo ID.
/// </summary>
/// <param name="Id">Mã định danh Todo cần truy vấn.</param>
public sealed record GetTodoQuery(Guid Id) : IQuery<Result<TodoDetailResponse>>;

/// <summary>
/// DTO trả về thông tin chi tiết Todo.
/// </summary>
/// <param name="Id">Mã định danh.</param>
/// <param name="Title">Tiêu đề.</param>
/// <param name="Description">Mô tả.</param>
/// <param name="IsCompleted">Trạng thái hoàn thành.</param>
/// <param name="CompletedAt">Thời gian hoàn thành (nếu có).</param>
/// <param name="CreatedAt">Thời gian tạo.</param>
public sealed record TodoDetailResponse(Guid Id, string Title, string? Description, bool IsCompleted, DateTimeOffset? CompletedAt, DateTimeOffset CreatedAt);
