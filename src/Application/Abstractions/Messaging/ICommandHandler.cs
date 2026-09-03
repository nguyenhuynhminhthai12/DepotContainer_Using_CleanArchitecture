
using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;

/// <summary>
/// Xử lý một <see cref="ICommand"/> và trả về <see cref="Result"/>.
/// Là giao diện shorthand khi handler không trả về giá trị đặc biệt.</summary>
/// <typeparam name="TCommand">Kiểu lệnh cần xử lý.</typeparam>
public interface ICommandHandler<in TCommand> : ICommandHandler<TCommand, Result>
    where TCommand : ICommand;

/// <summary>
/// Xử lý một <see cref="ICommand{TResponse}"/> và trả về <typeparamref name="TResponse"/>.
/// Đây là giao diện chính cho các CQRS command handler.</summary>
/// <typeparam name="TCommand">Kiểu lệnh cần xử lý.</typeparam>
/// <typeparam name="TResponse">Kiểu phản hồi trả về.</typeparam>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <summary>
    /// Xử lý lệnh bất đồng bộ.
    /// </summary>
    /// <param name="command">Lệnh cần xử lý.</param>
    /// <param name="cancellationToken">Token hủy.</param>
    /// <returns>Phản hồi <typeparamref name="TResponse"/>.</returns>
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
