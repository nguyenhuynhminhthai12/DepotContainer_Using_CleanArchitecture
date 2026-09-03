using Microsoft.Extensions.Caching.Hybrid;
using TechSpherex.CleanArchitecture.Application.Abstractions.Caching;

namespace TechSpherex.CleanArchitecture.Infrastructure.Caching;

/// <summary>
/// Triển khai <see cref="ICacheService"/> dựa trên HybridCache.
/// L1 = In-Memory (RAM), L2 = Redis (qua Aspire).
/// Trình bày: RAM → Redis → Factory.
/// </summary>
public sealed class HybridCacheService(HybridCache hybridCache) : ICacheService
{
    /// <inheritdoc/>
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        TimeSpan? localExpiration = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        var options = BuildOptions(expiration, localExpiration);

        // HybridCache mong đợi Func<TState, CancellationToken, ValueTask<T>>
        // Sử dụng factory làm tham số trạng thái để kết nối API
        return await hybridCache.GetOrCreateAsync(
            key,
            factory,
            static (state, ct) => new ValueTask<T>(state(ct)),
            options,
            tags: tags?.ToArray(),
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        TimeSpan? localExpiration = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        var options = BuildOptions(expiration, localExpiration);

        await hybridCache.SetAsync(
            key,
            value,
            options,
            tags: tags?.ToArray(),
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        hybridCache.RemoveAsync(key, cancellationToken).AsTask();

    /// <inheritdoc/>
    public Task InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default) =>
        hybridCache.RemoveByTagAsync(tag, cancellationToken).AsTask();

    /// <summary>
    /// Xây dựng <see cref="HybridCacheEntryOptions"/> từ thời gian hết hạn.
    /// </summary>
    /// <param name="expiration">Thời gian hết hạn chung (cả L1 và L2).</param>
    /// <param name="localExpiration">Thời gian hết hạn L1 (RAM).</param>
    /// <returns>Đối tượng tùy chọn cache, hoặc null nếu không có thời gian hết hạn.</returns>
    private static HybridCacheEntryOptions? BuildOptions(TimeSpan? expiration, TimeSpan? localExpiration)
    {
        if (expiration is null && localExpiration is null) return null;

        return new HybridCacheEntryOptions
        {
            Expiration = expiration,
            LocalCacheExpiration = localExpiration
        };
    }
}
