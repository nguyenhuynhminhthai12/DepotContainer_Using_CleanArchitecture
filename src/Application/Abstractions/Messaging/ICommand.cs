
using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;

public interface ICommand : ICommand<Result>;

#pragma warning disable S2326 // TResponse is used by the mediator pattern base interface
public interface ICommand<TResponse>;
#pragma warning restore S2326 // TResponse is used by the mediator pattern base interface
