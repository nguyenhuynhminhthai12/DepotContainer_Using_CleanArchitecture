
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Application.Features.Todos.Get;
using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Application.Features.Todos.GetAll;

/// <summary>
/// Truy vấn lấy danh sách Todo có phân trang.
/// </summary>
/// <param name="Page">Số trang (mặc định 1).</param>
/// <param name="PageSize">Số phần tử trên mỗi trang (mặc định 10).</param>
public sealed record GetAllTodosQuery(int Page = 1, int PageSize = 10) : IQuery<Result<PagedResult<TodoDetailResponse>>>;
