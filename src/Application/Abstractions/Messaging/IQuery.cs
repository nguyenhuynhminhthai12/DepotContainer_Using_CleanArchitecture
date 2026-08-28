namespace TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;

public interface IQuery<TResponse>
    where TResponse : class
{
    Type ResponseType => typeof(TResponse);
}
