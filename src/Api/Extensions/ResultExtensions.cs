using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Api.Extensions;

/// <summary>
/// Phương thức mở rộng chuyển đổi <see cref="Result"/> thành phản hồi HTTP Problem Details.
/// Ánh xạ <see cref="ErrorType"/> sang mã trạng thái HTTP tương ứng.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Chuyển đổi <see cref="Result"/> thất bại thành phản hồi HTTP 4xx theo chuẩn RFC 9457 Problem Details.
    /// </summary>
    /// <param name="result">Kết quả thất bại cần chuyển đổi.</param>
    /// <returns>Phản hồi <see cref="IResult"/> dạng Problem Details.</returns>
    /// <exception cref="InvalidOperationException">Nếu result thành công.</exception>
    public static IResult ToProblemDetails(this Result result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("Cannot convert a successful result to a problem.");

        var (statusCode, title) = result.Error!.Type switch
        {
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not Found"),
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Validation Error"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            _ => (StatusCodes.Status500InternalServerError, "Server Error")
        };

        return Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: result.Error.Message,
            extensions: new Dictionary<string, object?>
            {
                ["errorCode"] = result.Error.Code
            });
    }

    /// <summary>
    /// Chuyển đổi <see cref="Result{T}"/> thất bại thành phản hồi HTTP 4xx theo chuẩn RFC 9457 Problem Details.
    /// </summary>
    /// <typeparam name="T">Kiểu giá trị của Result.</typeparam>
    /// <param name="result">Kết quả thất bại cần chuyển đổi.</param>
    /// <returns>Phản hồi <see cref="IResult"/> dạng Problem Details.</returns>
    /// <exception cref="InvalidOperationException">Nếu result thành công.</exception>
    public static IResult ToProblemDetails<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("Cannot convert a successful result to a problem.");

        var (statusCode, title) = result.Error!.Type switch
        {
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not Found"),
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Validation Error"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            _ => (StatusCodes.Status500InternalServerError, "Server Error")
        };

        return Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: result.Error.Message,
            extensions: new Dictionary<string, object?>
            {
                ["errorCode"] = result.Error.Code
            });
    }
}
