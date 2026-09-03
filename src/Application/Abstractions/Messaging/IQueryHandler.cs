namespace TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;

/// <summary>
/// Xử lý một <see cref="IQuery{TResponse}"/> và trả về <typeparamref name="TResponse"/>.
/// Đây là giao diện chính cho các CQRS query handler.</summary>
/// <typeparam name="TQuery">Kiểu truy vấn cần xử lý.</typeparam>
/// <typeparam name="TResponse">Kiểu phản hồi trả về.</typeparam>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Xử lý truy vấn bất đồng bộ.
    /// </summary>
    /// <param name="query">Truy vấn cần xử lý.</param>
    /// <param name="cancellationToken">Token hủy.</param>
    /// <returns>Phản hồi <typeparamref name="TResponse"/>.</returns>
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
