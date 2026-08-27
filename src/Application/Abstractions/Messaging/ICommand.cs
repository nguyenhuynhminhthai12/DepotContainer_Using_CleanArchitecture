using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;

public interface ICommand : ICommand<Result>;

// TResponse constrains the return type of command handlers (CQRS pattern)
// SonarLint false positive: S2326 warns about unused type parameter
#pragma warning disable S2326
public interface ICommand<TResponse> where TResponse : notnull;
#pragma warning restore S2326
