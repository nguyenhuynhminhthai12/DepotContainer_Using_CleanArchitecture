namespace TechSpherex.CleanArchitecture.Application.Abstractions.Caching;

/// <summary>
/// Lớp trừu tượng cache sạch cho tầng Application.
/// Bọc (wrap) HybridCache (L1 In-Memory + L2 Redis) để các handler
/// không phụ thuộc trực tiếp vào các gói infrastructure.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Lấy giá trị từ cache hoặc tạo mới bằng hàm factory.
    /// Trình tự: L1 (RAM) → L2 (Redis) → Factory fallback.
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu của giá trị cache.</typeparam>
    /// <param name="key">Khóa cache.</param>
    /// <param name="factory">Hàm tạo giá trị khi không có trong cache.</param>
    /// <param name="expiration">Thời gian hết hạn chung (cả L1 và L2).</param>
    /// <param name="localExpiration">Thời gian hết hạn riêng cho L1 (RAM).</param>
    /// <param name="tags">Danh sách thẻ để xóa bỏ cache theo nhóm.</param>
    /// <param name="cancellationToken">Token hủy hoạt động bất đồng bộ.</param>
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        TimeSpan? localExpiration = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>Đặt một giá trị vào cả hai lớp cache L1 và L2.</summary>
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        TimeSpan? localExpiration = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>Xóa một khóa cụ thể khỏi tất cả các lớp cache.</summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Vô hiệu hóa mọi mục được đánh dấu bởi thẻ (tag) cho trước.</summary>
    Task InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default);
}
