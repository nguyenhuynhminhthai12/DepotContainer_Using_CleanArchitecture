namespace TechSpherex.CleanArchitecture.Domain.Common;

/// <summary>
/// Kết quả phân trang chứa danh sách phần tử và thông tin phân trang.
/// </summary>
/// <typeparam name="T">Kiểu dữ liệu của từng phần tử trong trang.</typeparam>
/// <param name="items">Danh sách phần tử trong trang hiện tại.</param>
/// <param name="totalCount">Tổng số phần tử của toàn bộ tập dữ liệu.</param>
/// <param name="page">Số trang hiện tại (bắt đầu từ 1).</param>
/// <param name="pageSize">Số phần tử tối đa trên mỗi trang.</param>
public sealed class PagedResult<T>(List<T> items, int totalCount, int page, int pageSize)
{
    /// <summary>Danh sách phần tử trong trang hiện tại.</summary>
    public List<T> Items { get; } = items;

    /// <summary>Tổng số phần tử của toàn bộ tập dữ liệu.</summary>
    public int TotalCount { get; } = totalCount;

    /// <summary>Số trang hiện tại (bắt đầu từ 1).</summary>
    public int Page { get; } = page;

    /// <summary>Số phần tử tối đa trên mỗi trang.</summary>
    public int PageSize { get; } = pageSize;

    /// <summary>Tổng số trang (tính từ 1, dựa trên <see cref="TotalCount"/> và <see cref="PageSize"/>).</summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>Cho biết còn trang kế tiếp hay không.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>Cho biết còn trang trước đó hay không.</summary>
    public bool HasPreviousPage => Page > 1;
}
