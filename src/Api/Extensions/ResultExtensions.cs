
using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Api.Extensions;

public static class ResultExtensions
{
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
