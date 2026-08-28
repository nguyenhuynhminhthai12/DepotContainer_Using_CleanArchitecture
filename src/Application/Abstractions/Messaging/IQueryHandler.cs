namespace TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
    where TResponse : class
{
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
