
using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;

/// <summary>
/// Đánh dấu một lệnh (command) không có phản hồi (response) — trả về <see cref="Result"/>.</summary>
public interface ICommand : ICommand<Result>;

#pragma warning disable S2326 // TResponse được sử dụng bởi giao diện base của mediator pattern
/// <summary>
/// Đánh dấu một lệnh (command) có phản hồi kiểu <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">Kiểu dữ liệu của phản hồi trả về.</typeparam>
public interface ICommand<TResponse>;
#pragma warning restore S2326 // TResponse được sử dụng bởi giao diện base của mediator pattern
