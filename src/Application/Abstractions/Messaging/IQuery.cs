namespace TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;

#pragma warning disable S2326 // TResponse được sử dụng bởi giao diện base của mediator pattern
/// <summary>
/// Đánh dấu một truy vấn (query) có phản hồi kiểu <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">Kiểu dữ liệu của phản hồi truy vấn.</typeparam>
public interface IQuery<TResponse>;
#pragma warning restore S2326 // TResponse được sử dụng bởi giao diện base của mediator pattern
