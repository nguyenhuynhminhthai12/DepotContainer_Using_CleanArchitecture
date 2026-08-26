using TechSpherex.CleanArchitecture.Application.Abstractions.Caching;

namespace TechSpherex.CleanArchitecture.Application.UnitTests;

/// <summary>
/// A no-op <see cref="ICacheService"/> that always executes the factory directly
/// (cache is effectively bypassed). Used in unit tests where Redis/HybridCache
/// infrastructure is not available.
/// </summary>
internal sealed class FakeCacheService : ICacheService
{
    public Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        TimeSpan? localExpiration = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
        => factory(cancellationToken);

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null,
        TimeSpan? localExpiration = null, IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
